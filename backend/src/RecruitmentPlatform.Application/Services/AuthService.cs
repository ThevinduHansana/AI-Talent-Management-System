using System.Security.Cryptography;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPlatform.Application.Common.Email;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.DTOs.Auth;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Application.Interfaces.Repositories;
using RecruitmentPlatform.Application.Interfaces.Services;
using RecruitmentPlatform.Domain.Common;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Application.Services;

/// <summary>
/// Handles registration, login, token refresh/rotation and password reset. Business rules for
/// authentication live here; the controller only adapts HTTP to these calls.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;
    private readonly JwtSettings _jwt;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IMapper mapper,
        IAuditService audit,
        IEmailService email,
        IOptions<JwtSettings> jwtOptions,
        IOptions<EmailSettings> emailOptions,
        ILogger<AuthService> logger)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _mapper = mapper;
        _audit = audit;
        _email = email;
        _jwt = jwtOptions.Value;
        _emailSettings = emailOptions.Value;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        if (await _uow.Users.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var role = await _uow.Repository<Role>()
            .FirstOrDefaultAsync(r => r.NormalizedName == RoleNames.Candidate.ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException("Role", RoleNames.Candidate);

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            IsEmailConfirmed = true // Email confirmation flow is out of scope for this build.
        };
        user.UserRoles.Add(new UserRole { Role = role });

        // Every self-registered account gets a candidate profile.
        user.Candidate = new Candidate { AvailabilityStatus = Domain.Enums.AvailabilityStatus.Available };

        await _uow.Users.AddAsync(user, cancellationToken);

        var auth = await IssueTokensAsync(user, new[] { role.Name }, ipAddress, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("UserRegistered", nameof(User), user.Id.ToString(),
            $"Candidate {user.Email} registered.", user.Id, ipAddress, cancellationToken: cancellationToken);

        // Welcome email. Best-effort: the account is already created and the caller is holding a
        // valid token, so a mail failure must not turn a successful signup into an error.
        try
        {
            await _email.SendAsync(
                user.Email,
                $"Welcome to GetCareers, {user.FirstName}",
                EmailTemplates.Welcome(user.FirstName, $"{_emailSettings.AppBaseUrl.TrimEnd('/')}/candidate/profile"),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send the welcome email to user {UserId}.", user.Id);
        }

        return auth;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByEmailWithRolesAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("This account has been deactivated.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var auth = await IssueTokensAsync(user, roles, ipAddress, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("UserLoggedIn", nameof(User), user.Id.ToString(),
            null, user.Id, ipAddress, cancellationToken: cancellationToken);

        return auth;
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var existing = await _uow.RefreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        var user = await _uow.Users.GetWithRolesAsync(existing.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (!user.IsActive)
        {
            throw new UnauthorizedException("This account has been deactivated.");
        }

        // Rotate: revoke the presented token and issue a new pair.
        var newRefresh = _tokenService.GenerateRefreshToken();
        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByToken = newRefresh;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var (accessToken, accessExpires) = _tokenService.GenerateAccessToken(user, roles);

        await _uow.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefresh,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        var userDto = _mapper.Map<AuthUserDto>(user);
        return new AuthResponse(accessToken, accessExpires, newRefresh, userDto);
    }

    public async Task LogoutAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default)
    {
        var token = await _uow.RefreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (token is { IsActive: true })
        {
            token.RevokedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            // Do not reveal whether the account exists.
            return string.Empty;
        }

        const int expiryMinutes = 60;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("PasswordResetRequested", nameof(User), user.Id.ToString(),
            null, user.Id, cancellationToken: cancellationToken);

        // Deliver the reset link. Failures are logged but never surfaced: the endpoint returns the
        // same response whether or not the account exists, so propagating a mail error here would
        // leak that the address is registered.
        try
        {
            var resetUrl = $"{_emailSettings.AppBaseUrl.TrimEnd('/')}/reset-password" +
                           $"?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

            await _email.SendAsync(
                user.Email,
                "Reset your GetCareers password",
                EmailTemplates.PasswordReset(user.FirstName, resetUrl, expiryMinutes),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send the password-reset email to user {UserId}.", user.Id);
        }

        return token;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new ValidationException("Token", "Invalid password reset request.");

        if (user.PasswordResetToken is null
            || user.PasswordResetToken != request.Token
            || user.PasswordResetTokenExpiresAt is null
            || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
        {
            throw new ValidationException("Token", "The password reset token is invalid or has expired.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;

        // Invalidate all active sessions after a password change.
        var activeTokens = await _uow.RefreshTokens.ListAsync(t => t.UserId == user.Id && t.RevokedAt == null, cancellationToken);
        foreach (var t in activeTokens)
        {
            t.RevokedAt = DateTime.UtcNow;
        }

        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("PasswordReset", nameof(User), user.Id.ToString(),
            null, user.Id, cancellationToken: cancellationToken);
    }

    public async Task<AuthUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetWithRolesAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        return _mapper.Map<AuthUserDto>(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, IReadOnlyList<string> roles, string? ipAddress, CancellationToken cancellationToken)
    {
        var (accessToken, accessExpires) = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _uow.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress
        }, cancellationToken);

        var userDto = new AuthUserDto(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.PhoneNumber, user.ProfilePictureUrl, roles);

        return new AuthResponse(accessToken, accessExpires, refreshToken, userDto);
    }
}
