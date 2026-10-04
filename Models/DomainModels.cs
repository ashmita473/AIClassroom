using System.ComponentModel.DataAnnotations;

namespace AIClassroom.Models;

public class Teacher
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(50)] public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    [MaxLength(30)] public string Role { get; set; } = "Teacher";
    public bool IsActive { get; set; } = true;
}

public class Student
{
    public int Id { get; set; }
    [MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(30)] public string RollNumber { get; set; } = "";
    public string PinHash { get; set; } = "";
    [MaxLength(20)] public string GroupName { get; set; } = "";
    public int ClassNumber { get; set; }
    [MaxLength(100)] public string Avatar { get; set; } = "🤖";
    public bool IsActive { get; set; } = true;
    public int TotalXp { get; set; }
    // Spendable XP wallet. TotalXp remains lifetime earned XP for rankings.
    public int XpBalance { get; set; } = 200;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveUtc { get; set; }
}

public class CurriculumModule
{
    public int Id { get; set; }
    [MaxLength(20)] public string GroupName { get; set; } = "";
    public int ClassMin { get; set; }
    public int ClassMax { get; set; }
    [MaxLength(20)] public string Code { get; set; } = "";
    [MaxLength(150)] public string Title { get; set; } = "";
    public int SortOrder { get; set; }
    public ICollection<CurriculumDay> Days { get; set; } = new List<CurriculumDay>();
}

public class CurriculumDay
{
    public int Id { get; set; }
    public int CurriculumModuleId { get; set; }
    public CurriculumModule? Module { get; set; }
    public int DayNumber { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    public string ContentSummary { get; set; } = "";
    public bool IsPublished { get; set; }
    public bool IsLocked { get; set; }
    public int XpReward { get; set; } = 50;
    public Lesson? Lesson { get; set; }
}

public class Lesson
{
    public int Id { get; set; }
    public int CurriculumDayId { get; set; }
    public CurriculumDay? CurriculumDay { get; set; }
    [MaxLength(200)] public string Introduction { get; set; } = "";
    public string Explanation { get; set; } = "";
    public string Examples { get; set; } = "";
    public string TeacherNotes { get; set; } = "";
    public string StudentNotesTemplate { get; set; } = "";
    public bool IsPublished { get; set; }
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}

public class Activity
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    [MaxLength(30)] public string ActivityType { get; set; } = "Question";
    [MaxLength(200)] public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int SortOrder { get; set; }
    public int XpReward { get; set; } = 10;
}

public class Game
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(20)] public string GroupName { get; set; } = "";
    public int? CurriculumDayId { get; set; }
    [MaxLength(50)] public string GameType { get; set; } = "";
    [MaxLength(300)] public string Description { get; set; } = "";
    public int BaseXp { get; set; } = 50;
    public bool IsActive { get; set; } = true;
}

public class GameResult
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int GameId { get; set; }
    public int Score { get; set; }
    public int XpEarned { get; set; }
    public DateTime PlayedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Question
{
    public int Id { get; set; }
    public int? QuizId { get; set; }
    public int? TestId { get; set; }
    [MaxLength(30)] public string QuestionType { get; set; } = "MCQ";
    [MaxLength(120)] public string Topic { get; set; } = "General";
    public string Prompt { get; set; } = "";
    public string? ImagePath { get; set; }
    public string? Answer { get; set; }
    public string? Explanation { get; set; }
    public int Points { get; set; } = 10;
    public int SortOrder { get; set; }
    public bool IsBanked { get; set; } = true;
    [MaxLength(30)] public string Difficulty { get; set; } = "Easy";
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}

public class QuestionOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
    [MaxLength(500)] public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }
    [MaxLength(500)] public string? MatchKey { get; set; }
    public int OrderIndex { get; set; }
}

