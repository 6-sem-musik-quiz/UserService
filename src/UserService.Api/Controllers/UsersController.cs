using UserService.Domain.DTOs;
using UserService.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken ct)
    {
        try
        {
            var created = await userService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id) => NotFound();
}
