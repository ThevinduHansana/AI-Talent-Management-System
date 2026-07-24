using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.DTOs.Auth;
using RecruitmentPlatform.Application.Interfaces.Services;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>Authentication endpoints: registration, login, token refresh and password reset.</summary>
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IWebHostEnvironment environment, ILogger<AuthController> logger)
    {
        _authService = authService;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>Registers a new candidate account and returns access + refresh tokens.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
        => Ok(await _authService.RegisterAsync(request, IpAddress, cancellationToken));

    /// <summary>Authenticates a user and returns access + refresh tokens.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        => Ok(await _authService.LoginAsync(request, IpAddress, cancellationToken));

    /// <summary>Exchanges a valid refresh token for a new access + refresh token pair.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
        => Ok(await _authService.RefreshTokenAsync(request, IpAddress, cancellationToken));

    /// <summary>Revokes a refresh token, ending the session.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Starts the password-reset flow. Returns 202 regardless of whether the email exists.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var token = await _authService.ForgotPasswordAsync(request, cancellationToken);

        // The token is a password-equivalent secret and must never travel in the response — that would
        // hand any caller an account takeover. Until a mailer is wired up, surface it to the developer
        // through the server-side log (never in Production, where it would leak into log aggregation).
        if (_environment.IsDevelopment() && !string.IsNullOrEmpty(token))
        {
            _logger.LogWarning(
                "DEV ONLY — password reset token for {Email}: {ResetToken}\n  Reset link: /reset-password?email={EmailEncoded}&token={ResetToken}",
                request.Email, token, Uri.EscapeDataString(request.Email), token);
        }

        // Always the same response, whether or not the account exists, so the endpoint cannot be
        // used to enumerate registered email addresses.
        return Accepted(new { message = "If an account exists, a reset link has been sent." });
    }

#if DEBUG
    /// <summary>
    /// DEV ONLY — issues a reset token and returns it directly, so the reset flow can be exercised
    /// without a mailer. Deliberately kept off <c>forgot-password</c>: that endpoint must never leak
    /// the token. This one is compiled out of Release builds entirely and additionally 404s outside
    /// the Development environment, so it cannot exist in a deployed API.
    /// </summary>
    [HttpPost("dev/reset-token")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> DevResetToken(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var token = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return string.IsNullOrEmpty(token) ? NotFound() : Ok(new { resetToken = token });
    }
#endif

    /// <summary>Completes a password reset using the emailed token.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Returns the profile of the currently authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken)
        => Ok(await _authService.GetCurrentUserAsync(CurrentUserId, cancellationToken));
}
