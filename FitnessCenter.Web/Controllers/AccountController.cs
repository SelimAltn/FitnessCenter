using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Web.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
