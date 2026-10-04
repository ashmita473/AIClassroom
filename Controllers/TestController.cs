using System.Security.Claims;
using AIClassroom.Data;
using AIClassroom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIClassroom.Controllers;

[Authorize(Roles="Student")]
public class TestController(AppDbContext db) : Controller
{
    int StudentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var s = await db.Students.FindAsync(StudentId);
        if (s == null) return RedirectToAction("Login", "Account");

        var tests = await db.Tests
            .Where(t => t.IsPublished && (t.GroupName == s.GroupName || t.GroupName == "All"))
            .Include(t => t.Questions)
            .OrderBy(x => x.Id)
            .ToListAsync();

        ViewBag.Attempts = await db.TestResults
            .Where(r => r.StudentId == s.Id)
            .GroupBy(r => r.TestId)
            .Select(g => new { TestId = g.Key, Count = g.Count(), Best = g.Max(x => x.Score) })
            .ToDictionaryAsync(x => x.TestId, x => x);

        return View(tests);
    }

    [HttpGet]
    public async Task<IActionResult> Take(int id)
    {
        var s = await db.Students.FindAsync(StudentId);
        var test = await db.Tests
            .Include(t => t.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(t => t.Id == id && t.IsPublished &&
                (t.GroupName == s!.GroupName || t.GroupName == "All"));

        if (s == null || test == null) return NotFound();

        var questions = test.Questions.OrderBy(q => q.SortOrder).ToList();
        if (test.RandomizeQuestions) questions = questions.OrderBy(_ => Random.Shared.Next()).ToList();
        var startedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // Refreshing the page must not restart the clock: reuse an unexpired server-side start time.
        if (StudentController.TryReadAttempt(HttpContext.Session.GetString($"test:{test.Id}"), out var priorStart, out _)
            && (test.TimeLimitMinutes <= 0 || startedMs - priorStart < test.TimeLimitMinutes * 60_000L))
            startedMs = priorStart;
        // Server-side record of when this attempt started and which questions it contains.
        HttpContext.Session.SetString($"test:{test.Id}", $"{startedMs}|{string.Join(',', questions.Select(q => q.Id))}");

        return View(new AssessmentTakeViewModel
        {
            Id = test.Id,
            Title = test.Title,
            AssessmentKind = "Test",
            TimeLimitMinutes = test.TimeLimitMinutes,
            PassingScore = test.PassingScore,
            MaxAttempts = int.MaxValue,
            ShowInstantResults = true,
            StartedAtUnixMs = startedMs,
            QuestionIds = questions.Select(q => q.Id).ToList(),
            Questions = questions
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(AssessmentTakeViewModel m)
    {
        var s = await db.Students.FindAsync(StudentId);
        var test = await db.Tests.Include(t => t.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(t => t.Id == m.Id && t.IsPublished && (t.GroupName == s!.GroupName || t.GroupName == "All"));

        if (s == null || test == null) return NotFound();

        // Posted ids/start time are ignored: the attempt must have been started server-side.
        if (!StudentController.TryReadAttempt(HttpContext.Session.GetString($"test:{test.Id}"), out var startedAtMs, out var selectedIds))
            return RedirectToAction(nameof(Index));
        HttpContext.Session.Remove($"test:{test.Id}");
        var questions = test.Questions.Where(q => selectedIds.Contains(q.Id)).OrderBy(q => q.SortOrder).ToList();
        if (questions.Count == 0) return RedirectToAction(nameof(Index));

        var elapsed = (int)Math.Max(0, (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startedAtMs) / 1000);
        var lateByTimeLimit = test.TimeLimitMinutes > 0 && elapsed > test.TimeLimitMinutes * 60 + 30;
        if (test.TimeLimitMinutes > 0) elapsed = Math.Min(elapsed, test.TimeLimitMinutes * 60);

        var correct = 0;
        var skipped = 0;
        var answers = new List<AssessmentAnswer>();

        foreach (var q in questions)
        {
            m.Answers.TryGetValue(q.Id, out var answer);
            if (lateByTimeLimit) answer = null;
            var ok = EvaluateAnswer(q, answer);
            if (string.IsNullOrWhiteSpace(answer)) skipped++;
            else if (ok) correct++;

            answers.Add(new AssessmentAnswer
            {
                QuestionId = q.Id,
                IsCorrect = ok,
                GivenAnswer = answer ?? "",
                Topic = q.Topic,
                PointsEarned = ok ? q.Points : 0
            });
        }

        var total = questions.Count;
        var wrong = Math.Max(0, total - correct - skipped);
        var score = total == 0 ? 0 : (int)Math.Round(correct * 100d / total);
        var passed = score >= test.PassingScore;
        // Anti-farming: XP once for the first attempt, and a pass bonus the first time the student passes.
        var priorResults = await db.TestResults.Where(x => x.StudentId == s.Id && x.TestId == test.Id).Select(x => x.Passed).ToListAsync();
        var xp = 0;
        if (priorResults.Count == 0) xp = passed ? 100 : 25;
        else if (passed && !priorResults.Any(p => p)) xp = 75;

        var result = new TestResult
        {
            StudentId = s.Id, TestId = test.Id, Score = score,
            Correct = correct, Wrong = wrong, Skipped = skipped,
            XpEarned = xp, TimeTakenSeconds = elapsed, Passed = passed
        };
        db.TestResults.Add(result);
        await db.SaveChangesAsync();

        foreach (var a in answers) a.TestResultId = result.Id;
        db.AssessmentAnswers.AddRange(answers);

        s.TotalXp += xp;
        s.XpBalance += xp;
        db.XpTransactions.Add(new XpTransaction
        {
            StudentId = s.Id, Points = xp, Reason = $"Test: {test.Title}"
        });

        if (score == 100)
        {
            var badge = await db.Badges.FirstOrDefaultAsync(b => b.Name == "Perfect Score");
            if (badge != null && !await db.StudentBadges.AnyAsync(x => x.StudentId == s.Id && x.BadgeId == badge.Id))
                db.StudentBadges.Add(new StudentBadge { StudentId = s.Id, BadgeId = badge.Id });
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Result), new { id = result.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Result(int id)
    {
        var r = await db.TestResults
            .Include(x => x.Test)
            .FirstOrDefaultAsync(x => x.Id == id && x.StudentId == StudentId);

        if (r == null) return NotFound();

        var answers = await db.AssessmentAnswers.Where(x => x.TestResultId == r.Id).ToListAsync();
        var topics = answers.GroupBy(x => string.IsNullOrWhiteSpace(x.Topic) ? "General" : x.Topic)
            .ToDictionary(g => g.Key, g => (int)Math.Round(g.Count(x => x.IsCorrect) * 100d / (double)g.Count()));

        return View(new AssessmentResultViewModel
        {
            Title = r.Test.Title,
            AssessmentKind = $"Test · {r.Test.TestType}",
            Score = r.Score,
            Correct = r.Correct,
            Wrong = r.Wrong,
            Skipped = r.Skipped,
            TimeTakenSeconds = r.TimeTakenSeconds,
            Passed = r.Passed,
            XpEarned = r.XpEarned,
            PassingScore = r.Test.PassingScore,
            TopicPerformance = topics,
            StrongAreas = topics.Where(x => x.Value >= 80).OrderByDescending(x => x.Value).Select(x => (x.Key, x.Value)).ToList(),
            NeedsRevision = topics.Where(x => x.Value < 70).OrderBy(x => x.Value).Select(x => (x.Key, x.Value)).ToList()
        });
    }

    private static bool EvaluateAnswer(Question q, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        raw = raw.Trim();

        if (q.QuestionType is "MCQ" or "TrueFalse" or "Image")
            return q.Options.Any(o => o.IsCorrect && string.Equals(o.Text.Trim(), raw, StringComparison.OrdinalIgnoreCase));

        if (q.QuestionType is "ShortAnswer" or "Scenario")
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
}
