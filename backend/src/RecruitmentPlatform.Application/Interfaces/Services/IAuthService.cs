using RecruitmentPlatform.Application.DTOs.Auth;

namespace RecruitmentPlatform.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task LogoutAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Generates a reset token. Returns the token so it can be emailed (dev returns it directly).</summary>
    Task<string> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<AuthUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
