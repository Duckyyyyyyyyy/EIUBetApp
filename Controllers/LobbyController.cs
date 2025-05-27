using EIUBetApp.Data;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EIUBetApp.Controllers
{
    [Authorize(Roles = "Player, Admin")]
    public class LobbyController : Controller
    {
        private readonly EIUBetAppContext _context;

        public LobbyController(EIUBetAppContext context)
        {
            _context = context;
        }

        public IActionResult Index(Guid gameId)
        {
            var players = _context.Player.Include(p => p.User).ToList();
            var rooms = _context.Room.Where(r => r.GameId == gameId).ToList();
            var model = new Tuple<IEnumerable<Player>, IEnumerable<Room>>(players, rooms);
            return View(model);
        }


    }
}
