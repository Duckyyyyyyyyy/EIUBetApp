using Microsoft.AspNetCore.Mvc;

namespace EIUBetApp.Controllers
{
    public class GameController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
