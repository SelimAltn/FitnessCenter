using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
