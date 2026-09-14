using OfficeSystem.Api.Endpoints;
using OfficeSystem.Api.Extensions;
using OfficeSystem.Application;
using OfficeSystem.Infrastructure;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

WebApplication app = builder.Build();

await app.InitialiseDatabaseAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("Office System API")
        .WithTheme(ScalarTheme.BluePlanet));
}

// Compression is applied everywhere except /api/auth. Those responses carry a
// freshly minted token alongside the caller's own email, which is exactly the
// reflected-secret shape BREACH exploits when the body is compressed under TLS.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api/auth"),
    branch => branch.UseResponseCompression());

// The Angular build is copied into wwwroot by the Dockerfile, so the client is
// served from the same origin as the API. That is why API_BASE_URL can stay '/api'.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors(ApiServiceCollectionExtensions.CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpointModules();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).WithTags("Diagnostics");

// Lowest routing precedence, so real endpoints still win. Without it the SPA
// fallback would answer a mistyped /api route with index.html and a 200.
app.Map("/api/{*rest}", () => Results.NotFound()).ExcludeFromDescription();

app.MapFallbackToFile("index.html");

await app.RunAsync();
