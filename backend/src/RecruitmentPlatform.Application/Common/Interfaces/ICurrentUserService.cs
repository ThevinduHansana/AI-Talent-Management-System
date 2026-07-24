namespace RecruitmentPlatform.Application.Common.Interfaces;

/// <summary>
/// Exposes the authenticated user for the current request. Implemented in the API layer over
/// the HTTP context so application services stay framework-agnostic.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    IReadOnlyList<string> Roles { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
