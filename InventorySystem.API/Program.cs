using InventorySystem.API.Extensions;
using InventorySystem.API.Middleware;
using InventorySystem.Infrastructure.Data;
using InventorySystem.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Inventory System API");

    var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = Directory.GetCurrentDirectory(),
        EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"
    });
    Log.Information("Web application builder created");

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .AddCommandLine(args);

    builder.Host.UseSerilog();
    builder.WebHost.UseKestrel();

    var urls = builder.Configuration["ASPNETCORE_URLS"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    if (!string.IsNullOrEmpty(urls))
    {
        builder.WebHost.UseUrls(urls);
    }

    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddSwaggerDocumentation();
    Log.Information("Services configured");

    var app = builder.Build();
    Log.Information("Application host built");

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Log.Information("Applying migrations...");
        await db.Database.MigrateAsync();
        Log.Information("Database migrated successfully");

        await DataSeeder.SeedAsync(db);
        Log.Information("Seed data applied");
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseHttpsRedirection();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex.GetType().Name == "HostAbortedException")
{
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
