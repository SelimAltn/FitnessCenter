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
            
            // Şube müdürleri ana siteye erişemez, BranchManager paneline yönlendir
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("BranchManager"))
            {
                return RedirectToAction("Index", "Home", new { area = "BranchManager" });
            }
            
            return View();
        }

        public IActionResult About()
        {
            return View();
        }
    }
}
