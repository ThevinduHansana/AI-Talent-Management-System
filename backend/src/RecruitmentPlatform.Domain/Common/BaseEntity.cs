namespace RecruitmentPlatform.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Provides a surrogate key and audit timestamps
/// that the persistence layer maintains automatically.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
