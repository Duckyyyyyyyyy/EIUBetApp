using Microsoft.AspNetCore.Mvc;

namespace EIUBetApp.Controllers
{
    public class GameController : Controller
    {
        public IActionResult Index(string room)
        {
            ViewBag.RoomName = room;
            return View();
        }
    }
}
