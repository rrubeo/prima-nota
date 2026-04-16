using System.Globalization;
using PrimaNota.Infrastructure;
using PrimaNota.Web.Components;
using Serilog;
using Serilog.Exceptions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting PrimaNota.Web");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails());

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHealthChecks()
        .AddInfrastructureHealthChecks(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
    });

    await app.RunAsync();
}
#pragma warning disable CA1031, S2139 // Top-level fatal handler: must log then rethrow to preserve process exit code
catch (Exception ex)
#pragma warning restore CA1031, S2139
{
    Log.Fatal(ex, "PrimaNota.Web terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
