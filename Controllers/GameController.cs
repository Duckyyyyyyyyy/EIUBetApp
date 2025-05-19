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
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult BauCua()
        {
            return View();
        }
    }
}
