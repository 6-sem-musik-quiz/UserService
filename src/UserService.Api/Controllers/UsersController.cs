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
