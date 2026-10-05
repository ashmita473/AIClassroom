using AIClassroom.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIClassroom.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202610060001_LessonVideos")]
public partial class LessonVideos : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.Sql(@"
IF OBJECT_ID(N'dbo.LessonVideos', N'U') IS NULL
BEGIN
CREATE TABLE dbo.LessonVideos (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LessonVideos PRIMARY KEY,
    LessonId int NOT NULL,
    Title nvarchar(200) NOT NULL,
    Description nvarchar(1000) NOT NULL,
    YouTubeVideoId nvarchar(20) NOT NULL,
    SortOrder int NOT NULL,
    IsRequired bit NOT NULL,
    XpReward int NOT NULL,
    IsPublished bit NOT NULL,
    CONSTRAINT FK_LessonVideos_Lessons FOREIGN KEY(LessonId) REFERENCES dbo.Lessons(Id) ON DELETE CASCADE
);
CREATE INDEX IX_LessonVideos_LessonId_SortOrder ON dbo.LessonVideos(LessonId, SortOrder);
END;
IF OBJECT_ID(N'dbo.StudentLessonVideoProgress', N'U') IS NULL
BEGIN
CREATE TABLE dbo.StudentLessonVideoProgress (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StudentLessonVideoProgress PRIMARY KEY,
    StudentId int NOT NULL,
    LessonVideoId int NOT NULL,
    Completed bit NOT NULL,
    CompletedAtUtc datetime2 NULL,
    CONSTRAINT FK_StudentLessonVideoProgress_Students FOREIGN KEY(StudentId) REFERENCES dbo.Students(Id) ON DELETE CASCADE,
    CONSTRAINT FK_StudentLessonVideoProgress_LessonVideos FOREIGN KEY(LessonVideoId) REFERENCES dbo.LessonVideos(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_StudentLessonVideoProgress_Student_Video ON dbo.StudentLessonVideoProgress(StudentId, LessonVideoId);
END;");
    }
    protected override void Down(MigrationBuilder m)
    {
        m.Sql(@"IF OBJECT_ID(N'dbo.StudentLessonVideoProgress', N'U') IS NOT NULL DROP TABLE dbo.StudentLessonVideoProgress; IF OBJECT_ID(N'dbo.LessonVideos', N'U') IS NOT NULL DROP TABLE dbo.LessonVideos;");
    }
}
