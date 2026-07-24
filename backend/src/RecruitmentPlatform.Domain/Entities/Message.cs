using RecruitmentPlatform.Domain.Common;

namespace RecruitmentPlatform.Domain.Entities;

/// <summary>
/// A direct message between two users (e.g. recruiter to candidate).
/// </summary>
public class Message : BaseEntity
{
    public Guid SenderUserId { get; set; }

    public User SenderUser { get; set; } = null!;

    public Guid RecipientUserId { get; set; }

    public User RecipientUser { get; set; } = null!;

    public string? Subject { get; set; }

    public string Body { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }
}
