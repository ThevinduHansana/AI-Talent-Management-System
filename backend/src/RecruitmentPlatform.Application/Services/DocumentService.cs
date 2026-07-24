using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.DTOs.Candidates;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Manages candidate resume documents. The binary is persisted through the storage abstraction
/// (local or cloud); only metadata is stored in the database.
/// </summary>
public class DocumentService : IDocumentService
{
    private const long MaxResumeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;

    public DocumentService(IUnitOfWork uow, IFileStorageService storage)
    {
        _uow = uow;
        _storage = storage;
    }

    public async Task<ResumeDto> UploadResumeAsync(Guid userId, Stream content, string fileName, string contentType, long length, bool makePrimary, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);

        if (length <= 0)
        {
            throw new ValidationException("File", "The uploaded file is empty.");
        }
        if (length > MaxResumeBytes)
        {
            throw new ValidationException("File", "Resume must be 5 MB or smaller.");
        }
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ValidationException("File", "Only PDF or Word documents are accepted.");
        }

        var stored = await _storage.SaveAsync(content, fileName, contentType, "resumes", cancellationToken);

        var existingResumes = await _uow.Repository<Resume>()
            .ListAsync(r => r.CandidateId == candidate.Id, cancellationToken);

        var isPrimary = makePrimary || existingResumes.Count == 0;
        if (isPrimary)
        {
            foreach (var r in existingResumes.Where(r => r.IsPrimary))
            {
                r.IsPrimary = false;
            }
        }

        var resume = new Resume
        {
            CandidateId = candidate.Id,
            FileName = fileName,
            StoredFileName = stored.StoredFileName,
            FilePath = stored.Path,
            ContentType = contentType,
            FileSize = length,
            IsPrimary = isPrimary,
            UploadedAt = DateTime.UtcNow
        };

        await _uow.Repository<Resume>().AddAsync(resume, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ResumeDto(resume.Id, resume.FileName, resume.ContentType, resume.FileSize, resume.IsPrimary, resume.UploadedAt);
    }

    public async Task<(Stream content, string contentType, string fileName)> DownloadResumeAsync(Guid userId, Guid resumeId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);
        var resume = await _uow.Repository<Resume>()
            .FirstOrDefaultAsync(r => r.Id == resumeId && r.CandidateId == candidate.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Resume), resumeId);

        var stream = await _storage.GetAsync(resume.FilePath, cancellationToken)
            ?? throw new NotFoundException("Resume file", resumeId);

        return (stream, resume.ContentType, resume.FileName);
    }

    public async Task DeleteResumeAsync(Guid userId, Guid resumeId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);
        var resume = await _uow.Repository<Resume>()
            .FirstOrDefaultAsync(r => r.Id == resumeId && r.CandidateId == candidate.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Resume), resumeId);

        await _storage.DeleteAsync(resume.FilePath, cancellationToken);
        _uow.Repository<Resume>().Remove(resume);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPrimaryResumeAsync(Guid userId, Guid resumeId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(userId, cancellationToken);
        var resumes = await _uow.Repository<Resume>()
            .ListAsync(r => r.CandidateId == candidate.Id, cancellationToken);

        var target = resumes.FirstOrDefault(r => r.Id == resumeId)
            ?? throw new NotFoundException(nameof(Resume), resumeId);

        foreach (var r in resumes)
        {
            r.IsPrimary = r.Id == target.Id;
        }
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private async Task<Candidate> GetCandidateAsync(Guid userId, CancellationToken cancellationToken)
        => await _uow.Candidates.GetByUserIdAsync(userId, cancellationToken)
           ?? throw new NotFoundException("Candidate profile", userId);
}
