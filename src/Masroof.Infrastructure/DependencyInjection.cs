using System.Net.Http.Headers;
using Masroof.Application.Abstractions;
using Masroof.Application.Ask;
using Masroof.Infrastructure.Ask;
using Masroof.Infrastructure.Classification;
using Masroof.Infrastructure.Identity;
using Masroof.Infrastructure.Llm;
using Masroof.Infrastructure.Persistence;
using Masroof.Infrastructure.PreParsers;
using Masroof.Infrastructure.Rules;
using Masroof.Infrastructure.Sms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;

namespace Masroof.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMasroofInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Sql")
            ?? throw new InvalidOperationException("Connection string 'Sql' is not configured.");

        services.AddDbContext<MasroofDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<MasroofDbContext>());

        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddSingleton<IClock, SystemClock>();

        services.Configure<LlmOptions>(config.GetSection(LlmOptions.SectionName));
        var llm = config.GetSection(LlmOptions.SectionName).Get<LlmOptions>() ?? new LlmOptions();

        services.AddSingleton<PromptBuilder>();
        services.AddSingleton(new LlmConcurrencyLimiter(llm.MaxConcurrency));
        services.AddSingleton<IPreParser, BankPreParser>();

        // On-device SMS inbox (macOS Messages chat.db) for scheduled bank/wallet ingestion.
        services.Configure<SmsInboxOptions>(config.GetSection(SmsInboxOptions.SectionName));
        services.AddSingleton<ISmsInbox, MessagesSmsInbox>();

        services.AddScoped<IReportQueries, ReportQueries>();
        services.AddScoped<IRuleStore, RuleStore>();
        services.AddScoped<IAskService, AskService>();

        // Typed HTTP clients to the LLM runtime, with Polly resilience (timeout, retry, breaker).
        services.AddHttpClient<ILlmTransactionParser, OllamaTransactionParser>(c => ConfigureLlmClient(c, llm))
            .AddResilience(llm);
        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(c => ConfigureLlmClient(c, llm))
            .AddResilience(llm);

        // Optional external category classifier (TypeSafe). Off unless configured; when enabled
        // it sends message text to an external API, relaxing zero-egress — an explicit opt-in.
        services.Configure<TypeSafeOptions>(config.GetSection(TypeSafeOptions.SectionName));
        var typeSafe = config.GetSection(TypeSafeOptions.SectionName).Get<TypeSafeOptions>() ?? new TypeSafeOptions();
        services.AddHttpClient<ICategoryClassifier, TypeSafeCategoryClassifier>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(typeSafe.TimeoutSeconds + 5);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        // Chat client for the Ask flow, with automatic tool invocation.
        services.AddSingleton<IChatClient>(_ =>
        {
            var ollama = new OllamaApiClient(new Uri(llm.Endpoint), llm.AskModel);
            return ((IChatClient)ollama)
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
        });

        return services;
    }

    private static void ConfigureLlmClient(HttpClient client, LlmOptions llm)
    {
        client.BaseAddress = new Uri(llm.Endpoint);
        client.Timeout = TimeSpan.FromSeconds(llm.TimeoutSeconds * 3 + 5);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static IHttpClientBuilder AddResilience(this IHttpClientBuilder builder, LlmOptions llm)
    {
        builder.AddStandardResilienceHandler(o =>
        {
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(llm.TimeoutSeconds);
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(llm.TimeoutSeconds * 3);
            o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(llm.TimeoutSeconds * 2);
            o.Retry.MaxRetryAttempts = 1;
        });
        return builder;
    }
}
