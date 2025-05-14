using Microsoft.AspNetCore.Mvc;

namespace EIUBetApp.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
