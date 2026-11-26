using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using UserService.Domain.Entities;
using UserService.Service.Interfaces;

namespace UserService.Service.Extensions;

public static class ServiceLayerExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<ITokenService, Services.TokenService>();
        services.AddScoped<IUserService, Services.UserService>();
        services.AddSingleton<Metrics.UserMetrics>(); // SCRUM-173
        services.AddScoped<IRefreshTokenService, Services.RefreshTokenService>();

        return services;
    }
}
