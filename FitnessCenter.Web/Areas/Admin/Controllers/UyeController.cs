using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UyeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
