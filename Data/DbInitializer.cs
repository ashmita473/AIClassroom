using AIClassroom.Services;

namespace AIClassroom.Data;

/// <summary>Compatibility entry point. The active seed logic lives in SeedData.
/// This class intentionally contains no duplicate entity creation code.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db, PasswordService passwords)
    {
        await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(db, passwords);
    }
}
