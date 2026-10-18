using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GBA.Ecommerce.HealthChecks;

internal sealed class MainDatabaseHealthCheck : IHealthCheck {
    internal static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

    private readonly Func<CancellationToken, Task> _probe;

    internal MainDatabaseHealthCheck(string? connectionString)
        : this(cancellationToken => OpenConnectionAsync(connectionString, cancellationToken)) { }

    internal MainDatabaseHealthCheck(Func<CancellationToken, Task> probe) {
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        try {
            await _probe(cancellationToken).WaitAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            return HealthCheckResult.Unhealthy("Main database connection timed out.");
        } catch (Exception ex) {
            return HealthCheckResult.Unhealthy("Main database connection failed.", ex);
        }
    }

    private static async Task OpenConnectionAsync(
        string? connectionString,
        CancellationToken cancellationToken) {
        SqlConnectionStringBuilder builder = new(connectionString) {
            ConnectTimeout = (int)ProbeTimeout.TotalSeconds
        };

        await using SqlConnection connection = new(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
    }
}
