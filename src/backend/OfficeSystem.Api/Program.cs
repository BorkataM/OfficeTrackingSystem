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

app.UseCors(ApiServiceCollectionExtensions.CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpointModules();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).WithTags("Diagnostics");

await app.RunAsync();
