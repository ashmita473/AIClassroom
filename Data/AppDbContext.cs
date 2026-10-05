using AIClassroom.Models;
using Microsoft.EntityFrameworkCore;

namespace AIClassroom.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<CurriculumModule> CurriculumModules => Set<CurriculumModule>();
    public DbSet<CurriculumDay> CurriculumDays => Set<CurriculumDay>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<LessonVideo> LessonVideos => Set<LessonVideo>();
    public DbSet<StudentLessonVideoProgress> StudentLessonVideoProgress => Set<StudentLessonVideoProgress>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameResult> GameResults => Set<GameResult>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizResult> QuizResults => Set<QuizResult>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<TestResult> TestResults => Set<TestResult>();
    public DbSet<AssessmentAnswer> AssessmentAnswers => Set<AssessmentAnswer>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<StudentNote> StudentNotes => Set<StudentNote>();
    public DbSet<TeacherNote> TeacherNotes => Set<TeacherNote>();
    public DbSet<StudentProgress> StudentProgress => Set<StudentProgress>();
    public DbSet<XpTransaction> XpTransactions => Set<XpTransaction>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<StudentBadge> StudentBadges => Set<StudentBadge>();
    public DbSet<LiveClass> LiveClasses => Set<LiveClass>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<ScoreSetting> ScoreSettings => Set<ScoreSetting>();
    public DbSet<CharacterItem> CharacterItems => Set<CharacterItem>();
    public DbSet<StudentCharacter> StudentCharacters => Set<StudentCharacter>();
    public DbSet<StudentCharacterItem> StudentCharacterItems => Set<StudentCharacterItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Teacher>().HasIndex(x => x.Username).IsUnique();
        b.Entity<Student>().HasIndex(x => new { x.Name, x.RollNumber, x.ClassNumber }).IsUnique();
        b.Entity<CurriculumModule>().HasIndex(x => new { x.GroupName, x.Code }).IsUnique();
        b.Entity<CurriculumDay>().HasIndex(x => new { x.CurriculumModuleId, x.DayNumber }).IsUnique();
        b.Entity<Lesson>().HasIndex(x => x.CurriculumDayId).IsUnique();
        b.Entity<LessonVideo>().HasIndex(x => new { x.LessonId, x.SortOrder });
        b.Entity<StudentLessonVideoProgress>().HasIndex(x => new { x.StudentId, x.LessonVideoId }).IsUnique();
        b.Entity<StudentLessonVideoProgress>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<StudentLessonVideoProgress>().HasOne(x => x.LessonVideo).WithMany().HasForeignKey(x => x.LessonVideoId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ScoreSetting>().HasIndex(x => x.Key).IsUnique();
        b.Entity<StudentBadge>().HasIndex(x => new { x.StudentId, x.BadgeId }).IsUnique();
        b.Entity<LiveClass>().HasOne(x => x.CurriculumDay).WithMany().HasForeignKey(x => x.CurriculumDayId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StudentBadge>().HasOne(x => x.Badge).WithMany().HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<StudentBadge>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<StudentCharacter>().HasIndex(x => x.StudentId).IsUnique();
        b.Entity<StudentCharacter>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<StudentCharacterItem>().HasIndex(x => new { x.StudentId, x.CharacterItemId }).IsUnique();
        b.Entity<StudentCharacterItem>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<StudentCharacterItem>().HasOne(x => x.CharacterItem).WithMany().HasForeignKey(x => x.CharacterItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
