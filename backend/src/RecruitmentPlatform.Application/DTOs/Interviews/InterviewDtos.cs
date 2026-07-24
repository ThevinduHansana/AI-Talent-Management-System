using RecruitmentPlatform.Domain.Enums;

namespace RecruitmentPlatform.Application.DTOs.Interviews;

/// <summary>
/// Request to schedule an interview. The candidate, job and recruiter are all resolved from the
/// application, so they cannot disagree with one another.
/// </summary>
public record CreateInterviewDto(
    Guid ApplicationId,
    string Title,
    DateTime InterviewDate,
    int DurationMinutes,
    InterviewMode Mode,
    string? Location,
    string? MeetingLink,
    Guid? InterviewerUserId,
    string? Notes);

/// <summary>Request to reschedule or amend an existing interview.</summary>
public record UpdateInterviewDto(
    string Title,
    DateTime InterviewDate,
    int DurationMinutes,
    InterviewMode Mode,
    string? Location,
    string? MeetingLink,
    Guid? InterviewerUserId,
    string? Notes,
    InterviewStatus? Status);

/// <summary>Full interview representation returned by the read endpoints.</summary>
public record InterviewResponseDto(
    Guid InterviewId,
    Guid ApplicationId,
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string Title,
    DateTime InterviewDate,
    int DurationMinutes,
    DateTime EndsAt,
    InterviewMode Mode,
    string? Location,
    string? MeetingLink,
    string? Notes,
    InterviewStatus Status,
    DateTime CreatedAt,
    string GoogleCalendarUrl,
    string OutlookCalendarUrl);

/// <summary>
/// Response for POST /api/interviews. Returns the calendar links so the recruiter can share them
/// directly without waiting for the email to arrive.
/// </summary>
public record InterviewCreatedDto(
    Guid InterviewId,
    string GoogleCalendarUrl,
    string OutlookCalendarUrl,
    InterviewStatus Status,
    string Message);