public class Quiz
{
    public int Id { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(20)] public string GroupName { get; set; } = "";
    public int? CurriculumDayId { get; set; }
    public int TimeLimitMinutes { get; set; } = 10;
    public int PassingScore { get; set; } = 60;
    public int MaxAttempts { get; set; } = 1;
    public bool RandomizeQuestions { get; set; } = true;
    public bool ShowInstantResults { get; set; } = true;
    public bool IsPublished { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}

public class QuizResult
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }
    public int Score { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public int Skipped { get; set; }
    public int XpEarned { get; set; }
    public int TimeTakenSeconds { get; set; }
    public bool Passed { get; set; }
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AssessmentAnswer
{
    public int Id { get; set; }
    public int? QuizResultId { get; set; }
    public int? TestResultId { get; set; }
    public int QuestionId { get; set; }
    public bool IsCorrect { get; set; }
    public string GivenAnswer { get; set; } = "";
    [MaxLength(120)] public string Topic { get; set; } = "General";
    public int PointsEarned { get; set; }
}

public class Test
{
    public int Id { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(30)] public string TestType { get; set; } = "Unit";
    [MaxLength(20)] public string GroupName { get; set; } = "";
    public int? CurriculumDayId { get; set; }
    public int TimeLimitMinutes { get; set; } = 20;
    public int PassingScore { get; set; } = 60;
    public bool RandomizeQuestions { get; set; } = true;
    public bool IsPublished { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}

public class TestResult
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int TestId { get; set; }
    public Test? Test { get; set; }
    public int Score { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public int Skipped { get; set; }
    public int XpEarned { get; set; }
    public int TimeTakenSeconds { get; set; }
    public bool Passed { get; set; }
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Assignment
{
    public int Id { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    public string Instructions { get; set; } = "";
    [MaxLength(20)] public string GroupName { get; set; } = "";
    public int? CurriculumDayId { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public int XpReward { get; set; } = 50;
    public bool IsPublished { get; set; }
}

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public string Content { get; set; } = "";
    public int? Marks { get; set; }
    public string? Feedback { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}

public class StudentNote
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int? CurriculumDayId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsPinned { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class TeacherNote
{
    public int Id { get; set; }
    public int? CurriculumDayId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    public string Content { get; set; } = "";
}

public class StudentProgress
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CurriculumDayId { get; set; }
    public int ProgressPercent { get; set; }
    public bool Completed { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class XpTransaction
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int Points { get; set; }
    [MaxLength(250)] public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Badge
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(250)] public string Description { get; set; } = "";
    [MaxLength(20)] public string Icon { get; set; } = "🏅";
}

public class StudentBadge
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int BadgeId { get; set; }
    public Badge? Badge { get; set; }
    public DateTime EarnedAtUtc { get; set; } = DateTime.UtcNow;
}

public class LiveClass
{
    public int Id { get; set; }
    [MaxLength(20)] public string GroupName { get; set; } = "";
    public int ClassNumber { get; set; }
    public int CurriculumDayId { get; set; }
    public CurriculumDay? CurriculumDay { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Scheduled";
    public int CurrentActivityIndex { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
}

public class Announcement
{
    public int Id { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    [MaxLength(20)] public string? GroupName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}



public class CharacterItem
{
    public int Id { get; set; }
    [MaxLength(30)] public string Category { get; set; } = "";
    [MaxLength(60)] public string Name { get; set; } = "";
    [MaxLength(40)] public string StyleKey { get; set; } = "";
    [MaxLength(250)] public string Description { get; set; } = "";
    public int Cost { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StudentCharacter
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int? HairItemId { get; set; }
    public int? OutfitItemId { get; set; }
    public int? HatItemId { get; set; }
    public int? AccessoryItemId { get; set; }
    public int? ShoesItemId { get; set; }
    public int? PetItemId { get; set; }
    public int? HairColorItemId { get; set; }
    public int? SkinToneItemId { get; set; }
    [MaxLength(10)] public string BodyType { get; set; } = "Boy";
}

public class StudentCharacterItem
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int CharacterItemId { get; set; }
    public CharacterItem? CharacterItem { get; set; }
    public DateTime UnlockedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ScoreSetting
{
    public int Id { get; set; }
    [MaxLength(50)] public string Key { get; set; } = "";
    public int Points { get; set; }
}
