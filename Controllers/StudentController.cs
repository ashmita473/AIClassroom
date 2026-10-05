using System.Security.Claims;
using AIClassroom.Data;
using AIClassroom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIClassroom.Services;

namespace AIClassroom.Controllers;
[Authorize(Roles="Student")]
public class StudentController(AppDbContext db, GameChallengeService games) : Controller
{
    int StudentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    static bool CanAccessDay(CurriculumDay day, Student s) =>
        day.Module != null && day.IsPublished && !day.IsLocked && day.Module.GroupName == s.GroupName &&
        day.Module.ClassMin <= s.ClassNumber && day.Module.ClassMax >= s.ClassNumber;
    static bool InGroup(string groupName, Student s) => groupName == s.GroupName || groupName == "All";
    [HttpGet] public async Task<IActionResult> Index()
    {
        var s = await db.Students.FindAsync(StudentId); if (s == null) return RedirectToAction("Login","Account");
        var days = await db.CurriculumDays.Include(d=>d.Module).Where(d=>d.Module!.GroupName==s.GroupName && d.Module.ClassMin<=s.ClassNumber && d.Module.ClassMax>=s.ClassNumber && d.IsPublished).OrderBy(d=>d.DayNumber).ToListAsync();
        var progress = await db.StudentProgress.Where(p=>p.StudentId==s.Id).ToListAsync();
        ViewBag.LiveClass = await db.LiveClasses.AnyAsync(x=>x.Status=="Live"&&x.GroupName==s.GroupName&&x.ClassNumber==s.ClassNumber);
        var classRanking = await db.Students.Where(x=>x.GroupName==s.GroupName&&x.ClassNumber==s.ClassNumber&&x.IsActive).OrderByDescending(x=>x.TotalXp).ThenBy(x=>x.Name).Take(5).ToListAsync();
        var quizResults = await db.QuizResults.Where(r=>r.StudentId==s.Id).ToListAsync();
        return View(new DashboardViewModel { Student=s, TotalLessons=days.Count, CompletedLessons=progress.Count(p=>p.Completed), Accuracy=quizResults.Count==0?0:quizResults.Average(r=>r.Score), Announcements=await db.Announcements.Where(a=>a.IsActive && (a.GroupName==null||a.GroupName==s.GroupName)).OrderByDescending(a=>a.CreatedAtUtc).Take(5).ToListAsync(), NextDays=days.Where(d=>!progress.Any(p=>p.CurriculumDayId==d.Id&&p.Completed)).Take(3).ToList(), Badges=await db.Badges.Take(8).ToListAsync(), ClassRanking=classRanking });
    }
    [HttpGet] public async Task<IActionResult> Lesson(int id)
    {
        var day = await db.CurriculumDays.Include(d=>d.Module).Include(d=>d.Lesson!).ThenInclude(l=>l.Activities).Include(d=>d.Lesson!).ThenInclude(l=>l.Videos).FirstOrDefaultAsync(d=>d.Id==id); if(day==null) return NotFound();
        var s=await db.Students.FindAsync(StudentId); if(s==null || !CanAccessDay(day,s)) return Forbid();
        var videoIds=day.Lesson?.Videos.Select(v=>v.Id).ToList() ?? new List<int>();
        ViewBag.CompletedVideoIds=await db.StudentLessonVideoProgress.Where(x=>x.StudentId==s.Id && videoIds.Contains(x.LessonVideoId) && x.Completed).Select(x=>x.LessonVideoId).ToHashSetAsync();
        return View(day);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteLessonVideo(int videoId)
    {
        var video=await db.LessonVideos.Include(x=>x.Lesson!).ThenInclude(x=>x.CurriculumDay!).ThenInclude(x=>x.Module).FirstOrDefaultAsync(x=>x.Id==videoId && x.IsPublished);
        var s=await db.Students.FindAsync(StudentId);
        var day=video?.Lesson?.CurriculumDay;
        if(video==null || s==null || day==null || !CanAccessDay(day,s)) return NotFound();
        var progress=await db.StudentLessonVideoProgress.FirstOrDefaultAsync(x=>x.StudentId==s.Id && x.LessonVideoId==video.Id);
        if(progress?.Completed==true) return Json(new { ok=true, already=true, xp=0 });
        progress ??= new StudentLessonVideoProgress{StudentId=s.Id,LessonVideoId=video.Id};
        progress.Completed=true; progress.CompletedAtUtc=DateTime.UtcNow;
        if(progress.Id==0) db.StudentLessonVideoProgress.Add(progress);
        var xp=Math.Max(0,video.XpReward);
        if(xp>0){s.TotalXp+=xp;s.XpBalance+=xp;db.XpTransactions.Add(new XpTransaction{StudentId=s.Id,Points=xp,Reason=$"Watched lesson video: {video.Title}"});}
        await db.SaveChangesAsync();
        return Json(new { ok=true, already=false, xp });
    }

    [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> CompleteLesson(int id)
    {
        var day=await db.CurriculumDays.Include(d=>d.Module).Include(d=>d.Lesson!).ThenInclude(l=>l.Videos).FirstOrDefaultAsync(d=>d.Id==id); var s=await db.Students.FindAsync(StudentId); if(day==null||s==null) return NotFound(); if(!CanAccessDay(day,s)) return Forbid();
        var requiredIds=day.Lesson?.Videos.Where(v=>v.IsPublished && v.IsRequired).Select(v=>v.Id).ToList() ?? new List<int>();
        if(requiredIds.Count>0){var watched=await db.StudentLessonVideoProgress.Where(x=>x.StudentId==s.Id&&requiredIds.Contains(x.LessonVideoId)&&x.Completed).Select(x=>x.LessonVideoId).ToListAsync();if(watched.Count!=requiredIds.Count){TempData["LessonVideoRequired"]="Please watch all required lesson videos before completing this mission.";return RedirectToAction(nameof(Lesson),new{id});}}
        var p=await db.StudentProgress.FirstOrDefaultAsync(x=>x.StudentId==s.Id&&x.CurriculumDayId==id) ?? new StudentProgress{StudentId=s.Id,CurriculumDayId=id};
        if(p.Id==0){db.StudentProgress.Add(p);}
        if(!p.Completed){p.Completed=true;p.ProgressPercent=100;p.CompletedAtUtc=DateTime.UtcNow;s.TotalXp+=day.XpReward;s.XpBalance+=day.XpReward;db.XpTransactions.Add(new XpTransaction{StudentId=s.Id,Points=day.XpReward,Reason=$"Completed Day {day.DayNumber}: {day.Title}"});
            await AwardBadge(s.Id,"First Lesson");
            if(day.DayNumber>=10) await AwardBadge(s.Id,"AI Explorer");
            if(day.Title.Contains("Algorithm",StringComparison.OrdinalIgnoreCase)) await AwardBadge(s.Id,"Algorithm Master");
            if(day.Title.Contains("Creative",StringComparison.OrdinalIgnoreCase)) await AwardBadge(s.Id,"AI Creator");}
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    [HttpGet]
    public async Task<IActionResult> Notes()
    {
        var notes = await db.StudentNotes.Where(n => n.StudentId == StudentId)
            .OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.UpdatedAtUtc).ToListAsync();
        ViewBag.TeacherNotes = await db.TeacherNotes.OrderByDescending(x => x.Id).ToListAsync();
        return View(notes);
    }

    [HttpGet]
    public IActionResult NewNote(int? dayId = null) => View(new NoteEditViewModel { CurriculumDayId = dayId });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewNote(NoteEditViewModel m)
    {
        if (!ModelState.IsValid) return View(m);
        db.StudentNotes.Add(new StudentNote { StudentId = StudentId, CurriculumDayId = m.CurriculumDayId, Title = m.Title.Trim(), Content = m.Content.Trim(), IsPinned = m.IsPinned });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Notes));
    }

