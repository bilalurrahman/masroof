using Masroof.Application;
using Masroof.Application.Abstractions;
using Masroof.Infrastructure;
using Masroof.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, cfg) => cfg
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext());

builder.Services.AddMasroofApplication();
builder.Services.AddMasroofInfrastructure(builder.Configuration);

// One settable current-user per scope, shared between ICurrentUser and the worker.
builder.Services.AddScoped<WorkerCurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<WorkerCurrentUser>());

builder.Services.AddHostedService<OutboxProcessor>();

// Weekly bank/wallet SMS ingestion (current month) from the on-device Messages inbox.
builder.Services.Configure<SmsIngestionScheduleOptions>(
    builder.Configuration.GetSection(SmsIngestionScheduleOptions.SectionName));
builder.Services.AddHostedService<WeeklySmsIngestionService>();

var host = builder.Build();
host.Run();
