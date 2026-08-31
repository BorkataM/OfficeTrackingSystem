using System.IO.Compression;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using OfficeSystem.Api.Endpoints;
using OfficeSystem.Api.Infrastructure;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Infrastructure.Authentication;

namespace OfficeSystem.Api.Extensions;

internal static class ApiServiceCollectionExtensions
{
    public const string CorsPolicy = "office-system-web";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddEndpointModules();

        // Enums cross the wire as names, so the client reads "Office" rather than 1.
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddCompression();

        services.AddOpenApi();

        services.AddBearerAuthentication(configuration);
        services.AddWebCors(configuration);
        services.AddAuthenticationRateLimiter();

        return services;
    }

    private static void AddBearerAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        JwtOptions jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                         ?? throw new InvalidOperationException(
                             $"Configuration section '{JwtOptions.SectionName}' is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();
    }

    private static void AddWebCors(this IServiceCollection services, IConfiguration configuration)
    {
        string[] origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                           ?? ["http://localhost:4200"];

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));
    }

    /// <summary>
    /// Schedule responses are repetitive JSON and compress to roughly a seventh of
    /// their size, which is the difference that shows on a slow connection.
    /// </summary>
    private static void AddCompression(this IServiceCollection services)
        => services.AddResponseCompression(options =>
        {
            // Only meaningful in production, where the API is behind TLS. The auth
            // endpoints are excluded from the pipeline instead — see UseApiPipeline.
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = [.. ResponseCompressionDefaults.MimeTypes, "application/problem+json"];
        });

    private static void AddAuthenticationRateLimiter(this IServiceCollection services)
        => services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitingPolicies.Authentication, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    // Partitioning by caller IP keeps one noisy client from locking everyone out.
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });
}
