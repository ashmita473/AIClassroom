using AIClassroom.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AIClassroom.Controllers;
[Authorize]
public class CurriculumController(AppDbContext db) : Controller
{
    [HttpGet] public async Task<IActionResult> Index(string? group){var q=db.CurriculumModules.Include(x=>x.Days).AsQueryable();if(User.IsInRole("Student")){var s=await db.Students.FindAsync(int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));group=s!.GroupName;q=q.Where(x=>x.GroupName==s.GroupName&&x.ClassMin<=s.ClassNumber&&x.ClassMax>=s.ClassNumber);}else if(group!=null)q=q.Where(x=>x.GroupName==group);return View(await q.OrderBy(x=>x.SortOrder).ToListAsync());}
    [HttpGet] public async Task<IActionResult> Day(int id)
    {
        var d=await db.CurriculumDays.Include(x=>x.Module).Include(x=>x.Lesson!).ThenInclude(x=>x.Activities).FirstOrDefaultAsync(x=>x.Id==id);
        if(d==null)return NotFound();
        if(User.IsInRole("Student"))
        {
            // Students may only open published, unlocked days of their own group/class.
            var s=await db.Students.FindAsync(int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));
            if(s==null||d.Module==null||!d.IsPublished||d.IsLocked||d.Module.GroupName!=s.GroupName||d.Module.ClassMin>s.ClassNumber||d.Module.ClassMax<s.ClassNumber)return Forbid();
        }
        if(User.IsInRole("Student")) return RedirectToAction("Lesson", "Student", new { id = d.Id });
        return View(d);
    }
}
