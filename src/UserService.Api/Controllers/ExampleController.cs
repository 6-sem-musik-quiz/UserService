using UserService.Domain.DTOs;
using UserService.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExampleController(IExampleService exampleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExampleResponse>>> GetAll(CancellationToken ct) =>
        Ok(await exampleService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExampleResponse>> GetById(Guid id, CancellationToken ct)
    {
        var result = await exampleService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ExampleResponse>> Create(ExampleRequest request, CancellationToken ct)
    {
        var created = await exampleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExampleResponse>> Update(Guid id, ExampleRequest request, CancellationToken ct)
    {
        var updated = await exampleService.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await exampleService.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