    [HttpGet]
    public async Task<IActionResult> EditNote(int id)
    {
        var n = await db.StudentNotes.FirstOrDefaultAsync(x => x.Id == id && x.StudentId == StudentId);
        if (n == null) return NotFound();
        return View(new NoteEditViewModel { Id = n.Id, CurriculumDayId = n.CurriculumDayId, Title = n.Title, Content = n.Content, IsPinned = n.IsPinned });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditNote(NoteEditViewModel m)
    {
        if (!ModelState.IsValid) return View(m);
        var n = await db.StudentNotes.FirstOrDefaultAsync(x => x.Id == m.Id && x.StudentId == StudentId);
        if (n == null) return NotFound();
        n.Title = m.Title.Trim(); n.Content = m.Content.Trim(); n.IsPinned = m.IsPinned; n.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Notes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PinNote(int id)
    {
        var n = await db.StudentNotes.FirstOrDefaultAsync(x => x.Id == id && x.StudentId == StudentId);
        if (n == null) return NotFound();
        n.IsPinned = !n.IsPinned; n.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Notes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var n = await db.StudentNotes.FirstOrDefaultAsync(x => x.Id == id && x.StudentId == StudentId);
        if (n != null) { db.StudentNotes.Remove(n); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Notes));
    }

    [HttpGet] public async Task<IActionResult> Games()
    {
        var s = await db.Students.FindAsync(StudentId);
        if (s == null) return RedirectToAction("Login", "Account");
        var games = await db.Games.Where(g => g.IsActive && (g.GroupName == s.GroupName || g.GroupName == "All")).ToListAsync();
        ViewBag.TotalXp = s.TotalXp;
        ViewBag.GamePoints = await db.GameResults.Where(r => r.StudentId == s.Id).SumAsync(r => (int?)r.Score) ?? 0;
        ViewBag.GamesPlayed = await db.GameResults.CountAsync(r => r.StudentId == s.Id);
        ViewBag.BadgesCount = await db.StudentBadges.CountAsync(b => b.StudentId == s.Id);
        return View(games);
    }
    [HttpGet] public async Task<IActionResult> Game(int id)
    {
        var s = await db.Students.FindAsync(StudentId);
        var g = await db.Games.FirstOrDefaultAsync(x => x.Id == id && x.IsActive && (x.GroupName == s!.GroupName || x.GroupName == "All"));
        if (s == null || g == null) return NotFound();
        var played = await db.GameResults.Where(x => x.StudentId == s.Id && x.GameId == g.Id).OrderByDescending(x => x.PlayedAtUtc).ToListAsync();
        ViewBag.Attempts = played.Count;
        ViewBag.BestScore = played.Count == 0 ? 0 : played.Max(x => x.Score);
        // Seed is kept server-side for this attempt; the same shuffle is rebuilt when the answers are submitted.
        var seed = Random.Shared.Next();
        HttpContext.Session.SetInt32($"game:{g.Id}", seed);
        var challenge = games.Create(g, seed);
        return View(new GamePlayViewModel
        {
            Game = g,
            Questions = challenge.Select(q => new GameChallengeQuestionViewModel { Prompt = q.Prompt, Options = q.Options.ToList() }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlayGame(int id, List<int?>? answers)
    {
        var s = await db.Students.FindAsync(StudentId);
        var g = await db.Games.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (s == null || g == null || !InGroup(g.GroupName, s)) return NotFound();

        var seed = HttpContext.Session.GetInt32($"game:{g.Id}");
        if (seed == null)
        {
            TempData["GameResult"] = "Please open the mission and start it before submitting.";
            return RedirectToAction(nameof(Games));
        }
        HttpContext.Session.Remove($"game:{g.Id}"); // one submission per started attempt
        var challenge = games.Create(g, seed.Value);
        var finalScore = games.Score(answers ?? [], challenge, out _);
        var configuredGameXp = await db.ScoreSettings.Where(x => x.Key == "GameCompleted").Select(x => (int?)x.Points).FirstOrDefaultAsync() ?? 50;
        var completedXp = g.BaseXp > 0 ? g.BaseXp : configuredGameXp;
        var perfectXp = await db.ScoreSettings.Where(x => x.Key == "PerfectGame").Select(x => (int?)x.Points).FirstOrDefaultAsync() ?? 100;

        var previous = await db.GameResults.Where(x => x.StudentId == s.Id && x.GameId == g.Id).Select(x => x.Score).ToListAsync();
        var xp = 0;
        if (previous.Count == 0) xp += completedXp;
        if (finalScore >= 100 && !previous.Any(p => p >= 100)) xp += perfectXp;

        db.GameResults.Add(new GameResult { StudentId = s.Id, GameId = g.Id, Score = finalScore, XpEarned = xp });
        if (xp > 0)
        {
            s.TotalXp += xp;
            s.XpBalance += xp;
            db.XpTransactions.Add(new XpTransaction { StudentId = s.Id, Points = xp, Reason = $"Completed game: {g.Name}" });
        }

        await AwardBadge(s.Id, "First Game");
        if (g.Name.Contains("Pattern", StringComparison.OrdinalIgnoreCase)) await AwardBadge(s.Id, "Pattern Detective");
        if (g.Name.Contains("Robot", StringComparison.OrdinalIgnoreCase)) await AwardBadge(s.Id, "Robot Trainer");
        if (finalScore >= 100) await AwardBadge(s.Id, "Perfect Score");

        await db.SaveChangesAsync();
        TempData["GameResult"] = xp > 0 ? $"{g.Name}: {finalScore}% — +{xp} XP" : $"{g.Name}: {finalScore}% — replay (no extra XP)";
        return RedirectToAction(nameof(Games));
    }

    [HttpGet]
    public async Task<IActionResult> Quizzes()
    {
        var s = await db.Students.FindAsync(StudentId);
        if (s == null) return RedirectToAction("Login", "Account");

        var quizzes = await db.Quizzes
            .Where(q => q.IsPublished && (q.GroupName == s.GroupName || q.GroupName == "All"))
            .Include(q => q.Questions)
            .OrderBy(q => q.Id)
            .ToListAsync();

        ViewBag.Attempts = await db.QuizResults
            .Where(r => r.StudentId == s.Id)
            .GroupBy(r => r.QuizId)
            .Select(g => new { QuizId = g.Key, Count = g.Count(), Best = g.Max(x => x.Score) })
            .ToDictionaryAsync(x => x.QuizId, x => x);

        return View(quizzes);
    }

    [HttpGet]
    public async Task<IActionResult> TakeQuiz(int id)
    {
        var s = await db.Students.FindAsync(StudentId);
        var q = await db.Quizzes
            .Include(x => x.Questions).ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished &&
                (x.GroupName == s!.GroupName || x.GroupName == "All"));

        if (s == null || q == null) return NotFound();

        var attempts = await db.QuizResults.CountAsync(x => x.StudentId == s.Id && x.QuizId == q.Id);
        if (attempts >= q.MaxAttempts)
        {
            TempData["Result"] = $"No attempts left for {q.Title}.";
            return RedirectToAction(nameof(Quizzes));
        }

        // Do not start the timer when the quiz page is opened.
        // The timer starts only after the student explicitly clicks Start Quiz.
        return View(new AssessmentTakeViewModel
        {
            Id = q.Id,
            Title = q.Title,
            AssessmentKind = "Quiz",
            TimeLimitMinutes = q.TimeLimitMinutes,
            PassingScore = q.PassingScore,
            MaxAttempts = q.MaxAttempts,
            ShowInstantResults = q.ShowInstantResults,
            StartedAtUnixMs = 0,
            QuestionIds = new List<int>(),
            Questions = new List<Question>()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TakeQuiz(AssessmentTakeViewModel m)
    {
        var s = await db.Students.FindAsync(StudentId);
        var q = await db.Quizzes.Include(x => x.Questions).ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == m.Id && x.IsPublished && (x.GroupName == s!.GroupName || x.GroupName == "All"));

        if (s == null || q == null) return NotFound();

        var attempts = await db.QuizResults.CountAsync(x => x.StudentId == s.Id && x.QuizId == q.Id);
        if (attempts >= q.MaxAttempts) return RedirectToAction(nameof(Quizzes));

        // Explicit start action: select/randomize questions and begin the timer now.
        // The start time and question set are stored server-side so the client cannot extend the timer or submit a subset.
        if (m.Start)
        {
            var startQuestions = q.Questions.OrderBy(x => x.SortOrder).ToList();
            if (q.RandomizeQuestions) startQuestions = startQuestions.OrderBy(_ => Random.Shared.Next()).ToList();
            var startedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            HttpContext.Session.SetString($"quiz:{q.Id}", $"{startedMs}|{string.Join(',', startQuestions.Select(x => x.Id))}");

            return View(new AssessmentTakeViewModel
            {
                Id = q.Id,
                Title = q.Title,
                AssessmentKind = "Quiz",
                TimeLimitMinutes = q.TimeLimitMinutes,
                PassingScore = q.PassingScore,
                MaxAttempts = q.MaxAttempts,
                ShowInstantResults = q.ShowInstantResults,
                StartedAtUnixMs = startedMs,
                QuestionIds = startQuestions.Select(x => x.Id).ToList(),
                Questions = startQuestions
            });
        }

        // Submission must match a server-side started attempt; the posted ids/time are ignored.
        if (!TryReadAttempt(HttpContext.Session.GetString($"quiz:{q.Id}"), out var startedAtMs, out var selectedIds))
        {
            TempData["Result"] = "Please press Start Quiz before submitting.";
            return RedirectToAction(nameof(Quizzes));
        }
        HttpContext.Session.Remove($"quiz:{q.Id}"); // one submission per started attempt
        var questions = q.Questions.Where(x => selectedIds.Contains(x.Id)).OrderBy(x => x.SortOrder).ToList();
        if (questions.Count == 0) return RedirectToAction(nameof(Quizzes));

        var elapsed = (int)Math.Max(0, (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startedAtMs) / 1000);
        // Small grace period for network latency; later submissions are scored as time-expired (late answers still count as skipped below).
        var lateByTimeLimit = q.TimeLimitMinutes > 0 && elapsed > q.TimeLimitMinutes * 60 + 30;
        if (q.TimeLimitMinutes > 0) elapsed = Math.Min(elapsed, q.TimeLimitMinutes * 60);

        var correct = 0;
        var skipped = 0;
        var answers = new List<AssessmentAnswer>();

        foreach (var question in questions)
        {
            m.Answers.TryGetValue(question.Id, out var answer);
            if (lateByTimeLimit) answer = null;
            var isCorrect = EvaluateAnswer(question, answer);

            if (string.IsNullOrWhiteSpace(answer)) skipped++;
            else if (isCorrect) correct++;

            answers.Add(new AssessmentAnswer
            {
                QuestionId = question.Id,
                IsCorrect = isCorrect,
                GivenAnswer = answer ?? "",
                Topic = question.Topic,
                PointsEarned = isCorrect ? question.Points : 0
            });
        }

        var total = questions.Count;
        var wrong = Math.Max(0, total - correct - skipped);
        var score = total == 0 ? 0 : (int)Math.Round(correct * 100d / total);
        var passed = score >= q.PassingScore;
        // Anti-farming: XP is only paid for improving on the student's previous best correct-answer count for this quiz.
        var bestPriorCorrect = attempts == 0 ? 0 : await db.QuizResults.Where(x => x.StudentId == s.Id && x.QuizId == q.Id).MaxAsync(x => x.Correct);
        var xp = Math.Max(0, correct - bestPriorCorrect) * 10;

        var result = new QuizResult
        {
            StudentId = s.Id, QuizId = q.Id, Score = score, Correct = correct,
            Wrong = wrong, Skipped = skipped, XpEarned = xp,
            TimeTakenSeconds = elapsed, Passed = passed
        };
        db.QuizResults.Add(result);
        await db.SaveChangesAsync();

        foreach (var a in answers) a.QuizResultId = result.Id;
        db.AssessmentAnswers.AddRange(answers);

        s.TotalXp += xp;
        s.XpBalance += xp;
        db.XpTransactions.Add(new XpTransaction
        {
            StudentId = s.Id, Points = xp, Reason = $"Quiz: {q.Title}"
        });

        if (score == 100) await AwardBadge(s.Id, "Quiz Master");
        if (passed) await AwardBadge(s.Id, "Quiz Finisher");
        await db.SaveChangesAsync();

        if (!q.ShowInstantResults)
        {
            TempData["Result"] = $"{q.Title}: submission recorded. Your teacher has disabled instant results.";
            return RedirectToAction(nameof(Quizzes));
        }
        return RedirectToAction(nameof(QuizResult), new { id = result.Id });
    }

    [HttpGet]
    public async Task<IActionResult> QuizResult(int id)
    {
        var r = await db.QuizResults.Include(x => x.Quiz).FirstOrDefaultAsync(x => x.Id == id && x.StudentId == StudentId);
        if (r == null) return NotFound();
        return View(await BuildResultViewModel(r));
    }

    [HttpGet]
    public async Task<IActionResult> Scorecard()
    {
        var s = await db.Students.FindAsync(StudentId);
        if (s == null) return NotFound();

        ViewBag.Games = await db.GameResults.Where(x => x.StudentId == s.Id).CountAsync();
        ViewBag.Tests = await db.TestResults.Where(x => x.StudentId == s.Id).CountAsync();
        ViewBag.Quizzes = await db.QuizResults.Where(x => x.StudentId == s.Id).CountAsync();
        ViewBag.LessonsCompleted = await db.StudentProgress.CountAsync(x => x.StudentId == s.Id && x.Completed);
        ViewBag.TotalLessons = await db.CurriculumDays.Include(x => x.Module)
            .CountAsync(x => x.Module!.GroupName == s.GroupName && x.Module.ClassMin <= s.ClassNumber && x.Module.ClassMax >= s.ClassNumber && x.IsPublished);
        ViewBag.Badges = await db.StudentBadges.Where(x => x.StudentId == s.Id).Include(x => x.Badge).ToListAsync();
        ViewBag.XpHistory = await db.XpTransactions.Where(x => x.StudentId == s.Id).OrderByDescending(x => x.CreatedAtUtc).Take(15).ToListAsync();

        var assessmentAnswers = await db.AssessmentAnswers.Where(x =>
            x.QuizResultId != null && db.QuizResults.Any(r => r.Id == x.QuizResultId && r.StudentId == s.Id) ||
            x.TestResultId != null && db.TestResults.Any(r => r.Id == x.TestResultId && r.StudentId == s.Id))
            .ToListAsync();

        ViewBag.Accuracy = assessmentAnswers.Count == 0 ? 0 : Math.Round(assessmentAnswers.Count(x => x.IsCorrect) * 100d / assessmentAnswers.Count);
        ViewBag.TopicPerformance = assessmentAnswers
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Topic) ? "General" : x.Topic)
            .Select(g => new { Topic = g.Key, Accuracy = (int)Math.Round(g.Count(x => x.IsCorrect) * 100d / (double)g.Count()) })
            .OrderByDescending(x => x.Accuracy).ToList();

        return View(s);
    }

    internal static bool TryReadAttempt(string? raw, out long startedAtMs, out HashSet<int> ids)
    {
        startedAtMs = 0; ids = new HashSet<int>();
        if (string.IsNullOrEmpty(raw)) return false;
        var parts = raw.Split('|', 2);
        if (parts.Length != 2 || !long.TryParse(parts[0], out startedAtMs)) return false;
        foreach (var p in parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries))
            if (int.TryParse(p, out var id)) ids.Add(id);
        return ids.Count > 0;
    }

    private async Task<AssessmentResultViewModel> BuildResultViewModel(QuizResult r)
    {
        var answers = await db.AssessmentAnswers.Where(x => x.QuizResultId == r.Id).ToListAsync();
        return BuildResult(r.Quiz.Title, "Quiz", r.Score, r.Correct, r.Wrong, r.Skipped,
            r.TimeTakenSeconds, r.Passed, r.XpEarned, r.Quiz.PassingScore, answers);
    }

    private static AssessmentResultViewModel BuildResult(string title, string kind, int score, int correct, int wrong,
        int skipped, int seconds, bool passed, int xp, int passingScore, List<AssessmentAnswer> answers)
    {
        var topics = answers.GroupBy(x => string.IsNullOrWhiteSpace(x.Topic) ? "General" : x.Topic)
            .ToDictionary(g => g.Key, g => (int)Math.Round(g.Count(x => x.IsCorrect) * 100d / (double)g.Count()));

        return new AssessmentResultViewModel
        {
            Title = title, AssessmentKind = kind, Score = score, Correct = correct,
            Wrong = wrong, Skipped = skipped, TimeTakenSeconds = seconds,
            Passed = passed, XpEarned = xp, PassingScore = passingScore,
            TopicPerformance = topics,
            StrongAreas = topics.Where(x => x.Value >= 80).OrderByDescending(x => x.Value).Select(x => (x.Key, x.Value)).ToList(),
            NeedsRevision = topics.Where(x => x.Value < 70).OrderBy(x => x.Value).Select(x => (x.Key, x.Value)).ToList()
        };
    }

    private static bool EvaluateAnswer(Question q, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        raw = raw.Trim();

        if (q.QuestionType is "MCQ" or "TrueFalse" or "Image")
            return q.Options.Any(o => o.IsCorrect && string.Equals(o.Text.Trim(), raw, StringComparison.OrdinalIgnoreCase));

        if (q.QuestionType == "ShortAnswer" || q.QuestionType == "Scenario")
        {
            var accepted = (q.Answer ?? "").Split(new[] { '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return accepted.Any(a => Normalize(a) == Normalize(raw));
        }

        if (q.QuestionType == "Match")
        {
            try
            {
                var submitted = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(raw) ?? new();
                var expected = q.Options.ToDictionary(o => o.Text, o => o.MatchKey ?? "", StringComparer.OrdinalIgnoreCase);
                return expected.Count > 0 && expected.Count == submitted.Count &&
                       expected.All(x => submitted.Any(s => string.Equals(s.Key, x.Key, StringComparison.OrdinalIgnoreCase) &&
                                                           string.Equals(s.Value, x.Value, StringComparison.OrdinalIgnoreCase)));
            }
            catch { return false; }
        }

        if (q.QuestionType == "Ordering")
        {
            try
            {
                var submitted = System.Text.Json.JsonSerializer.Deserialize<List<string>>(raw) ?? new();
                var expected = q.Options.OrderBy(o => o.OrderIndex).Select(o => o.Text).ToList();
                return submitted.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        return false;
    }

    private static string Normalize(string value) =>
        string.Join(" ", value.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    [HttpGet]
    public async Task<IActionResult> Assignments()
    {
        var s=await db.Students.FindAsync(StudentId);
        if(s==null) return RedirectToAction("Login","Account");
        var assignments=await db.Assignments.Where(a=>a.IsPublished&&(a.GroupName==s.GroupName||a.GroupName=="All")).OrderBy(a=>a.DueDateUtc==null).ThenBy(a=>a.DueDateUtc).ThenByDescending(a=>a.Id).ToListAsync();
        var submissions=await db.Submissions.Where(x=>x.StudentId==s.Id).ToDictionaryAsync(x=>x.AssignmentId);
        ViewBag.Submissions=submissions;
        return View(assignments);
    }

    [HttpGet]
    public async Task<IActionResult> SubmitAssignment(int id)
    {
        var s=await db.Students.FindAsync(StudentId);
        var a=await db.Assignments.FirstOrDefaultAsync(x=>x.Id==id&&x.IsPublished);
        if(a==null||s==null||!InGroup(a.GroupName,s))return NotFound();
        var existing=await db.Submissions.FirstOrDefaultAsync(x=>x.AssignmentId==id&&x.StudentId==s.Id);
        return View(new AssignmentSubmitViewModel{AssignmentId=id,Content=existing?.Content??""});
    }
    [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> SubmitAssignment(AssignmentSubmitViewModel m)
    {
        var s=await db.Students.FindAsync(StudentId);
        var a=await db.Assignments.FirstOrDefaultAsync(x=>x.Id==m.AssignmentId&&x.IsPublished);
        if(a==null||s==null||!InGroup(a.GroupName,s))return NotFound();
        var content=(m.Content??"").Trim();
        if(content.Length==0||content.Length>10000){ModelState.AddModelError(nameof(m.Content),"Please write between 1 and 10,000 characters.");return View(m);}
        var existing=await db.Submissions.FirstOrDefaultAsync(x=>x.AssignmentId==a.Id&&x.StudentId==s.Id);
        if(existing!=null){ TempData["Result"]="This assignment has already been submitted. Your latest saved response is shown in the assignment status."; return RedirectToAction(nameof(Assignments)); }
        db.Submissions.Add(new Submission{AssignmentId=a.Id,StudentId=s.Id,Content=content});
        s.TotalXp+=a.XpReward;s.XpBalance+=a.XpReward;
        db.XpTransactions.Add(new XpTransaction{StudentId=s.Id,Points=a.XpReward,Reason=$"Assignment: {a.Title}"});
        await db.SaveChangesAsync();
        TempData["Result"]="Assignment submitted successfully.";
        return RedirectToAction(nameof(Assignments));
    }

    [HttpGet]
    public async Task<IActionResult> Customize()
    {
        var student = await db.Students.FindAsync(StudentId);
        if (student == null) return RedirectToAction("Login", "Account");

        var character = await db.StudentCharacters.FirstOrDefaultAsync(x => x.StudentId == student.Id)
            ?? new StudentCharacter { StudentId = student.Id, BodyType = "Boy" };

        var items = await db.CharacterItems.Where(x => x.IsActive).OrderBy(x => x.Category).ThenBy(x => x.Cost).ToListAsync();
        var unlocked = await db.StudentCharacterItems.Where(x => x.StudentId == student.Id).Select(x => x.CharacterItemId).ToListAsync();

        return View(new CharacterCustomizeViewModel
        {
            Student = student,
            Character = character,
            Items = items,
            UnlockedItemIds = unlocked.ToHashSet()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Customize(string bodyType, string category, int itemId)
    {
        var student = await db.Students.FindAsync(StudentId);
        var item = await db.CharacterItems.FindAsync(itemId);
        if (student == null || item == null || !item.IsActive) return NotFound();

        var character = await db.StudentCharacters.FirstOrDefaultAsync(x => x.StudentId == student.Id);
        if (character == null)
        {
            character = new StudentCharacter { StudentId = student.Id, BodyType = "Boy" };
            db.StudentCharacters.Add(character);
        }

        if (category != "Body" && !string.Equals(item.Category, category, StringComparison.Ordinal)) return BadRequest();
        if (category == "Body")
        {
            character.BodyType = bodyType is "Boy" or "Girl" ? bodyType : "Boy";
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Customize));
        }

        var owned = await db.StudentCharacterItems.AnyAsync(x => x.StudentId == student.Id && x.CharacterItemId == item.Id);
        if (!owned)
        {
            if (student.XpBalance < item.Cost)
            {
                TempData["CharacterMessage"] = $"You need {item.Cost - student.XpBalance} more XP to unlock {item.Name}.";
                return RedirectToAction(nameof(Customize));
            }

            student.XpBalance -= item.Cost;
            db.XpTransactions.Add(new XpTransaction
            {
                StudentId = student.Id,
                Points = -item.Cost,
                Reason = $"Character item unlocked: {item.Name}"
            });
            db.StudentCharacterItems.Add(new StudentCharacterItem { StudentId = student.Id, CharacterItemId = item.Id });
        }

        switch (category)
        {
            case "Skin": character.SkinToneItemId = item.Id; break;
            case "Hair": character.HairItemId = item.Id; break;
            case "HairColor": character.HairColorItemId = item.Id; break;
            case "Outfit": character.OutfitItemId = item.Id; break;
            case "Hat": character.HatItemId = item.Id; break;
            case "Accessory": character.AccessoryItemId = item.Id; break;
            case "Shoes": character.ShoesItemId = item.Id; break;
            case "Pet": character.PetItemId = item.Id; break;
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Customize));
    }

    [HttpGet] public async Task<IActionResult> Leaderboard(){var s=await db.Students.FindAsync(StudentId);var rows=await db.Students.Where(x=>x.GroupName==s!.GroupName&&x.ClassNumber==s.ClassNumber&&x.IsActive).OrderByDescending(x=>x.TotalXp).ThenBy(x=>x.Name).Take(30).ToListAsync();return View(rows);}
    private async Task AwardBadge(int studentId, string badgeName)
    {
        var badge=await db.Badges.FirstOrDefaultAsync(b=>b.Name==badgeName);
        if(badge!=null && !await db.StudentBadges.AnyAsync(x=>x.StudentId==studentId&&x.BadgeId==badge.Id)) db.StudentBadges.Add(new StudentBadge{StudentId=studentId,BadgeId=badge.Id});
    }
}