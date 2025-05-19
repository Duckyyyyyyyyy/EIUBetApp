using EIUBetApp.Data;
using Microsoft.AspNetCore.Mvc;

namespace EIUBetApp.Controllers
{
    public class GameController : Controller
    {
        private readonly EIUBetAppContext _context;
        public GameController(EIUBetAppContext context)
        {
            _context = context;
        }
        public IActionResult Index(string room)
        {
            ViewBag.RoomName = room;
            return View();
        }
        public IActionResult BauCua(string room)
        {
            ViewBag.RoomName = room;
            return View();
        }
    }
}
