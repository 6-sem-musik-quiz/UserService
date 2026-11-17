using UserService.Domain.DTOs;

namespace UserService.Service.Interfaces;

public interface IExampleService
{
    Task<IReadOnlyList<ExampleResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ExampleResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExampleResponse> CreateAsync(ExampleRequest request, CancellationToken ct = default);
    Task<ExampleResponse?> UpdateAsync(Guid id, ExampleRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
