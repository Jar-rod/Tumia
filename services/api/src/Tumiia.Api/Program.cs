using Tumiia.Api.Logging;
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
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<TumiiaDbContext>("database");

var app = builder.Build();

// Request pipeline. Order matters: the correlation id must exist before anything logs.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseTumiiaRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseCors(CorsSetup.LocalFrontendPolicy);
}

app.UseAuthorization();
app.MapControllers();

if (OpenApiSetup.IsSwaggerEnabled(app.Configuration, app.Environment))
{
    app.UseTumiiaSwagger();
}

// In AWS, migrations run as a separate task before deploy. Locally, keep the database up to date on start.
if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();
}

await app.RunAsync();
