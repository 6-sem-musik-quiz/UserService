using UserService.Domain.Entities;
using UserService.Repository.Data;
using UserService.Repository.Interfaces;

namespace UserService.Repository.Repositories;

public class ExampleRepository(AppDbContext context) : Repository<Example>(context), IExampleRepository
{
}
