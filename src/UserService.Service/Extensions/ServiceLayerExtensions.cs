using UserService.Service.Interfaces;
using UserService.Service.Services;
using Microsoft.Extensions.DependencyInjection;
using UserServiceImpl = UserService.Service.Services.UserService;

namespace UserService.Service.Extensions;

public static class ServiceLayerExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped<IExampleService, ExampleService>();
        services.AddScoped<IUserService, UserServiceImpl>();
        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
