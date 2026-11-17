using UserService.Domain.DTOs;
using UserService.Repository.Interfaces;
using UserService.Service.Interfaces;
using UserService.Service.Mappings;

namespace UserService.Service.Services;

public class ExampleService(IExampleRepository repository) : IExampleService
{
    public async Task<IReadOnlyList<ExampleResponse>> GetAllAsync(CancellationToken ct = default) =>
        (await repository.GetAllAsync(ct)).Select(e => e.ToResponse()).ToList();

    public async Task<ExampleResponse?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        (await repository.GetByIdAsync(id, ct))?.ToResponse();

    public async Task<ExampleResponse> CreateAsync(ExampleRequest request, CancellationToken ct = default)
    {
        var entity = await repository.AddAsync(request.ToEntity(), ct);
        return entity.ToResponse();
    }

    public async Task<ExampleResponse?> UpdateAsync(Guid id, ExampleRequest request, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return null;

        entity.Name = request.Name;
        entity.Description = request.Description;
        await repository.UpdateAsync(entity, ct);

        return entity.ToResponse();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return false;

        await repository.DeleteAsync(entity, ct);
        return true;
    }
}
