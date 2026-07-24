using RecruitmentPlatform.Application.DTOs.Interviews;

namespace RecruitmentPlatform.Application.Interfaces.Services;

/// <summary>
/// Interview scheduling with calendar integration: persists the interview, generates Google and
/// Outlook links plus an .ics attachment, and emails the invitation to the candidate.
/// </summary>
public interface IInterviewSchedulingService
{
    Task<InterviewCreatedDto> CreateAsync(Guid userId, CreateInterviewDto request, CancellationToken cancellationToken = default);

    Task<InterviewResponseDto> UpdateAsync(Guid userId, Guid interviewId, UpdateInterviewDto request, CancellationToken cancellationToken = default);

    /// <summary>Cancels the interview and emails a calendar cancellation to the candidate.</summary>
    Task CancelAsync(Guid userId, Guid interviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Interviews visible to the caller: a recruiter sees the ones on their jobs, a candidate sees
    /// their own. Optionally filtered to future, non-cancelled interviews.
    /// </summary>
    Task<IReadOnlyList<InterviewResponseDto>> GetAllAsync(Guid userId, bool upcomingOnly = false, CancellationToken cancellationToken = default);

    Task<InterviewResponseDto> GetByIdAsync(Guid userId, Guid interviewId, CancellationToken cancellationToken = default);
}
