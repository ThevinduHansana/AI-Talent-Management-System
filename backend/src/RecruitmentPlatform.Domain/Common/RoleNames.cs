namespace RecruitmentPlatform.Domain.Common;

/// <summary>
/// Canonical role names used across authorization policies and seeding.
/// </summary>
public static class RoleNames
{
    public const string Candidate = "Candidate";
    public const string Recruiter = "Recruiter";
    public const string HiringManager = "HiringManager";
    public const string Administrator = "Administrator";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Candidate, Recruiter, HiringManager, Administrator
    };
}
