using System.Security.Claims;
using AIClassroom.Data;
using AIClassroom.Models;
using AIClassroom.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AIClassroom.Controllers;
public class AccountController(AppDbContext db, PasswordService passwords, LoginThrottle throttle, ILogger<AccountController> logger) : Controller
{
    [HttpGet] public IActionResult Login(string? returnUrl = null) => View(new StudentLoginViewModel());
    private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    private const string LockedMessage = "Too many failed attempts. Please wait a few minutes and try again.";

    [HttpPost] [ValidateAntiForgeryToken] [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(StudentLoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);
        var name = (model.Name ?? "").Trim(); var roll = (model.RollNumber ?? "").Trim();
        var account = $"s|{name}|{roll}|{model.ClassNumber}";
        if (throttle.IsLocked(ClientIp, account)) { ModelState.AddModelError("", LockedMessage); return View(new StudentLoginViewModel { Name = model.Name, RollNumber = model.RollNumber, ClassNumber = model.ClassNumber }); }

        var student = await db.Students.FirstOrDefaultAsync(s => s.IsActive && s.Name == name && s.RollNumber == roll && s.ClassNumber == model.ClassNumber);
        // Always run one PBKDF2 verification so response time does not reveal whether the account exists.
        var ok = passwords.Verify(model.Pin ?? "", student?.PinHash ?? passwords.DummyHash);
        if (student == null || !ok)
        {
            throttle.RecordFailure(ClientIp, account);
            logger.LogWarning("Failed student login from {Ip}", ClientIp);
            ModelState.AddModelError("", "Name, roll number, class or PIN is incorrect.");
            return View(new StudentLoginViewModel { Name = model.Name, RollNumber = model.RollNumber, ClassNumber = model.ClassNumber });
        }
        throttle.RecordSuccess(ClientIp, account);
        await SignIn(student.Id.ToString(), student.Name, "Student", student.GroupName, student.ClassNumber.ToString());
        student.LastActiveUtc = DateTime.UtcNow; await db.SaveChangesAsync();
        return RedirectToAction("Index", "Student");
    }
    [HttpGet] public IActionResult AdminLogin() => View(new AdminLoginViewModel());
    [HttpPost] [ValidateAntiForgeryToken] [EnableRateLimiting("login")]
    public async Task<IActionResult> AdminLogin(AdminLoginViewModel model)
    {
        var username = (model.Username ?? "").Trim();
        var account = $"t|{username}";
        if (throttle.IsLocked(ClientIp, account)) { ModelState.AddModelError("", LockedMessage); return View(new AdminLoginViewModel { Username = model.Username }); }

        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.IsActive && t.Username == username);
        var ok = passwords.Verify(model.Password ?? "", teacher?.PasswordHash ?? passwords.DummyHash);
        if (teacher == null || !ok)
        {
            throttle.RecordFailure(ClientIp, account);
            logger.LogWarning("Failed teacher login for '{User}' from {Ip}", username, ClientIp);
            ModelState.AddModelError("", "Invalid username or password.");
            return View(new AdminLoginViewModel { Username = model.Username });
        }
        throttle.RecordSuccess(ClientIp, account);
        logger.LogInformation("Teacher login: {User} from {Ip}", username, ClientIp);
        await SignIn(teacher.Id.ToString(), teacher.Name, teacher.Role, "", "", teacher.ProfileImagePath);
        return RedirectToAction("Index", "Admin");
    }
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
    public IActionResult AccessDenied() => View();
    private async Task SignIn(string id, string name, string role, string group, string classNumber, string profileImage = "")
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier,id), new(ClaimTypes.Name,name), new(ClaimTypes.Role,role), new("Group",group), new("ClassNumber",classNumber), new("ProfileImage",profileImage ?? "") };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    }
}
