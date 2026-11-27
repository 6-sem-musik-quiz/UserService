using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using UserService.Service.Exceptions;
using UserService.Service.Interfaces;

namespace UserService.Api.Controllers;

// SCRUM-197 (Platform ADR-0005): cleanup after a load test. Edge: DELETE /api/userservice/admin/users.
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public class AdminUsersController(IUserService userService) : ControllerBase
{
    public record DeleteResult(int Deleted);

    [HttpDelete]
    [ProducesResponseType<DeleteResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeleteResult>> DeleteLoadBots([FromQuery] string? prefix, CancellationToken ct)
    {
        try
        {
            return Ok(new DeleteResult(await userService.DeleteLoadBotsAsync(prefix ?? string.Empty, ct)));
        }
        catch (UserValidationException ex)
        {
            var modelState = new ModelStateDictionary();
            foreach (var (field, messages) in ex.Errors)
                foreach (var message in messages)
                    modelState.AddModelError(field, message);
            return ValidationProblem(modelState);
        }
    }
}

public static class AuthPolicies
{
    // The JWT claim "role" (set by TokenService) must be "Admin". Same policy as MusicService.
    public const string AdminOnly = "AdminOnly";
}
