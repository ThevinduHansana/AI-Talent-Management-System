using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Application.Common.Exceptions;
using RecruitmentPlatform.Application.Common.Interfaces;

namespace RecruitmentPlatform.API.Controllers;

/// <summary>
/// Base controller providing access to the authenticated user id. Controllers stay thin: they
/// translate HTTP to service calls and never contain business logic.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ICurrentUserService? _currentUser;

    protected ICurrentUserService CurrentUser =>
        _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

    /// <summary>Returns the authenticated user id or throws when the request is unauthenticated.</summary>
    protected Guid CurrentUserId =>
        CurrentUser.UserId ?? throw new UnauthorizedException("The request is not authenticated.");

    protected string? IpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();
}
