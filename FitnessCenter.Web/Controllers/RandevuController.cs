using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Web.Controllers
{
    public class RandevuController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
