using UserService.Service.Interfaces;
using UserService.Service.Services;
using Microsoft.Extensions.DependencyInjection;

namespace UserService.Service.Extensions;

public static class ServiceLayerExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped<IExampleService, ExampleService>();

        return services;
    }
}
