namespace AIClassroom.Models;

public class StudentLoginViewModel { public string Name { get; set; } = ""; public string RollNumber { get; set; } = ""; public string Pin { get; set; } = ""; public int ClassNumber { get; set; } = 3; }
public class AdminLoginViewModel { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
public class DashboardViewModel { public Student Student { get; set; } = new(); public int CompletedLessons { get; set; } public int TotalLessons { get; set; } public double Accuracy { get; set; } public List<Announcement> Announcements { get; set; } = new(); public List<CurriculumDay> NextDays { get; set; } = new(); public List<Badge> Badges { get; set; } = new(); public List<Student> ClassRanking { get; set; } = new(); }
public class AdminDashboardViewModel { public int Students { get; set; } public int Teachers { get; set; } public int PublishedLessons { get; set; } public int Games { get; set; } public int Quizzes { get; set; } public int Tests { get; set; } public int Submissions { get; set; } public List<Student> RecentStudents { get; set; } = new(); public List<Announcement> Announcements { get; set; } = new(); }
public class QuizTakeViewModel { public Quiz Quiz { get; set; } = new(); public Dictionary<int,string> Answers { get; set; } = new(); }
public class NoteEditViewModel { public int? Id { get; set; } public int? CurriculumDayId { get; set; } public string Title { get; set; } = ""; public string Content { get; set; } = ""; public bool IsPinned { get; set; } }
public class TeacherNoteEditViewModel
{
    public int? Id { get; set; }
    public int? CurriculumDayId { get; set; }
    public string Category { get; set; } = "Pre-written";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
}

public class AssignmentSubmitViewModel { public int AssignmentId { get; set; } public string Content { get; set; } = ""; }
public class AssignmentEditViewModel
{
    public int? Id { get; set; }
    public string Title { get; set; } = "";
    public string Instructions { get; set; } = "";
    public string GroupName { get; set; } = "A";
    public int? CurriculumDayId { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public int XpReward { get; set; } = 50;
    public bool IsPublished { get; set; }
}

public class AssignmentGradeViewModel
{
    public int SubmissionId { get; set; }
    public string StudentName { get; set; } = "";
    public string AssignmentTitle { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime SubmittedAtUtc { get; set; }
    public int? Marks { get; set; }
    public string Feedback { get; set; } = "";
}



public class CharacterCustomizeViewModel
{
    public Student Student { get; set; } = new();
    public StudentCharacter Character { get; set; } = new();
    public List<CharacterItem> Items { get; set; } = new();
    public HashSet<int> UnlockedItemIds { get; set; } = new();
}


public class AssessmentTakeViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string AssessmentKind { get; set; } = "Quiz";
    public int TimeLimitMinutes { get; set; }
    public int PassingScore { get; set; }
    public int MaxAttempts { get; set; }
    public bool ShowInstantResults { get; set; } = true;
    public long StartedAtUnixMs { get; set; }
    public bool Start { get; set; }
    public List<int> QuestionIds { get; set; } = new();
    public List<Question> Questions { get; set; } = new();
    public Dictionary<int,string> Answers { get; set; } = new();
}

public class AssessmentResultViewModel
{
    public string Title { get; set; } = "";
    public string AssessmentKind { get; set; } = "Quiz";
    public int Score { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public int Skipped { get; set; }
    public int TimeTakenSeconds { get; set; }
    public bool Passed { get; set; }
    public int XpEarned { get; set; }
    public int PassingScore { get; set; }
    public Dictionary<string, int> TopicPerformance { get; set; } = new();
    public List<(string Topic, int Accuracy)> StrongAreas { get; set; } = new();
    public List<(string Topic, int Accuracy)> NeedsRevision { get; set; } = new();
}

public class QuestionEditorViewModel
{
    public string ParentType { get; set; } = "Quiz";
    public int ParentId { get; set; }
    public int? QuestionId { get; set; }
    public string QuestionType { get; set; } = "MCQ";
    public string Topic { get; set; } = "General";
    public string Prompt { get; set; } = "";
    public string? ImagePath { get; set; }
    public Microsoft.AspNetCore.Http.IFormFile? ImageFile { get; set; }
    public string? Answer { get; set; }
    public string? Explanation { get; set; }
    public int Points { get; set; } = 10;
    public int SortOrder { get; set; }
    public bool IsBanked { get; set; } = true;
    public string Difficulty { get; set; } = "Easy";
    public string OptionsText { get; set; } = "";
    public string CorrectOption { get; set; } = "";
}



public class TeacherProfileViewModel
{
    public string Name { get; set; } = "";
    public string Username { get; set; } = "";
    public string Role { get; set; } = "Teacher";
    public string Department { get; set; } = "";
    public string Bio { get; set; } = "";
    public string ProfileImagePath { get; set; } = "";
    public Microsoft.AspNetCore.Http.IFormFile? ProfileImage { get; set; }
}

public class ChangePasswordViewModel
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}

public class ChangeStudentPinViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string RollNumber { get; set; } = "";
    public int ClassNumber { get; set; }
    public string NewPin { get; set; } = "";
    public string ConfirmPin { get; set; } = "";
}

public class ResetTeacherPasswordViewModel
{
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = "";
    public string Username { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}

public class GamePlayViewModel
{
    public Game Game { get; set; } = new();
    public List<GameChallengeQuestionViewModel> Questions { get; set; } = new();
}

public class GameChallengeQuestionViewModel
{
    public string Prompt { get; set; } = "";
    public List<string> Options { get; set; } = new();
}
