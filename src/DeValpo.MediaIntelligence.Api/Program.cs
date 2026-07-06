using DeValpo.MediaIntelligence.Api.Health;
using DeValpo.MediaIntelligence.Api.Middleware;
using DeValpo.MediaIntelligence.Application;
using DeValpo.MediaIntelligence.Infrastructure;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "DeValpo Media Intelligence Engine",
        Version = "v1",
        Description = "Reusable multi-client media and document intelligence API."
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
        }
    });
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("MediaIntelligence")
    ?? throw new InvalidOperationException("ConnectionStrings:MediaIntelligence is required.");

builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres")
    .AddCheck<LocalStorageHealthCheck>("local-storage");

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

var swaggerEnabled = app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment());
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("DefaultCors");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await ApplyMigrationsIfConfiguredAsync(app);

app.Run();

static async Task ApplyMigrationsIfConfiguredAsync(WebApplication app)
{
    var databaseOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;

    if (!databaseOptions.ApplyMigrationsOnStartup)
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MediaIntelligenceDbContext>();
    await dbContext.Database.MigrateAsync();
}
