using Microsoft.Extensions.Options;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;

namespace RecruitmentPlatform.Infrastructure.Services;

public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>Root directory for locally stored uploads. Relative paths resolve to the content root.</summary>
    public string LocalBasePath { get; set; } = "uploads";
}

/// <summary>
/// Stores uploaded files on the local filesystem. Implements the same abstraction a cloud
/// provider would, so swapping to blob storage requires no changes to business logic.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IOptions<StorageSettings> options)
    {
        _basePath = Path.IsPathRooted(options.Value.LocalBasePath)
            ? options.Value.LocalBasePath
            : Path.Combine(AppContext.BaseDirectory, options.Value.LocalBasePath);
        Directory.CreateDirectory(_basePath);
    }

    public async Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType, string category, CancellationToken cancellationToken = default)
    {
        var safeCategory = SanitizeSegment(category);
        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";

        var categoryDir = Path.Combine(_basePath, safeCategory);
        Directory.CreateDirectory(categoryDir);

        var absolutePath = Path.Combine(categoryDir, storedFileName);
        await using (var fileStream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        // The stored path is relative so it remains valid if the base directory moves.
        var relativePath = Path.Combine(safeCategory, storedFileName).Replace('\\', '/');
        var size = new FileInfo(absolutePath).Length;
        return new StoredFile(storedFileName, relativePath, contentType, size);
    }

    public Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        var absolutePath = Resolve(path);
        if (!File.Exists(absolutePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var absolutePath = Resolve(path);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
        return Task.CompletedTask;
    }

    private string Resolve(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(_basePath, normalized));

        // Guard against path traversal outside the storage root.
        if (!full.StartsWith(Path.GetFullPath(_basePath), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Resolved path escapes the storage root.");
        }
        return full;
    }

    private static string SanitizeSegment(string segment)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(segment.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "misc" : cleaned.ToLowerInvariant();
    }
}
