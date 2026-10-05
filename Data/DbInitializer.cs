using AIClassroom.Services;
using Microsoft.EntityFrameworkCore;

namespace AIClassroom.Data;

/// <summary>Runs EF Core migrations and the idempotent seed routine.</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db, PasswordService passwords)
    {
        await db.Database.MigrateAsync();
        await SeedData.InitializeAsync(db, passwords);
    }
}
