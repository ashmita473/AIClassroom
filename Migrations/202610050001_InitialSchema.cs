using AIClassroom.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIClassroom.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202610050001_InitialSchema")]
public partial class InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.Teachers', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Teachers (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Teachers PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Username nvarchar(50) NOT NULL,
    PasswordHash nvarchar(max) NOT NULL,
    Role nvarchar(30) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Teachers_IsActive DEFAULT(1)
);
CREATE UNIQUE INDEX IX_Teachers_Username ON dbo.Teachers(Username);
END;
IF OBJECT_ID(N'dbo.Students', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Students (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Students PRIMARY KEY,
    Name nvarchar(120) NOT NULL,
    RollNumber nvarchar(30) NOT NULL,
    PinHash nvarchar(max) NOT NULL,
    GroupName nvarchar(20) NOT NULL,
    ClassNumber int NOT NULL,
    Avatar nvarchar(100) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Students_IsActive DEFAULT(1),
    TotalXp int NOT NULL CONSTRAINT DF_Students_TotalXp DEFAULT(0),
    XpBalance int NOT NULL CONSTRAINT DF_Students_XpBalance DEFAULT(200),
    CreatedAtUtc datetime2 NOT NULL,
    LastActiveUtc datetime2 NULL
);
CREATE UNIQUE INDEX IX_Students_Name_Roll_Class ON dbo.Students(Name, RollNumber, ClassNumber);
END;
IF OBJECT_ID(N'dbo.CurriculumModules', N'U') IS NULL
BEGIN
CREATE TABLE dbo.CurriculumModules (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CurriculumModules PRIMARY KEY,
    GroupName nvarchar(20) NOT NULL, ClassMin int NOT NULL, ClassMax int NOT NULL,
    Code nvarchar(20) NOT NULL, Title nvarchar(150) NOT NULL, SortOrder int NOT NULL
);
CREATE UNIQUE INDEX IX_CurriculumModules_Group_Code ON dbo.CurriculumModules(GroupName, Code);
END;
IF OBJECT_ID(N'dbo.CurriculumDays', N'U') IS NULL
BEGIN
CREATE TABLE dbo.CurriculumDays (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CurriculumDays PRIMARY KEY,
    CurriculumModuleId int NOT NULL, DayNumber int NOT NULL, Title nvarchar(200) NOT NULL,
    ContentSummary nvarchar(max) NOT NULL, IsPublished bit NOT NULL, IsLocked bit NOT NULL,
    XpReward int NOT NULL,
    CONSTRAINT FK_CurriculumDays_CurriculumModules FOREIGN KEY(CurriculumModuleId) REFERENCES dbo.CurriculumModules(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_CurriculumDays_Module_Day ON dbo.CurriculumDays(CurriculumModuleId, DayNumber);
END;
IF OBJECT_ID(N'dbo.Lessons', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Lessons (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Lessons PRIMARY KEY,
    CurriculumDayId int NOT NULL, Introduction nvarchar(200) NOT NULL, Explanation nvarchar(max) NOT NULL,
    Examples nvarchar(max) NOT NULL, TeacherNotes nvarchar(max) NOT NULL, StudentNotesTemplate nvarchar(max) NOT NULL, IsPublished bit NOT NULL,
    CONSTRAINT FK_Lessons_CurriculumDays FOREIGN KEY(CurriculumDayId) REFERENCES dbo.CurriculumDays(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_Lessons_CurriculumDayId ON dbo.Lessons(CurriculumDayId);
END;
IF OBJECT_ID(N'dbo.Activities', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Activities (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Activities PRIMARY KEY,
    LessonId int NOT NULL, ActivityType nvarchar(30) NOT NULL, Title nvarchar(200) NOT NULL,
    Content nvarchar(max) NOT NULL, SortOrder int NOT NULL, XpReward int NOT NULL,
    CONSTRAINT FK_Activities_Lessons FOREIGN KEY(LessonId) REFERENCES dbo.Lessons(Id) ON DELETE CASCADE
);
END;
IF OBJECT_ID(N'dbo.Games', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Games (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Games PRIMARY KEY,
    Name nvarchar(100) NOT NULL, GroupName nvarchar(20) NOT NULL, CurriculumDayId int NULL,
    GameType nvarchar(50) NOT NULL, Description nvarchar(300) NOT NULL, BaseXp int NOT NULL, IsActive bit NOT NULL
);
END;
IF OBJECT_ID(N'dbo.GameResults', N'U') IS NULL
BEGIN
CREATE TABLE dbo.GameResults (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_GameResults PRIMARY KEY,
    StudentId int NOT NULL, GameId int NOT NULL, Score int NOT NULL, XpEarned int NOT NULL, PlayedAtUtc datetime2 NOT NULL
);
END;
IF OBJECT_ID(N'dbo.Quizzes', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Quizzes (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Quizzes PRIMARY KEY,
    Title nvarchar(200) NOT NULL, GroupName nvarchar(20) NOT NULL, CurriculumDayId int NULL,
    TimeLimitMinutes int NOT NULL, PassingScore int NOT NULL, MaxAttempts int NOT NULL,
    RandomizeQuestions bit NOT NULL, ShowInstantResults bit NOT NULL, IsPublished bit NOT NULL
);
END;
IF OBJECT_ID(N'dbo.Tests', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Tests (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tests PRIMARY KEY,
    Title nvarchar(200) NOT NULL, TestType nvarchar(30) NOT NULL, GroupName nvarchar(20) NOT NULL, CurriculumDayId int NULL,
    TimeLimitMinutes int NOT NULL, PassingScore int NOT NULL, RandomizeQuestions bit NOT NULL, IsPublished bit NOT NULL
);
END;
IF OBJECT_ID(N'dbo.Questions', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Questions (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Questions PRIMARY KEY,
    QuizId int NULL, TestId int NULL, QuestionType nvarchar(30) NOT NULL, Topic nvarchar(120) NOT NULL,
    Prompt nvarchar(max) NOT NULL, ImagePath nvarchar(max) NULL, Answer nvarchar(max) NULL, Explanation nvarchar(max) NULL,
    Points int NOT NULL, SortOrder int NOT NULL, IsBanked bit NOT NULL, Difficulty nvarchar(30) NOT NULL,
    CONSTRAINT FK_Questions_Quizzes FOREIGN KEY(QuizId) REFERENCES dbo.Quizzes(Id),
    CONSTRAINT FK_Questions_Tests FOREIGN KEY(TestId) REFERENCES dbo.Tests(Id)
);
END;
IF OBJECT_ID(N'dbo.QuestionOptions', N'U') IS NULL
BEGIN
CREATE TABLE dbo.QuestionOptions (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_QuestionOptions PRIMARY KEY,
    QuestionId int NOT NULL, Text nvarchar(500) NOT NULL, IsCorrect bit NOT NULL, MatchKey nvarchar(500) NULL, OrderIndex int NOT NULL,
    CONSTRAINT FK_QuestionOptions_Questions FOREIGN KEY(QuestionId) REFERENCES dbo.Questions(Id) ON DELETE CASCADE
);
END;
IF OBJECT_ID(N'dbo.QuizResults', N'U') IS NULL
BEGIN
CREATE TABLE dbo.QuizResults (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_QuizResults PRIMARY KEY,
    StudentId int NOT NULL, QuizId int NOT NULL, Score int NOT NULL, Correct int NOT NULL, Wrong int NOT NULL, Skipped int NOT NULL,
    XpEarned int NOT NULL, TimeTakenSeconds int NOT NULL, Passed bit NOT NULL, CompletedAtUtc datetime2 NOT NULL
);
END;
IF OBJECT_ID(N'dbo.TestResults', N'U') IS NULL
BEGIN
CREATE TABLE dbo.TestResults (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TestResults PRIMARY KEY,
    StudentId int NOT NULL, TestId int NOT NULL, Score int NOT NULL, Correct int NOT NULL, Wrong int NOT NULL, Skipped int NOT NULL,
    XpEarned int NOT NULL, TimeTakenSeconds int NOT NULL, Passed bit NOT NULL, CompletedAtUtc datetime2 NOT NULL
);
END;
IF OBJECT_ID(N'dbo.AssessmentAnswers', N'U') IS NULL
BEGIN
CREATE TABLE dbo.AssessmentAnswers (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssessmentAnswers PRIMARY KEY,
    QuizResultId int NULL, TestResultId int NULL, QuestionId int NOT NULL, IsCorrect bit NOT NULL,
    GivenAnswer nvarchar(max) NOT NULL, Topic nvarchar(120) NOT NULL, PointsEarned int NOT NULL
);
END;
IF OBJECT_ID(N'dbo.Assignments', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Assignments (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Assignments PRIMARY KEY,
    Title nvarchar(200) NOT NULL, Instructions nvarchar(max) NOT NULL, GroupName nvarchar(20) NOT NULL,
    CurriculumDayId int NULL, DueDateUtc datetime2 NULL, XpReward int NOT NULL, IsPublished bit NOT NULL
);
END;
IF OBJECT_ID(N'dbo.Submissions', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Submissions (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Submissions PRIMARY KEY,
    AssignmentId int NOT NULL, StudentId int NOT NULL, Content nvarchar(max) NOT NULL, Marks int NULL, Feedback nvarchar(max) NULL, SubmittedAtUtc datetime2 NOT NULL
);
END;
IF OBJECT_ID(N'dbo.StudentNotes', N'U') IS NULL
BEGIN
CREATE TABLE dbo.StudentNotes (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StudentNotes PRIMARY KEY,
    StudentId int NOT NULL, CurriculumDayId int NULL, Title nvarchar(200) NOT NULL, Content nvarchar(max) NOT NULL,
    IsPinned bit NOT NULL, UpdatedAtUtc datetime2 NOT NULL
);
END;
IF OBJECT_ID(N'dbo.TeacherNotes', N'U') IS NULL
BEGIN
CREATE TABLE dbo.TeacherNotes (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeacherNotes PRIMARY KEY,
    CurriculumDayId int NULL, Title nvarchar(200) NOT NULL, Content nvarchar(max) NOT NULL
);
END;
IF OBJECT_ID(N'dbo.StudentProgress', N'U') IS NULL
BEGIN
CREATE TABLE dbo.StudentProgress (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StudentProgress PRIMARY KEY,
    StudentId int NOT NULL, CurriculumDayId int NOT NULL, ProgressPercent int NOT NULL, Completed bit NOT NULL, CompletedAtUtc datetime2 NULL
);
END;
IF OBJECT_ID(N'dbo.XpTransactions', N'U') IS NULL
BEGIN
CREATE TABLE dbo.XpTransactions (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_XpTransactions PRIMARY KEY,
    StudentId int NOT NULL, Points int NOT NULL, Reason nvarchar(250) NOT NULL, CreatedAtUtc datetime2 NOT NULL
);
END;
IF OBJECT_ID(N'dbo.Badges', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Badges (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Badges PRIMARY KEY,
    Name nvarchar(100) NOT NULL, Description nvarchar(250) NOT NULL, Icon nvarchar(20) NOT NULL
);
END;
IF OBJECT_ID(N'dbo.StudentBadges', N'U') IS NULL
BEGIN
CREATE TABLE dbo.StudentBadges (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StudentBadges PRIMARY KEY,
    StudentId int NOT NULL, BadgeId int NOT NULL, EarnedAtUtc datetime2 NOT NULL,
    CONSTRAINT FK_StudentBadges_Students FOREIGN KEY(StudentId) REFERENCES dbo.Students(Id) ON DELETE CASCADE,
    CONSTRAINT FK_StudentBadges_Badges FOREIGN KEY(BadgeId) REFERENCES dbo.Badges(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_StudentBadges_Student_Badge ON dbo.StudentBadges(StudentId, BadgeId);
END;
IF OBJECT_ID(N'dbo.LiveClasses', N'U') IS NULL
BEGIN
CREATE TABLE dbo.LiveClasses (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LiveClasses PRIMARY KEY,
    GroupName nvarchar(20) NOT NULL, ClassNumber int NOT NULL, CurriculumDayId int NOT NULL, Status nvarchar(20) NOT NULL,
    CurrentActivityIndex int NOT NULL, StartedAtUtc datetime2 NOT NULL, EndedAtUtc datetime2 NULL,
    CONSTRAINT FK_LiveClasses_CurriculumDays FOREIGN KEY(CurriculumDayId) REFERENCES dbo.CurriculumDays(Id)
);
END;
IF OBJECT_ID(N'dbo.Announcements', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Announcements (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Announcements PRIMARY KEY,
    Title nvarchar(200) NOT NULL, Message nvarchar(max) NOT NULL, GroupName nvarchar(20) NULL, CreatedAtUtc datetime2 NOT NULL, IsActive bit NOT NULL
);
END;
IF OBJECT_ID(N'dbo.ScoreSettings', N'U') IS NULL
BEGIN
CREATE TABLE dbo.ScoreSettings (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ScoreSettings PRIMARY KEY,
    [Key] nvarchar(50) NOT NULL, Points int NOT NULL
);
CREATE UNIQUE INDEX IX_ScoreSettings_Key ON dbo.ScoreSettings([Key]);
END;
IF OBJECT_ID(N'dbo.CharacterItems', N'U') IS NULL
BEGIN
CREATE TABLE dbo.CharacterItems (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CharacterItems PRIMARY KEY,
    Category nvarchar(30) NOT NULL, Name nvarchar(60) NOT NULL, StyleKey nvarchar(40) NOT NULL, Description nvarchar(250) NOT NULL,
    Cost int NOT NULL, IsDefault bit NOT NULL, IsActive bit NOT NULL
);
END;
IF OBJECT_ID(N'dbo.StudentCharacters', N'U') IS NULL
BEGIN
CREATE TABLE dbo.StudentCharacters (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StudentCharacters PRIMARY KEY,
    StudentId int NOT NULL, HairItemId int NULL, OutfitItemId int NULL, HatItemId int NULL, AccessoryItemId int NULL,
    ShoesItemId int NULL, PetItemId int NULL, HairColorItemId int NULL, SkinToneItemId int NULL, BodyType nvarchar(10) NOT NULL,
    CONSTRAINT FK_StudentCharacters_Students FOREIGN KEY(StudentId) REFERENCES dbo.Students(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_StudentCharacters_StudentId ON dbo.StudentCharacters(StudentId);
END;
IF OBJECT_ID(N'dbo.StudentCharacterItems', N'U') IS NULL
BEGIN
CREATE TABLE dbo.StudentCharacterItems (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StudentCharacterItems PRIMARY KEY,
    StudentId int NOT NULL, CharacterItemId int NOT NULL, UnlockedAtUtc datetime2 NOT NULL,
    CONSTRAINT FK_StudentCharacterItems_Students FOREIGN KEY(StudentId) REFERENCES dbo.Students(Id) ON DELETE CASCADE,
    CONSTRAINT FK_StudentCharacterItems_CharacterItems FOREIGN KEY(CharacterItemId) REFERENCES dbo.CharacterItems(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_StudentCharacterItems_Student_Item ON dbo.StudentCharacterItems(StudentId, CharacterItemId);
END;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.StudentCharacterItems', N'U') IS NOT NULL DROP TABLE dbo.StudentCharacterItems;
IF OBJECT_ID(N'dbo.StudentCharacters', N'U') IS NOT NULL DROP TABLE dbo.StudentCharacters;
IF OBJECT_ID(N'dbo.CharacterItems', N'U') IS NOT NULL DROP TABLE dbo.CharacterItems;
IF OBJECT_ID(N'dbo.ScoreSettings', N'U') IS NOT NULL DROP TABLE dbo.ScoreSettings;
IF OBJECT_ID(N'dbo.Announcements', N'U') IS NOT NULL DROP TABLE dbo.Announcements;
IF OBJECT_ID(N'dbo.LiveClasses', N'U') IS NOT NULL DROP TABLE dbo.LiveClasses;
IF OBJECT_ID(N'dbo.StudentBadges', N'U') IS NOT NULL DROP TABLE dbo.StudentBadges;
IF OBJECT_ID(N'dbo.Badges', N'U') IS NOT NULL DROP TABLE dbo.Badges;
IF OBJECT_ID(N'dbo.XpTransactions', N'U') IS NOT NULL DROP TABLE dbo.XpTransactions;
IF OBJECT_ID(N'dbo.StudentProgress', N'U') IS NOT NULL DROP TABLE dbo.StudentProgress;
IF OBJECT_ID(N'dbo.TeacherNotes', N'U') IS NOT NULL DROP TABLE dbo.TeacherNotes;
IF OBJECT_ID(N'dbo.StudentNotes', N'U') IS NOT NULL DROP TABLE dbo.StudentNotes;
IF OBJECT_ID(N'dbo.Submissions', N'U') IS NOT NULL DROP TABLE dbo.Submissions;
IF OBJECT_ID(N'dbo.Assignments', N'U') IS NOT NULL DROP TABLE dbo.Assignments;
IF OBJECT_ID(N'dbo.AssessmentAnswers', N'U') IS NOT NULL DROP TABLE dbo.AssessmentAnswers;
IF OBJECT_ID(N'dbo.TestResults', N'U') IS NOT NULL DROP TABLE dbo.TestResults;
IF OBJECT_ID(N'dbo.QuizResults', N'U') IS NOT NULL DROP TABLE dbo.QuizResults;
IF OBJECT_ID(N'dbo.QuestionOptions', N'U') IS NOT NULL DROP TABLE dbo.QuestionOptions;
IF OBJECT_ID(N'dbo.Questions', N'U') IS NOT NULL DROP TABLE dbo.Questions;
IF OBJECT_ID(N'dbo.Tests', N'U') IS NOT NULL DROP TABLE dbo.Tests;
IF OBJECT_ID(N'dbo.Quizzes', N'U') IS NOT NULL DROP TABLE dbo.Quizzes;
IF OBJECT_ID(N'dbo.GameResults', N'U') IS NOT NULL DROP TABLE dbo.GameResults;
IF OBJECT_ID(N'dbo.Games', N'U') IS NOT NULL DROP TABLE dbo.Games;
IF OBJECT_ID(N'dbo.Activities', N'U') IS NOT NULL DROP TABLE dbo.Activities;
IF OBJECT_ID(N'dbo.Lessons', N'U') IS NOT NULL DROP TABLE dbo.Lessons;
IF OBJECT_ID(N'dbo.CurriculumDays', N'U') IS NOT NULL DROP TABLE dbo.CurriculumDays;
IF OBJECT_ID(N'dbo.CurriculumModules', N'U') IS NOT NULL DROP TABLE dbo.CurriculumModules;
IF OBJECT_ID(N'dbo.Students', N'U') IS NOT NULL DROP TABLE dbo.Students;
IF OBJECT_ID(N'dbo.Teachers', N'U') IS NOT NULL DROP TABLE dbo.Teachers;");
    }
}
