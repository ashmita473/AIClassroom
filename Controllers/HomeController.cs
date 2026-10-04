using Microsoft.AspNetCore.Mvc;
namespace AIClassroom.Controllers;
public class HomeController : Controller { public IActionResult Index() => View(); public IActionResult Error() => View(); }
