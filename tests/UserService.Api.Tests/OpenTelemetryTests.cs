using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using UserService.Service.Metrics;

namespace UserService.Api.Tests;

// SCRUM-173: the app starts with OpenTelemetry traces and metrics, and the user meter.
public class OpenTelemetryTests(UserServiceApiFactory factory) : IClassFixture<UserServiceApiFactory>
{
    [Fact]
    public void App_StartsWithTracesAndMetrics()
    {
        using var client = factory.CreateClient(); // starts Program, including the OpenTelemetry setup

        Assert.NotNull(factory.Services.GetService<TracerProvider>());
        Assert.NotNull(factory.Services.GetService<MeterProvider>());
        Assert.NotNull(factory.Services.GetService<UserMetrics>());
    }
}
