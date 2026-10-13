using ChatApp.Application.DTOs;
using ChatApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[Route("api/users")]
public sealed class UsersController(IUserService userService) : ApiControllerBase
{
    /// <summary>Logs in with a nickname. The user is created the first time the nickname is used.</summary>
    [HttpPost("login")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await userService.LoginAsync(request, cancellationToken));

    /// <summary>Returns the logged-in user (useful to check that a saved login is still valid).</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) =>
        Ok(await userService.GetRequiredAsync(CurrentUserId, cancellationToken));

    /// <summary>Lists all users with their online status, online users first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await userService.GetAllAsync(cancellationToken));
}
