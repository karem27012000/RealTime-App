using ChatApp.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

/// <summary>
/// Base class for controllers that need to know who is calling.
/// The Angular app sends the logged-in user's id in the "X-User-Id" header.
/// </summary>
/// <remarks>
/// This is nickname-only login, so the id is trusted as-is. A real app would use a
/// signed token (JWT) instead, so users could not pretend to be someone else.
/// </remarks>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    public const string UserIdHeader = "X-User-Id";

    protected Guid CurrentUserId =>
        Guid.TryParse(Request.Headers[UserIdHeader], out var id) ? id : throw new UnauthorizedException();
}
