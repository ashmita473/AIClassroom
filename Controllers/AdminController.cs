using AIClassroom.Data;
using AIClassroom.Models;
using AIClassroom.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIClassroom.Controllers;
[Authorize(Roles="SuperAdmin,Teacher")]
public class AdminController(AppDbContext db, PasswordService passwords) : Controller
{
    [HttpGet] public async Task<IActionResult> Index(){return View(new AdminDashboardViewModel{Students=await db.Students.CountAsync(),Teachers=await db.Teachers.CountAsync(),PublishedLessons=await db.CurriculumDays.CountAsync(x=>x.IsPublished),Games=await db.Games.CountAsync(),Quizzes=await db.Quizzes.CountAsync(),Tests=await db.Tests.CountAsync(),Submissions=await db.Submissions.CountAsync(),RecentStudents=await db.Students.OrderByDescending(x=>x.CreatedAtUtc).Take(8).ToListAsync(),Announcements=await db.Announcements.OrderByDescending(x=>x.CreatedAtUtc).Take(5).ToListAsync()});}
    [HttpGet] public async Task<IActionResult> Students(string? group=null,int? classNumber=null,string? q=null){var query=db.Students.AsQueryable();if(!string.IsNullOrWhiteSpace(group))query=query.Where(x=>x.GroupName==group);if(classNumber.HasValue)query=query.Where(x=>x.ClassNumber==classNumber);if(!string.IsNullOrWhiteSpace(q))query=query.Where(x=>x.Name.Contains(q)||x.RollNumber.Contains(q));ViewBag.Group=group;ViewBag.ClassNumber=classNumber;ViewBag.Q=q;return View(await query.OrderBy(x=>x.GroupName).ThenBy(x=>x.ClassNumber).ThenBy(x=>x.RollNumber).ToListAsync());}
    [HttpGet] public IActionResult AddStudent()=>View(new Student());
    [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> AddStudent(Student input,string? Pin)
    {
        ModelState.Clear(); // validate explicitly below (implicit non-nullable checks would flag server-set fields like PinHash)
        // Copy only the fields a teacher may set (blocks over-posting of TotalXp, XpBalance, IsActive, Id, etc.).
        var name=(input.Name??"").Trim(); var roll=(input.RollNumber??"").Trim();
        var m=new Student{Name=name,RollNumber=roll,GroupName=input.GroupName,ClassNumber=input.ClassNumber,Avatar=string.IsNullOrWhiteSpace(input.Avatar)?"🤖":input.Avatar.Trim()};
        if(name.Length==0||name.Length>120||roll.Length==0||roll.Length>30)ModelState.AddModelError("","Name and roll number are required.");
        if(m.GroupName is not("A" or "B")||m.ClassNumber<3||m.ClassNumber>8)ModelState.AddModelError("","Choose a valid group (A/B) and class (3-8).");
        if(m.Avatar.Length>100)m.Avatar=m.Avatar[..100];
        var pin=(Pin??"").Trim();
        if(pin.Length<4||pin.Length>8||!pin.All(char.IsAsciiDigit)||pin.Distinct().Count()==1||"0123456789".Contains(pin)||"9876543210".Contains(pin))
            ModelState.AddModelError("","PIN must be 4-8 digits and not an obvious sequence (like 1234 or 0000).");
        if(await db.Students.AnyAsync(x=>x.Name==name&&x.RollNumber==roll&&x.ClassNumber==m.ClassNumber))ModelState.AddModelError("","A student with this name, roll number and class already exists.");
        if(!ModelState.IsValid)return View(m);
        m.PinHash=passwords.Hash(pin);db.Students.Add(m);await db.SaveChangesAsync();return RedirectToAction(nameof(Students));
    }
    [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> ToggleStudent(int id){var s=await db.Students.FindAsync(id);if(s!=null){s.IsActive=!s.IsActive;await db.SaveChangesAsync();}return RedirectToAction(nameof(Students));}
    [HttpGet] public async Task<IActionResult> Curriculum(string? group=null){var q=db.CurriculumModules.Include(x=>x.Days).AsQueryable();if(group!=null)q=q.Where(x=>x.GroupName==group);ViewBag.Group=group;return View(await q.OrderBy(x=>x.GroupName).ThenBy(x=>x.SortOrder).ToListAsync());}
    [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> ToggleDay(int id){var d=await db.CurriculumDays.FindAsync(id);if(d!=null){d.IsLocked=!d.IsLocked;d.IsPublished=!d.IsLocked;await db.SaveChangesAsync();}return RedirectToAction(nameof(Curriculum),new{group=Request.Query["group"].ToString()});}
    [HttpGet] public async Task<IActionResult> Games(){return View(await db.Games.OrderBy(x=>x.GroupName).ThenBy(x=>x.Name).ToListAsync());}
    [HttpGet] public IActionResult AddGame()=>View(new Game());
    [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> AddGame(Game m){if(!ModelState.IsValid)return View(m);db.Games.Add(m);await db.SaveChangesAsync();return RedirectToAction(nameof(Games));}
    [HttpGet]
    public async Task<IActionResult> Quizzes()
    {
        return View(await db.Quizzes.Include(x => x.Questions).OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpGet]
    public IActionResult AddQuiz() => View(new Quiz());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuiz(Quiz m)
    {
        if (!ModelState.IsValid) return View(m);
        db.Quizzes.Add(m);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(QuizQuestions), new { quizId = m.Id });
    }

    [HttpGet]
    public async Task<IActionResult> QuizQuestions(int quizId)
    {
        var quiz = await db.Quizzes.Include(x => x.Questions).ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == quizId);
        return quiz == null ? NotFound() : View(quiz);
    }

    [HttpGet]
    public async Task<IActionResult> Tests()
    {
        return View(await db.Tests.Include(x => x.Questions).OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpGet]
    public IActionResult AddTest() => View(new Test());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTest(Test m)
    {
        if (!ModelState.IsValid) return View(m);
        db.Tests.Add(m);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(TestQuestions), new { testId = m.Id });
    }

    [HttpGet]
    public async Task<IActionResult> TestQuestions(int testId)
    {
        var test = await db.Tests.Include(x => x.Questions).ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == testId);
        return test == null ? NotFound() : View(test);
    }

    [HttpGet]
    public async Task<IActionResult> QuestionBank(string? type = null, string? topic = null)
    {
        var q = db.Questions.Include(x => x.Options).Where(x => x.IsBanked).AsQueryable();
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(x => x.QuestionType == type);
        if (!string.IsNullOrWhiteSpace(topic)) q = q.Where(x => x.Topic.Contains(topic));
        ViewBag.Type = type;
        ViewBag.Topic = topic;
        ViewBag.Quizzes = await db.Quizzes.OrderBy(x => x.Title).ToListAsync();
        ViewBag.Tests = await db.Tests.OrderBy(x => x.Title).ToListAsync();
        return View(await q.OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpGet]
    public IActionResult AddQuestion(string parentType, int parentId)
    {
        if (parentType is not ("Quiz" or "Test")) return BadRequest();
        return View(new QuestionEditorViewModel { ParentType = parentType, ParentId = parentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(QuestionEditorViewModel m)
    {
        if (m.ParentType is not ("Quiz" or "Test")) return BadRequest();

        var validParent = m.ParentType == "Quiz"
            ? await db.Quizzes.AnyAsync(x => x.Id == m.ParentId)
            : await db.Tests.AnyAsync(x => x.Id == m.ParentId);

        if (!validParent) return NotFound();

        if (string.IsNullOrWhiteSpace(m.Prompt))
        {
            ModelState.AddModelError(nameof(m.Prompt), "Question text is required.");
            return View(m);
        }

        string? uploadedImagePath = m.ImagePath;
        if (m.ImageFile is not null && m.ImageFile.Length > 0)
        {
            var safeExt = await GetSafeImageExtensionAsync(m.ImageFile);
            if (safeExt == null)
            {
                ModelState.AddModelError("", "Upload a PNG, JPG, GIF or WEBP image up to 2 MB.");
                return View(m);
            }
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "questions");
            Directory.CreateDirectory(folder);
            var extension = safeExt;
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, fileName);
            await using var stream = System.IO.File.Create(fullPath);
            await m.ImageFile.CopyToAsync(stream);
            uploadedImagePath = $"/uploads/questions/{fileName}";
        }

        var q = new Question
        {
            QuizId = m.ParentType == "Quiz" ? m.ParentId : null,
            TestId = m.ParentType == "Test" ? m.ParentId : null,
            QuestionType = m.QuestionType,
            Topic = string.IsNullOrWhiteSpace(m.Topic) ? "General" : m.Topic.Trim(),
            Prompt = m.Prompt.Trim(),
            ImagePath = string.IsNullOrWhiteSpace(uploadedImagePath) ? null : uploadedImagePath.Trim(),
            Answer = m.Answer,
            Explanation = m.Explanation,
            Points = Math.Max(1, m.Points),
            SortOrder = m.SortOrder,
            IsBanked = m.IsBanked,
            Difficulty = m.Difficulty
        };

        var lines = (m.OptionsText ?? "")
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (m.QuestionType is "MCQ" or "TrueFalse" or "Image")
        {
            foreach (var line in lines)
            {
                var parts = line.Split('|', 2, StringSplitOptions.TrimEntries);
                q.Options.Add(new QuestionOption
                {
                    Text = parts[0],
                    IsCorrect = parts.Length == 2 && parts[1].Equals("correct", StringComparison.OrdinalIgnoreCase)
                });
            }
            if (m.QuestionType == "TrueFalse" && q.Options.Count == 0)
            {
                q.Options.Add(new QuestionOption { Text = "True", IsCorrect = string.Equals(m.Answer, "True", StringComparison.OrdinalIgnoreCase) });
                q.Options.Add(new QuestionOption { Text = "False", IsCorrect = string.Equals(m.Answer, "False", StringComparison.OrdinalIgnoreCase) });
            }
        }
        else if (m.QuestionType == "Match")
        {
            foreach (var line in lines)
            {
                var parts = line.Split("=>", 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                    q.Options.Add(new QuestionOption { Text = parts[0], MatchKey = parts[1] });
            }
        }
        else if (m.QuestionType == "Ordering")
        {
            var i = 0;
            foreach (var line in lines)
                q.Options.Add(new QuestionOption { Text = line, OrderIndex = i++ });
        }

        db.Questions.Add(q);
        await db.SaveChangesAsync();

        if (m.ParentType == "Quiz") return RedirectToAction(nameof(QuizQuestions), new { quizId = m.ParentId });
        return RedirectToAction(nameof(TestQuestions), new { testId = m.ParentId });
    }

    // Allowlist by real file content (magic bytes), never by the client-supplied name or content type.
    private static async Task<string?> GetSafeImageExtensionAsync(IFormFile file)
    {
        if (file.Length is <= 0 or > 2 * 1024 * 1024) return null;
        var head = new byte[12];
        await using var s = file.OpenReadStream();
        var read = await s.ReadAsync(head.AsMemory(0, 12));
        if (read < 12) return null;
        if (head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47) return ".png";
        if (head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF) return ".jpg";
        if (head[0] == 'G' && head[1] == 'I' && head[2] == 'F' && head[3] == '8') return ".gif";
        if (head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F' && head[8] == 'W' && head[9] == 'E' && head[10] == 'B' && head[11] == 'P') return ".webp";
        return null;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UseQuestion(int id, string parentType, int parentId)
    {
        var source = await db.Questions.Include(x => x.Options).FirstOrDefaultAsync(x => x.Id == id);
        if (source == null || parentType is not ("Quiz" or "Test")) return NotFound();
        var valid = parentType == "Quiz" ? await db.Quizzes.AnyAsync(x => x.Id == parentId) : await db.Tests.AnyAsync(x => x.Id == parentId);
        if (!valid) return NotFound();
        var copy = new Question
        {
            QuizId = parentType == "Quiz" ? parentId : null,
            TestId = parentType == "Test" ? parentId : null,
            QuestionType = source.QuestionType, Topic = source.Topic, Prompt = source.Prompt,
            ImagePath = source.ImagePath, Answer = source.Answer, Explanation = source.Explanation,
            Points = source.Points, SortOrder = source.SortOrder, IsBanked = source.IsBanked, Difficulty = source.Difficulty
        };
        foreach (var o in source.Options) copy.Options.Add(new QuestionOption { Text = o.Text, IsCorrect = o.IsCorrect, MatchKey = o.MatchKey, OrderIndex = o.OrderIndex });
        db.Questions.Add(copy);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(QuestionBank));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var q = await db.Questions.FindAsync(id);
        if (q == null) return NotFound();

        var quizId = q.QuizId;
        var testId = q.TestId;
        db.Questions.Remove(q);
        await db.SaveChangesAsync();

        if (quizId.HasValue) return RedirectToAction(nameof(QuizQuestions), new { quizId });
        if (testId.HasValue) return RedirectToAction(nameof(TestQuestions), new { testId });
        return RedirectToAction(nameof(QuestionBank));
    }

    [HttpGet] public async Task<IActionResult> Assignments(){return View(await db.Assignments.OrderByDescending(x=>x.DueDateUtc).ToListAsync());}
    [HttpGet] public IActionResult AddAssignment()=>View(new Assignment());
    [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> AddAssignment(Assignment m){db.Assignments.Add(m);await db.SaveChangesAsync();return RedirectToAction(nameof(Assignments));}
    [HttpGet] public async Task<IActionResult> Notes(){return View(await db.TeacherNotes.OrderBy(x=>x.Id).ToListAsync());}
    [HttpGet] public IActionResult AddNote()=>View(new TeacherNote());
    [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> AddNote(TeacherNote m){db.TeacherNotes.Add(m);await db.SaveChangesAsync();return RedirectToAction(nameof(Notes));}
    [HttpGet] public async Task<IActionResult> Announcements(){return View(await db.Announcements.OrderByDescending(x=>x.CreatedAtUtc).ToListAsync());}
    [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> Announcements(string title,string message,string? groupName){if(string.IsNullOrWhiteSpace(title)||string.IsNullOrWhiteSpace(message)||title.Length>200||message.Length>2000||(!string.IsNullOrWhiteSpace(groupName)&&groupName is not("A" or "B")))return BadRequest();db.Announcements.Add(new Announcement{Title=title,Message=message,GroupName=string.IsNullOrWhiteSpace(groupName)?null:groupName});await db.SaveChangesAsync();return RedirectToAction(nameof(Announcements));}
    [HttpGet] public async Task<IActionResult> Analytics(){var students=await db.Students.ToListAsync();var results=await db.QuizResults.ToListAsync();ViewBag.AvgXp=students.Count==0?0:Math.Round(students.Average(x=>(double)x.TotalXp));ViewBag.AvgQuiz=results.Count==0?0:Math.Round(results.Average(x=>(double)x.Score));ViewBag.Completed=await db.StudentProgress.CountAsync(x=>x.Completed);ViewBag.GamePlays=await db.GameResults.CountAsync();return View(students);}
    [HttpGet] public async Task<IActionResult> Reports(){ViewBag.Students=await db.Students.CountAsync();ViewBag.QuizResults=await db.QuizResults.CountAsync();ViewBag.TestResults=await db.TestResults.CountAsync();ViewBag.Submissions=await db.Submissions.CountAsync();return View();}
    [HttpGet] public async Task<IActionResult> QuizResults(){return View(await db.QuizResults.Include(x=>x.Student).Include(x=>x.Quiz).OrderByDescending(x=>x.CompletedAtUtc).ToListAsync());}
    [HttpGet] public async Task<IActionResult> TestResults(){return View(await db.TestResults.Include(x=>x.Student).Include(x=>x.Test).OrderByDescending(x=>x.CompletedAtUtc).ToListAsync());}
    [HttpGet] public async Task<IActionResult> Live(){var days=await db.CurriculumDays.Where(x=>x.IsPublished).OrderBy(x=>x.DayNumber).ToListAsync();return View(days);}
}
