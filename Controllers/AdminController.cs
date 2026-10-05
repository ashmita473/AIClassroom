using AIClassroom.Data;
using AIClassroom.Models;
using AIClassroom.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIClassroom.Controllers;
[Authorize(Roles="SuperAdmin,Teacher")]
public class AdminController(AppDbContext db, PasswordService passwords, IWebHostEnvironment env) : Controller
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

    [HttpGet]
    public async Task<IActionResult> ChangeStudentPin(int id)
    {
        var student = await db.Students.FindAsync(id);
        if (student == null) return NotFound();
        return View(new ChangeStudentPinViewModel
        {
            StudentId = student.Id, StudentName = student.Name,
            RollNumber = student.RollNumber, ClassNumber = student.ClassNumber
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStudentPin(ChangeStudentPinViewModel model)
    {
        var student = await db.Students.FindAsync(model.StudentId);
        if (student == null) return NotFound();
        model.StudentName = student.Name; model.RollNumber = student.RollNumber; model.ClassNumber = student.ClassNumber;
        var pin = (model.NewPin ?? "").Trim();
        if (pin.Length < 4 || pin.Length > 8 || !pin.All(char.IsAsciiDigit) ||
            pin.Distinct().Count() == 1 || "0123456789".Contains(pin) || "9876543210".Contains(pin))
            ModelState.AddModelError(nameof(model.NewPin), "PIN must be 4-8 digits and not an obvious sequence.");
        if (pin != model.ConfirmPin) ModelState.AddModelError(nameof(model.ConfirmPin), "PINs do not match.");
        if (!ModelState.IsValid) return View(model);
        student.PinHash = passwords.Hash(pin);
        await db.SaveChangesAsync();
        TempData["SecurityMessage"] = $"PIN changed for {student.Name}.";
        return RedirectToAction(nameof(Students));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var teacherId = GetTeacherId();
        var teacher = await db.Teachers.FindAsync(teacherId);
        if (teacher == null) return NotFound();
        return View(new TeacherProfileViewModel
        {
            Name = teacher.Name,
            Username = teacher.Username,
            Role = teacher.Role,
            Department = teacher.Department,
            Bio = teacher.Bio,
            ProfileImagePath = teacher.ProfileImagePath
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(TeacherProfileViewModel model)
    {
        var teacherId = GetTeacherId();
        var teacher = await db.Teachers.FindAsync(teacherId);
        if (teacher == null) return NotFound();

        model.Username = teacher.Username;
        if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length > 100)
            ModelState.AddModelError(nameof(model.Name), "Enter a name up to 100 characters.");
        if ((model.Department ?? "").Length > 100)
            ModelState.AddModelError(nameof(model.Department), "Department is too long.");
        if ((model.Bio ?? "").Length > 500)
            ModelState.AddModelError(nameof(model.Bio), "Bio is too long.");

        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var selectedRole = isSuperAdmin && (model.Role is "SuperAdmin" or "Teacher") ? model.Role : teacher.Role;
        // Never let the last active SuperAdmin demote themselves (would lock everyone out of Teachers/PIN/password management).
        if (teacher.Role == "SuperAdmin" && selectedRole != "SuperAdmin" &&
            !await db.Teachers.AnyAsync(x => x.Id != teacher.Id && x.IsActive && x.Role == "SuperAdmin"))
        {
            ModelState.AddModelError(nameof(model.Role), "You are the only SuperAdmin. Promote another teacher first.");
            selectedRole = teacher.Role;
        }

        if (model.ProfileImage != null && model.ProfileImage.Length > 0)
        {
            if (model.ProfileImage.Length > 2 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.ProfileImage), "Profile picture must be 2 MB or smaller.");
            if (await GetSafeImageExtensionAsync(model.ProfileImage) == null)
                ModelState.AddModelError(nameof(model.ProfileImage), "Use a real PNG, JPG, GIF or WebP image up to 2 MB.");
        }

        if (!ModelState.IsValid)
        {
            model.ProfileImagePath = teacher.ProfileImagePath;
            model.Role = teacher.Role;
            return View(model);
        }

        teacher.Name = model.Name.Trim();
        teacher.Department = (model.Department ?? "").Trim();
        teacher.Bio = (model.Bio ?? "").Trim();
        teacher.Role = selectedRole;

        if (model.ProfileImage != null && model.ProfileImage.Length > 0)
        {
            var folder = Path.Combine(env.WebRootPath, "uploads", "teachers");
            Directory.CreateDirectory(folder);
            var ext = await GetSafeImageExtensionAsync(model.ProfileImage) ?? ".png";
            var fileName = $"teacher-{teacher.Id}-{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(folder, fileName);
            await using (var stream = System.IO.File.Create(filePath))
                await model.ProfileImage.CopyToAsync(stream);

            DeleteTeacherImage(teacher.ProfileImagePath);
            teacher.ProfileImagePath = $"/uploads/teachers/{fileName}";
        }

        await db.SaveChangesAsync();
        await RefreshTeacherCookie(teacher);
        TempData["ProfileMessage"] = "Your teacher profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    private int GetTeacherId() => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    private void DeleteTeacherImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("/uploads/teachers/", StringComparison.OrdinalIgnoreCase)) return;
        var relative = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(env.WebRootPath, relative);
        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
    }

    private async Task RefreshTeacherCookie(Teacher teacher)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, teacher.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, teacher.Name),
            new(System.Security.Claims.ClaimTypes.Role, teacher.Role),
            new("Group", ""),
            new("ClassNumber", ""),
            new("ProfileImage", teacher.ProfileImagePath ?? "")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new System.Security.Claims.ClaimsPrincipal(identity));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var teacherId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var teacher = await db.Teachers.FindAsync(teacherId);
        if (teacher == null) return NotFound();
        if (!passwords.Verify(model.CurrentPassword ?? "", teacher.PasswordHash))
            ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
        if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 10)
            ModelState.AddModelError(nameof(model.NewPassword), "Password must be at least 10 characters.");
        if (model.NewPassword != model.ConfirmPassword)
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Passwords do not match.");
        if (!ModelState.IsValid) return View(model);
        teacher.PasswordHash = passwords.Hash(model.NewPassword);
        await db.SaveChangesAsync();
        TempData["SecurityMessage"] = "Your password has been changed.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles="SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> Teachers() => View(await db.Teachers.OrderBy(x => x.Name).ToListAsync());

    [Authorize(Roles="SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> ResetTeacherPassword(int id)
    {
        var teacher = await db.Teachers.FindAsync(id);
        if (teacher == null) return NotFound();
        return View(new ResetTeacherPasswordViewModel { TeacherId = teacher.Id, TeacherName = teacher.Name, Username = teacher.Username });
    }

    [Authorize(Roles="SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetTeacherPassword(ResetTeacherPasswordViewModel model)
    {
        var teacher = await db.Teachers.FindAsync(model.TeacherId);
        if (teacher == null) return NotFound();
        model.TeacherName = teacher.Name; model.Username = teacher.Username;
        if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 10)
            ModelState.AddModelError(nameof(model.NewPassword), "Password must be at least 10 characters.");
        if (model.NewPassword != model.ConfirmPassword)
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Passwords do not match.");
        if (!ModelState.IsValid) return View(model);
        teacher.PasswordHash = passwords.Hash(model.NewPassword);
        await db.SaveChangesAsync();
        TempData["SecurityMessage"] = $"Password reset for {teacher.Username}.";
        return RedirectToAction(nameof(Teachers));
    }
    [HttpGet] public async Task<IActionResult> Curriculum(string? group=null){var q=db.CurriculumModules.Include(x=>x.Days).ThenInclude(x=>x.Lesson).ThenInclude(x=>x!.Activities).AsQueryable();if(group!=null)q=q.Where(x=>x.GroupName==group);ViewBag.Group=group;return View(await q.OrderBy(x=>x.GroupName).ThenBy(x=>x.SortOrder).ToListAsync());}
    [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> ToggleDay(int id){var d=await db.CurriculumDays.FindAsync(id);if(d!=null){d.IsLocked=!d.IsLocked;d.IsPublished=!d.IsLocked;await db.SaveChangesAsync();}return RedirectToAction(nameof(Curriculum),new{group=Request.Query["group"].ToString()});}
    [HttpGet]
    public async Task<IActionResult> AddModule() => View(new CurriculumModuleEditViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddModule(CurriculumModuleEditViewModel m)
    {
        ValidateModule(m);
        if (!ModelState.IsValid) return View(m);
        db.CurriculumModules.Add(new CurriculumModule { GroupName=m.GroupName, ClassMin=m.ClassMin, ClassMax=m.ClassMax, Code=m.Code.Trim(), Title=m.Title.Trim(), SortOrder=m.SortOrder });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Curriculum), new { group=m.GroupName });
    }

    [HttpGet]
    public async Task<IActionResult> EditModule(int id)
    {
        var m=await db.CurriculumModules.FindAsync(id); if(m==null) return NotFound();
        return View(new CurriculumModuleEditViewModel { Id=m.Id, GroupName=m.GroupName, ClassMin=m.ClassMin, ClassMax=m.ClassMax, Code=m.Code, Title=m.Title, SortOrder=m.SortOrder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditModule(CurriculumModuleEditViewModel m)
    {
        ValidateModule(m); if(!m.Id.HasValue) return NotFound(); if(!ModelState.IsValid) return View(m);
        var x=await db.CurriculumModules.FindAsync(m.Id.Value); if(x==null) return NotFound();
        x.GroupName=m.GroupName; x.ClassMin=m.ClassMin; x.ClassMax=m.ClassMax; x.Code=m.Code.Trim(); x.Title=m.Title.Trim(); x.SortOrder=m.SortOrder;
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Curriculum), new { group=m.GroupName });
    }

    [HttpGet]
    public async Task<IActionResult> AddDay(int moduleId)
    {
        var module=await db.CurriculumModules.FindAsync(moduleId); if(module==null) return NotFound();
        var next=(await db.CurriculumDays.Where(x=>x.CurriculumModuleId==moduleId).Select(x=>(int?)x.DayNumber).MaxAsync() ?? 0)+1;
        return View(new CurriculumDayEditViewModel { CurriculumModuleId=moduleId, DayNumber=next, XpReward=50, IsPublished=true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDay(CurriculumDayEditViewModel m)
    {
        ValidateDay(m); var module=await db.CurriculumModules.FindAsync(m.CurriculumModuleId); if(module==null) return NotFound();
        if(await db.CurriculumDays.AnyAsync(x=>x.CurriculumModuleId==m.CurriculumModuleId && x.DayNumber==m.DayNumber)) ModelState.AddModelError("DayNumber","That lesson/day number already exists.");
        if(!ModelState.IsValid) return View(m);
        db.CurriculumDays.Add(new CurriculumDay { CurriculumModuleId=m.CurriculumModuleId, DayNumber=m.DayNumber, Title=m.Title.Trim(), ContentSummary=m.ContentSummary.Trim(), XpReward=Math.Max(0,m.XpReward), IsPublished=m.IsPublished, IsLocked=m.IsLocked });
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Curriculum), new { group=module.GroupName });
    }

    [HttpGet]
    public async Task<IActionResult> EditDay(int id)
    {
        var d=await db.CurriculumDays.Include(x=>x.Module).FirstOrDefaultAsync(x=>x.Id==id); if(d==null) return NotFound();
        return View(new CurriculumDayEditViewModel { Id=d.Id, CurriculumModuleId=d.CurriculumModuleId, DayNumber=d.DayNumber, Title=d.Title, ContentSummary=d.ContentSummary, XpReward=d.XpReward, IsPublished=d.IsPublished, IsLocked=d.IsLocked });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDay(CurriculumDayEditViewModel m)
    {
        ValidateDay(m); if(!m.Id.HasValue) return NotFound();
        var d=await db.CurriculumDays.Include(x=>x.Module).FirstOrDefaultAsync(x=>x.Id==m.Id.Value); if(d==null) return NotFound();
        if(await db.CurriculumDays.AnyAsync(x=>x.Id!=d.Id && x.CurriculumModuleId==m.CurriculumModuleId && x.DayNumber==m.DayNumber)) ModelState.AddModelError("DayNumber","That lesson/day number already exists.");
        if(!ModelState.IsValid) return View(m);
        d.CurriculumModuleId=m.CurriculumModuleId; d.DayNumber=m.DayNumber; d.Title=m.Title.Trim(); d.ContentSummary=m.ContentSummary.Trim(); d.XpReward=Math.Max(0,m.XpReward); d.IsPublished=m.IsPublished; d.IsLocked=m.IsLocked;
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Curriculum), new { group=d.Module?.GroupName });
    }

    [HttpGet]
    public async Task<IActionResult> EditLesson(int dayId)
    {
        var day=await db.CurriculumDays.Include(x=>x.Module).Include(x=>x.Lesson!).ThenInclude(x=>x.Activities).Include(x=>x.Lesson!).ThenInclude(x=>x.Videos).FirstOrDefaultAsync(x=>x.Id==dayId); if(day==null) return NotFound();
        var quizzes=await db.Quizzes.Include(x=>x.Questions).Where(x=>x.GroupName==day.Module!.GroupName || x.CurriculumDayId==day.Id).OrderBy(x=>x.Title).ToListAsync();
        var vm=BuildLessonEditor(day,quizzes); return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLesson(LessonEditorViewModel m)
    {
        var day=await db.CurriculumDays.Include(x=>x.Module).Include(x=>x.Lesson!).ThenInclude(x=>x.Activities).Include(x=>x.Lesson!).ThenInclude(x=>x.Videos).FirstOrDefaultAsync(x=>x.Id==m.CurriculumDayId); if(day==null) return NotFound();
        if(string.IsNullOrWhiteSpace(m.Introduction)) ModelState.AddModelError(nameof(m.Introduction),"Introduction is required.");
        if(!ModelState.IsValid){ await PopulateLessonEditorChoices(m,day); return View(m); }
        var lesson=day.Lesson ?? new Lesson { CurriculumDayId=day.Id };
        lesson.Introduction=SanitizeLessonHtml(m.Introduction); lesson.Explanation=SanitizeLessonHtml(m.Explanation); lesson.Examples=SanitizeLessonHtml(m.Examples); lesson.TeacherNotes=SanitizeLessonHtml(m.TeacherNotes); lesson.StudentNotesTemplate=SanitizeLessonHtml(m.StudentNotesTemplate); lesson.IsPublished=m.IsPublished;
        if(lesson.Id==0) db.Lessons.Add(lesson);
        var existing=lesson.Activities.ToDictionary(x=>x.Id);
        foreach(var a in m.Activities)
        {
            if(a.Id.HasValue && existing.TryGetValue(a.Id.Value,out var row))
            {
                if(a.Delete) db.Activities.Remove(row); else { row.ActivityType=a.ActivityType.Trim(); row.Title=a.Title.Trim(); row.Content=SanitizeLessonHtml(a.Content); row.SortOrder=a.SortOrder; row.XpReward=Math.Max(0,a.XpReward); }
            }
            else if(!a.Delete && (!string.IsNullOrWhiteSpace(a.Title)||!string.IsNullOrWhiteSpace(a.Content)))
                lesson.Activities.Add(new Activity{ActivityType=a.ActivityType.Trim(),Title=a.Title.Trim(),Content=SanitizeLessonHtml(a.Content),SortOrder=a.SortOrder,XpReward=Math.Max(0,a.XpReward)});
        }
        var existingVideos=lesson.Videos.ToDictionary(x=>x.Id);
        foreach(var v in m.Videos ?? new List<LessonVideoEditViewModel>())
        {
            var videoId=ExtractYouTubeVideoId(v.YouTubeUrl);
            if(v.Id.HasValue && existingVideos.TryGetValue(v.Id.Value,out var row))
            {
                if(v.Delete) db.LessonVideos.Remove(row);
                else if(string.IsNullOrWhiteSpace(videoId)) ModelState.AddModelError("", $"A valid YouTube URL is required for video '{v.Title}'.");
                else { row.Title=v.Title.Trim(); row.Description=v.Description.Trim(); row.YouTubeVideoId=videoId; row.SortOrder=Math.Max(1,v.SortOrder); row.IsRequired=v.IsRequired; row.XpReward=Math.Max(0,v.XpReward); row.IsPublished=v.IsPublished; }
            }
            else if(!v.Delete && (!string.IsNullOrWhiteSpace(v.Title) || !string.IsNullOrWhiteSpace(v.YouTubeUrl)))
            {
                if(string.IsNullOrWhiteSpace(videoId)) ModelState.AddModelError("", $"A valid YouTube URL is required for video '{v.Title}'.");
                else lesson.Videos.Add(new LessonVideo{Title=v.Title.Trim(),Description=v.Description.Trim(),YouTubeVideoId=videoId,SortOrder=Math.Max(1,v.SortOrder),IsRequired=v.IsRequired,XpReward=Math.Max(0,v.XpReward),IsPublished=v.IsPublished});
            }
        }
        if(!ModelState.IsValid){ await PopulateLessonEditorChoices(m,day); return View(m); }

        var allDayQuizzes=await db.Quizzes.Where(x=>x.CurriculumDayId==day.Id).ToListAsync();
        foreach(var q in allDayQuizzes) q.CurriculumDayId=null;
        if(m.SelectedQuizIds!=null && m.SelectedQuizIds.Count>0)
        {
            var selected=await db.Quizzes.Where(x=>m.SelectedQuizIds.Contains(x.Id)).ToListAsync(); foreach(var q in selected) q.CurriculumDayId=day.Id;
        }
        await db.SaveChangesAsync();
        TempData["CurriculumMessage"]=$"Lesson saved: {day.Title}";
        return RedirectToAction(nameof(Curriculum),new {group=day.Module?.GroupName});
    }

    private LessonEditorViewModel BuildLessonEditor(CurriculumDay day, List<Quiz> quizzes)
    {
        var l=day.Lesson;
        return new LessonEditorViewModel { CurriculumDayId=day.Id, DayTitle=day.Title, XpReward=day.XpReward, Introduction=l?.Introduction??"", Explanation=l?.Explanation??"", Examples=l?.Examples??"", TeacherNotes=l?.TeacherNotes??"", StudentNotesTemplate=l?.StudentNotesTemplate??"", IsPublished=l?.IsPublished??day.IsPublished, Activities=l?.Activities.OrderBy(x=>x.SortOrder).Select(x=>new LessonActivityEditViewModel{Id=x.Id,ActivityType=x.ActivityType,Title=x.Title,Content=x.Content,SortOrder=x.SortOrder,XpReward=x.XpReward}).ToList()??new(), Videos=l?.Videos.OrderBy(x=>x.SortOrder).Select(x=>new LessonVideoEditViewModel{Id=x.Id,Title=x.Title,Description=x.Description,YouTubeUrl=$"https://www.youtube.com/watch?v={x.YouTubeVideoId}",VideoId=x.YouTubeVideoId,SortOrder=x.SortOrder,IsRequired=x.IsRequired,XpReward=x.XpReward,IsPublished=x.IsPublished}).ToList()??new(), AvailableQuizzes=quizzes.Select(q=>new LinkedQuizViewModel{Id=q.Id,Title=q.Title,IsPublished=q.IsPublished,QuestionCount=q.Questions.Count}).ToList(), SelectedQuizIds=quizzes.Where(q=>q.CurriculumDayId==day.Id).Select(q=>q.Id).ToList() };
    }
    private async Task PopulateLessonEditorChoices(LessonEditorViewModel m, CurriculumDay day){var quizzes=await db.Quizzes.Include(x=>x.Questions).Where(x=>x.GroupName==day.Module!.GroupName||x.CurriculumDayId==day.Id).OrderBy(x=>x.Title).ToListAsync();m.DayTitle=day.Title;m.AvailableQuizzes=quizzes.Select(q=>new LinkedQuizViewModel{Id=q.Id,Title=q.Title,IsPublished=q.IsPublished,QuestionCount=q.Questions.Count}).ToList();}
    private void ValidateModule(CurriculumModuleEditViewModel m){if(m.GroupName is not("A" or "B")) ModelState.AddModelError(nameof(m.GroupName),"Group must be A or B.");if(m.ClassMin<3||m.ClassMax>8||m.ClassMin>m.ClassMax)ModelState.AddModelError(nameof(m.ClassMin),"Choose a valid class range.");if(string.IsNullOrWhiteSpace(m.Code)||string.IsNullOrWhiteSpace(m.Title))ModelState.AddModelError("","Code and title are required.");}
    private void ValidateDay(CurriculumDayEditViewModel m){if(m.DayNumber<1)ModelState.AddModelError(nameof(m.DayNumber),"Lesson number must be 1 or higher.");if(string.IsNullOrWhiteSpace(m.Title))ModelState.AddModelError(nameof(m.Title),"Lesson title is required.");if(m.XpReward<0||m.XpReward>10000)ModelState.AddModelError(nameof(m.XpReward),"XP must be between 0 and 10000.");}
    private static string ExtractYouTubeVideoId(string? url)
    {
        if(string.IsNullOrWhiteSpace(url)) return "";
        if(Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            if(uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase)) return uri.AbsolutePath.Trim('/').Split('/')[0];
            if(uri.Host.EndsWith("youtube.com", StringComparison.OrdinalIgnoreCase))
            {
                var q=Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                if(q.TryGetValue("v",out var v)) return v.ToString().Trim();
                var parts=uri.AbsolutePath.Trim('/').Split('/');
                if(parts.Length>=2 && (parts[0].Equals("embed",StringComparison.OrdinalIgnoreCase)||parts[0].Equals("shorts",StringComparison.OrdinalIgnoreCase)||parts[0].Equals("live",StringComparison.OrdinalIgnoreCase))) return parts[1];
            }
        }
        return "";
    }

    // Allow-list sanitizer (Ganss.Xss). The old regex approach missed unquoted handlers, <svg onload>, entity-encoded schemes, etc.
    private static readonly Ganss.Xss.HtmlSanitizer LessonSanitizer = CreateLessonSanitizer();
    private static Ganss.Xss.HtmlSanitizer CreateLessonSanitizer()
    {
        var sanitizer = new Ganss.Xss.HtmlSanitizer();
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        return sanitizer;
    }

    private static string SanitizeLessonHtml(string? html)
        => string.IsNullOrWhiteSpace(html) ? "" : LessonSanitizer.Sanitize(html).Trim();

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

    [HttpGet]
    public async Task<IActionResult> Assignments()
    {
        var assignments = await db.Assignments.OrderByDescending(x => x.DueDateUtc).ThenByDescending(x => x.Id).ToListAsync();
        ViewBag.SubmissionCounts = await db.Submissions.GroupBy(x => x.AssignmentId).Select(g => new { g.Key, Count = g.Count(), Graded = g.Count(x => x.Marks.HasValue) }).ToDictionaryAsync(x => x.Key, x => (x.Count, x.Graded));
        return View(assignments);
    }

    [HttpGet] public IActionResult AddAssignment() => View(new AssignmentEditViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAssignment(AssignmentEditViewModel m)
    {
        ValidateAssignment(m);
        if (!ModelState.IsValid) return View(m);
        db.Assignments.Add(new Assignment { Title = m.Title.Trim(), Instructions = m.Instructions.Trim(), GroupName = m.GroupName, CurriculumDayId = m.CurriculumDayId, DueDateUtc = ToUtc(m.DueDateUtc), XpReward = m.XpReward, IsPublished = m.IsPublished });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Assignments));
    }

    [HttpGet]
    public async Task<IActionResult> EditAssignment(int id)
    {
        var a = await db.Assignments.FindAsync(id);
        if (a == null) return NotFound();
        return View(new AssignmentEditViewModel { Id=a.Id, Title=a.Title, Instructions=a.Instructions, GroupName=a.GroupName, CurriculumDayId=a.CurriculumDayId, DueDateUtc=a.DueDateUtc, XpReward=a.XpReward, IsPublished=a.IsPublished });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAssignment(AssignmentEditViewModel m)
    {
        ValidateAssignment(m);
        if (!m.Id.HasValue) return NotFound();
        if (!ModelState.IsValid) return View(m);
        var a = await db.Assignments.FindAsync(m.Id.Value);
        if (a == null) return NotFound();
        a.Title=m.Title.Trim(); a.Instructions=m.Instructions.Trim(); a.GroupName=m.GroupName; a.CurriculumDayId=m.CurriculumDayId; a.DueDateUtc=ToUtc(m.DueDateUtc); a.XpReward=m.XpReward; a.IsPublished=m.IsPublished;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Assignments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssignment(int id)
    {
        var a = await db.Assignments.FindAsync(id);
        if (a == null) return NotFound();
        db.Submissions.RemoveRange(db.Submissions.Where(x => x.AssignmentId == id));
        db.Assignments.Remove(a);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Assignments));
    }

    [HttpGet]
    public async Task<IActionResult> AssignmentSubmissions(int id)
    {
        var a = await db.Assignments.FindAsync(id);
        if (a == null) return NotFound();
        ViewBag.Assignment = a;
        var rows = await db.Submissions.Include(x => x.Student).Where(x => x.AssignmentId == id).OrderByDescending(x => x.SubmittedAtUtc).Select(x => new AssignmentGradeViewModel { SubmissionId=x.Id, StudentName=x.Student!.Name, AssignmentTitle=a.Title, Content=x.Content, SubmittedAtUtc=x.SubmittedAtUtc, Marks=x.Marks, Feedback=x.Feedback ?? "" }).ToListAsync();
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> GradeAssignment(int id)
    {
        var s = await db.Submissions.Include(x=>x.Student).Include(x=>x.Assignment).FirstOrDefaultAsync(x=>x.Id==id);
        if (s == null) return NotFound();
        ViewBag.AssignmentId = s.AssignmentId;
        return View(new AssignmentGradeViewModel { SubmissionId=s.Id, StudentName=s.Student?.Name ?? "Student", AssignmentTitle=s.Assignment?.Title ?? "Assignment", Content=s.Content, SubmittedAtUtc=s.SubmittedAtUtc, Marks=s.Marks, Feedback=s.Feedback ?? "" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GradeAssignment(AssignmentGradeViewModel m)
    {
        if (m.Marks.HasValue && (m.Marks.Value < 0 || m.Marks.Value > 100)) ModelState.AddModelError(nameof(m.Marks), "Marks must be between 0 and 100.");
        if ((m.Feedback ?? "").Length > 3000) ModelState.AddModelError(nameof(m.Feedback), "Feedback is too long.");
        var s = await db.Submissions.Include(x=>x.Assignment).FirstOrDefaultAsync(x=>x.Id==m.SubmissionId);
        if (s == null) return NotFound();
        ViewBag.AssignmentId = s.AssignmentId;
        if (!ModelState.IsValid) return View(m);
        s.Marks=m.Marks; s.Feedback=string.IsNullOrWhiteSpace(m.Feedback)?null:m.Feedback.Trim();
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(AssignmentSubmissions), new { id=s.AssignmentId });
    }

    private static DateTime? ToUtc(DateTime? value) => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified).ToUniversalTime() : null;

    private void ValidateAssignment(AssignmentEditViewModel m)
    {
        if (string.IsNullOrWhiteSpace(m.Title) || m.Title.Trim().Length > 200) ModelState.AddModelError(nameof(m.Title), "Enter an assignment title up to 200 characters.");
        if (string.IsNullOrWhiteSpace(m.Instructions) || m.Instructions.Trim().Length > 10000) ModelState.AddModelError(nameof(m.Instructions), "Instructions are required and must be under 10,000 characters.");
        if (m.GroupName is not ("A" or "B" or "All")) ModelState.AddModelError(nameof(m.GroupName), "Choose Group A, Group B or All.");
        if (m.XpReward < 0 || m.XpReward > 1000) ModelState.AddModelError(nameof(m.XpReward), "XP must be between 0 and 1000.");
    }
    [HttpGet] public async Task<IActionResult> Notes(){return View(await db.TeacherNotes.OrderByDescending(x=>x.Id).ToListAsync());}
    [HttpGet] public IActionResult AddNote()=>View(new TeacherNoteEditViewModel());
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(TeacherNoteEditViewModel m)
    {
        if (!ModelState.IsValid) return View(m);
        var category = m.Category is "Lesson Notes" or "Important Points" or "Examples" or "Revision Notes" ? m.Category : "Pre-written";
        db.TeacherNotes.Add(new TeacherNote { CurriculumDayId = m.CurriculumDayId, Title = $"[{category}] {m.Title.Trim()}", Content = m.Content.Trim() });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Notes));
    }
    [HttpGet] public async Task<IActionResult> Announcements(){return View(await db.Announcements.OrderByDescending(x=>x.CreatedAtUtc).ToListAsync());}
    [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> Announcements(string title,string message,string? groupName){if(string.IsNullOrWhiteSpace(title)||string.IsNullOrWhiteSpace(message)||title.Length>200||message.Length>2000||(!string.IsNullOrWhiteSpace(groupName)&&groupName is not("A" or "B")))return BadRequest();db.Announcements.Add(new Announcement{Title=title,Message=message,GroupName=string.IsNullOrWhiteSpace(groupName)?null:groupName});await db.SaveChangesAsync();return RedirectToAction(nameof(Announcements));}
    [HttpGet] public async Task<IActionResult> Analytics(){var students=await db.Students.ToListAsync();var results=await db.QuizResults.ToListAsync();ViewBag.AvgXp=students.Count==0?0:Math.Round(students.Average(x=>(double)x.TotalXp));ViewBag.AvgQuiz=results.Count==0?0:Math.Round(results.Average(x=>(double)x.Score));ViewBag.Completed=await db.StudentProgress.CountAsync(x=>x.Completed);ViewBag.GamePlays=await db.GameResults.CountAsync();return View(students);}
    [HttpGet] public async Task<IActionResult> Reports(){ViewBag.Students=await db.Students.CountAsync();ViewBag.QuizResults=await db.QuizResults.CountAsync();ViewBag.TestResults=await db.TestResults.CountAsync();ViewBag.Submissions=await db.Submissions.CountAsync();return View();}
    [HttpGet] public async Task<IActionResult> QuizResults(){return View(await db.QuizResults.Include(x=>x.Student).Include(x=>x.Quiz).OrderByDescending(x=>x.CompletedAtUtc).ToListAsync());}
    [HttpGet] public async Task<IActionResult> TestResults(){return View(await db.TestResults.Include(x=>x.Student).Include(x=>x.Test).OrderByDescending(x=>x.CompletedAtUtc).ToListAsync());}
    [HttpGet] public async Task<IActionResult> Live(){var days=await db.CurriculumDays.Where(x=>x.IsPublished).OrderBy(x=>x.DayNumber).ToListAsync();return View(days);}
}
