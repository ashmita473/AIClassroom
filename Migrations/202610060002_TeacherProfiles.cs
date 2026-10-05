using AIClassroom.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIClassroom.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202610060002_TeacherProfiles")]
public partial class TeacherProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Guarded so databases that already have these columns (e.g. created earlier by EnsureCreated) do not fail.
        migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Teachers','ProfileImagePath') IS NULL ALTER TABLE dbo.Teachers ADD ProfileImagePath nvarchar(500) NOT NULL CONSTRAINT DF_Teachers_ProfileImagePath DEFAULT('');
IF COL_LENGTH('dbo.Teachers','Department') IS NULL ALTER TABLE dbo.Teachers ADD Department nvarchar(100) NOT NULL CONSTRAINT DF_Teachers_Department DEFAULT('');
IF COL_LENGTH('dbo.Teachers','Bio') IS NULL ALTER TABLE dbo.Teachers ADD Bio nvarchar(500) NOT NULL CONSTRAINT DF_Teachers_Bio DEFAULT('');
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Teachers','ProfileImagePath') IS NOT NULL BEGIN ALTER TABLE dbo.Teachers DROP CONSTRAINT DF_Teachers_ProfileImagePath; ALTER TABLE dbo.Teachers DROP COLUMN ProfileImagePath; END
IF COL_LENGTH('dbo.Teachers','Department') IS NOT NULL BEGIN ALTER TABLE dbo.Teachers DROP CONSTRAINT DF_Teachers_Department; ALTER TABLE dbo.Teachers DROP COLUMN Department; END
IF COL_LENGTH('dbo.Teachers','Bio') IS NOT NULL BEGIN ALTER TABLE dbo.Teachers DROP CONSTRAINT DF_Teachers_Bio; ALTER TABLE dbo.Teachers DROP COLUMN Bio; END
");
    }
}
