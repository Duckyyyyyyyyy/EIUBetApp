using Microsoft.AspNetCore.Mvc;

namespace EIUBetApp.Controllers
{
    public class LobbyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
