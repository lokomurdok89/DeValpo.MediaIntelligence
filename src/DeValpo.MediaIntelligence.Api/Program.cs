using DeValpo.MediaIntelligence.Api.Health;
using DeValpo.MediaIntelligence.Api.Middleware;
using DeValpo.MediaIntelligence.Api.Swagger;
using DeValpo.MediaIntelligence.Application;
using DeValpo.MediaIntelligence.Infrastructure;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(SwaggerResourceText.DocumentName, new OpenApiInfo
    {
        Title = SwaggerResourceText.Title,
        Version = SwaggerResourceText.Version,
        Description = SwaggerResourceText.Description
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
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
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            $"/swagger/{SwaggerResourceText.DocumentName}/swagger.json",
            SwaggerResourceText.EndpointName);
        options.RoutePrefix = "swagger";
    });

    app.MapGet("/", () => Results.Redirect("/swagger"));
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
