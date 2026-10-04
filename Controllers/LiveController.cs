using AIClassroom.Data;
using AIClassroom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AIClassroom.Controllers;
[Authorize] public class LiveController(AppDbContext db) : Controller
{
    [Authorize(Roles="SuperAdmin,Teacher")][HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> Start(string group,int classNumber,int dayId){if(group is not("A" or "B")||classNumber<3||classNumber>8||!await db.CurriculumDays.AnyAsync(d=>d.Id==dayId))return BadRequest();var live=new LiveClass{GroupName=group,ClassNumber=classNumber,CurriculumDayId=dayId,Status="Live",StartedAtUtc=DateTime.UtcNow};db.LiveClasses.Add(live);await db.SaveChangesAsync();return RedirectToAction(nameof(Room),new{id=live.Id});}
    [Authorize(Roles="Student")] [HttpGet] public async Task<IActionResult> StudentRoom(){var s=await db.Students.FindAsync(int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));if(s==null)return NotFound();var live=await db.LiveClasses.Include(x=>x.CurriculumDay).FirstOrDefaultAsync(x=>x.Status=="Live"&&x.GroupName==s.GroupName&&x.ClassNumber==s.ClassNumber);return live==null?RedirectToAction("Index","Student"):RedirectToAction(nameof(Room),new{id=live.Id});}
    [HttpGet] public async Task<IActionResult> Room(int id)
    {
        var l=await db.LiveClasses.Include(x=>x.CurriculumDay).ThenInclude(x=>x!.Lesson).ThenInclude(x=>x!.Activities).FirstOrDefaultAsync(x=>x.Id==id);
        if(l==null)return NotFound();
        if(User.IsInRole("Student"))
        {
            // Students may only enter a live class that is running for their own group and class.
            var s=await db.Students.FindAsync(int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));
            if(s==null||l.Status!="Live"||l.GroupName!=s.GroupName||l.ClassNumber!=s.ClassNumber)return Forbid();
        }
        return View(l);
    }
    [Authorize(Roles="SuperAdmin,Teacher")][HttpPost][ValidateAntiForgeryToken] public async Task<IActionResult> End(int id){var l=await db.LiveClasses.FindAsync(id);if(l!=null){l.Status="Ended";l.EndedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();}return RedirectToAction("Index","Admin");}
}
