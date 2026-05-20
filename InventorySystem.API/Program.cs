using InventorySystem.Application;
using InventorySystem.Infrastructure;
using InventorySystem.Infrastructure.Data;
using InventorySystem.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();

    await DataSeeder.SeedAsync(db);
}

app.UseHttpsRedirection();

app.Run();
