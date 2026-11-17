using UserService.Domain.Entities;
using UserService.Repository.Data;
using UserService.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace UserService.Repository.Repositories;

public class Repository<T>(AppDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context = context;
    protected DbSet<T> Set => Context.Set<T>();

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().ToListAsync(ct);

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        Set.Add(entity);
        await Context.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        Set.Update(entity);
        await Context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        Set.Remove(entity);
        await Context.SaveChangesAsync(ct);
    }
}
