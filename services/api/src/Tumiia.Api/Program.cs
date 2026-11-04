using Tumiia.Api.Logging;
using Tumiia.Api.RateLimiting;
using Tumiia.Api.Security;
using Tumiia.Api.Startup;
using Tumiia.Infrastructure;
using Tumiia.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.AddTumiiaLogging();
builder.Services.AddTumiiaControllers();
builder.Services.AddTumiiaApiVersioning();
builder.Services.AddTumiiaErrorHandling();
builder.Services.AddTumiiaCors();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddTumiiaApplication();
builder.Services.AddTumiiaAuthentication();
builder.Services.AddTumiiaRateLimiting(builder.Configuration);
builder.Services.Configure<OriginVerifySettings>(builder.Configuration.GetSection(OriginVerifySettings.SectionName));
builder.Services.AddHealthChecks().AddDbContextCheck<TumiiaDbContext>("database");

var app = builder.Build();
var swaggerEnabled = OpenApiSetup.IsSwaggerEnabled(app.Configuration, app.Environment);

// Request pipeline. Order matters: the correlation id must exist before anything logs, the origin check runs
// before anything else looks at the request, and rate limiting runs after authentication so it knows the user.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseTumiiaRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<OriginVerifyMiddleware>();

if (swaggerEnabled)
{
    app.UseTumiiaSwaggerUi();
}

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCors(CorsSetup.LocalFrontendPolicy);
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

if (swaggerEnabled)
{
    app.MapTumiiaOpenApi();
}

// A start-up failure (missing setting, unreachable database) is logged and ends the process with exit code 1.
// Left unhandled, .NET aborts, and in a container (where the app is process 1) that can hang instead of exiting.
try
{
    // In AWS, migrations run as a separate task before deploy. Locally, keep the database up to date on start.
    if (app.Environment.IsDevelopment())
    {
        await app.ApplyMigrationsAsync();
    }

    await app.RunAsync();
    return 0;
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    app.Logger.LogCritical(exception, "The API stopped because of an error");
    return 1;
}
