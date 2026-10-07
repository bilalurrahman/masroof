using Dapper;
using Masroof.Infrastructure.Llm;
using Masroof.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Masroof.Api.Health;

/// <summary>Readiness check: the database answers a trivial query.</summary>
public sealed class SqlHealthCheck(ISqlConnectionFactory factory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var conn = await factory.OpenAsync(ct);
            await conn.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: ct));
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server is not reachable.", ex);
        }
    }
}

/// <summary>Readiness check: the LLM runtime responds to its model-list endpoint.</summary>
public sealed class LlmHealthCheck(IHttpClientFactory httpFactory, IOptions<LlmOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var client = httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var response = await client.GetAsync($"{options.Value.Endpoint.TrimEnd('/')}/api/tags", ct);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded($"LLM responded {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("LLM runtime is not reachable.", ex);
        }
    }
}
