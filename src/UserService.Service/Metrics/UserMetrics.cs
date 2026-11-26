using System.Diagnostics.Metrics;

namespace UserService.Service.Metrics;

// SCRUM-173: UserService's own instruments (meter "Slopster.User"), exported over OTLP.
//   slopster_logins_total{result=success|failed}  one per login attempt
public sealed class UserMetrics : IDisposable
{
    public const string MeterName = "Slopster.User";
    public const string LoginsName = "slopster_logins_total";

    private readonly Counter<long> _logins;

    public UserMetrics()
    {
        Meter = new Meter(MeterName);
        _logins = Meter.CreateCounter<long>(LoginsName, description: "Login attempts, by result.");
    }

    public Meter Meter { get; }

    public void Login(bool success) =>
        _logins.Add(1, new KeyValuePair<string, object?>("result", success ? "success" : "failed"));

    public void Dispose() => Meter.Dispose();
}
