using Microsoft.AspNetCore.Mvc;
using UserService.Domain.DTOs;
using UserService.Service.Exceptions;
using UserService.Service.Interfaces;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IUserService userService,
    IRefreshTokenService refreshTokenService,
    ITokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<AuthLoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthLoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        try
        {
            var loginResult = await userService.LoginAsync(request, ct);
            var (refreshToken, refreshExpiresAt) = await refreshTokenService.CreateAsync(loginResult.User.Id, ct);

            return Ok(new AuthLoginResponse(
                loginResult.Token,
                loginResult.ExpiresAt,
                refreshToken,
                refreshExpiresAt,
                loginResult.User));
        }
        catch (InvalidCredentialsException)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid user name or password.");
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType<RefreshResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Refresh token is required.");

        try
        {
            var (newRefreshToken, refreshExpiresAt, user) = await refreshTokenService.RotateAsync(request.RefreshToken, ct);
            var (accessToken, accessExpiresAt) = tokenService.CreateToken(user);

            return Ok(new RefreshResponse(accessToken, accessExpiresAt, newRefreshToken, refreshExpiresAt));
        }
        catch (InvalidRefreshTokenException)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid or expired refresh token.");
        }
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        await refreshTokenService.RevokeAsync(request.RefreshToken, ct);
        return NoContent();
    }
}
