using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Api.Health;
using DeValpo.MediaIntelligence.Api.Middleware;
using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Api.Swagger;
using DeValpo.MediaIntelligence.Application;
using DeValpo.MediaIntelligence.Infrastructure;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Reflection;
using System.Text;

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

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = ApiResourceText.SwaggerBearerDescription
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
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

builder.Services.AddScoped<IDevJwtTokenService, DevJwtTokenService>();
builder.Services.AddScoped<IClientAccessTokenService, ClientAccessTokenService>();
AddJwtAuthentication(builder.Services, builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("MediaIntelligence")
    ?? throw new InvalidOperationException(ApiResourceText.ConnectionStringRequired);

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

    app.MapGet("/", () => Results.Redirect("/swagger"))
        .AllowAnonymous()
        .ExcludeFromDescription();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("DefaultCors");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await ApplyMigrationsIfConfiguredAsync(app);

app.Run();

// Configures JWT validation and the default policy that protects functional endpoints.
static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
{
    services.AddOptions<JwtOptions>()
        .Bind(configuration.GetSection("Jwt"))
        .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), ApiResourceText.JwtIssuerRequired)
        .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), ApiResourceText.JwtAudienceRequired)
        .Validate(options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32, ApiResourceText.JwtSigningKeyMinimumLength)
        .Validate(options => options.AccessTokenMinutes > 0, ApiResourceText.JwtAccessTokenMinutesRequired)
        .ValidateOnStart();

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();

    services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
        {
            var jwtOptions = jwtOptionsAccessor.Value;
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

            options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminScope", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                HasScope(context.User, "admin") ||
                HasScope(context.User, "media-intelligence:admin"));
        });

        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
}

static bool HasScope(System.Security.Claims.ClaimsPrincipal user, string scope)
{
    return user.FindAll(AuthClaimTypes.Scope)
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Any(value => string.Equals(value, scope, StringComparison.OrdinalIgnoreCase));
}

// Applies EF Core migrations at startup only when the database option explicitly enables it.
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

public partial class Program;
