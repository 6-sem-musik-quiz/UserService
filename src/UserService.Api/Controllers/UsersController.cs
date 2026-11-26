using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using UserService.Domain.DTOs;
using UserService.Domain.Exceptions;
using UserService.Service.Exceptions;
using UserService.Service.Interfaces;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken ct)
    {
        // sub claim holds the user ID; MapInboundClaims=false keeps the original claim name.
        if (!int.TryParse(User.FindFirstValue("sub"), out var userId))
            return Unauthorized();

        var user = await userService.GetByIdAsync(userId, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType<IEnumerable<UserSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetByIds([FromQuery] string? ids, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(ids))
            return Ok(Array.Empty<UserSummaryDto>());

        var parts = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > 100)
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Too many IDs",
                detail: "At most 100 IDs are allowed per request.");

        var parsedIds = new List<int>(parts.Length);
        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var id))
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid ID",
                    detail: $"'{part}' is not a valid integer ID.");
            parsedIds.Add(id);
        }

        var users = await userService.GetByIdsAsync(parsedIds, ct);
        return Ok(users);
    }

    // Public sign-up (edge: POST /api/userservice/users). New users are always Player.
    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        try
        {
            var created = await userService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (UserValidationException ex)
        {
            var modelState = new ModelStateDictionary();
            foreach (var (field, messages) in ex.Errors)
                foreach (var message in messages)
                    modelState.AddModelError(field, message);
            return ValidationProblem(modelState);
        }
        catch (DuplicateUserNameException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "User name already taken.",
                detail: "A user with this user name already exists.");
        }
    }
}
