using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Eğitmenler ana siteye erişemez, Trainer paneline yönlendir
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Trainer"))
            {
                return RedirectToAction("Index", "Home", new { area = "Trainer" });
            }
            
            return View();
        }
    }
}
