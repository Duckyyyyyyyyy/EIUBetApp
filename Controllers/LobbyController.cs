using EIUBetApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Authorization;


namespace EIUBetApp.Controllers
{
    [Authorize(Roles = "Player")]
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