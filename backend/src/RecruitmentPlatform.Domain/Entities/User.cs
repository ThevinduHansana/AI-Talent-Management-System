using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// An authenticated account. A user carries identity/security data and optionally links to a
/// role-specific profile (Candidate, Recruiter, HiringManager).
/// </summary>
public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsEmailConfirmed { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Password reset
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    // Optional organizational association (recruiters / hiring managers / admins).
    public Guid? OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigation
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public Candidate? Candidate { get; set; }

    public Recruiter? Recruiter { get; set; }

    public HiringManager? HiringManager { get; set; }
}
