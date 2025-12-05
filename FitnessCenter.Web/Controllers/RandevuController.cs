using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = "MemberOnly")]
public class RandevuController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
