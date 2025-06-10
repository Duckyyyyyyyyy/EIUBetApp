using EIUBetApp.Data;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            var currentPlayerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var currentPlayer = _context.Player
                .Include(p => p.User)
                .FirstOrDefault(p => p.PlayerId == currentPlayerId);

            ViewBag.CurrentPlayer = currentPlayer;

            var players = _context.Player
                .Include(p => p.User)
                .Where(p => p.IsAvailable == true)
                .ToList();

            var rooms = _context.Room
                .Where(r => r.GameId == gameId && r.IsAvailable == true)
                .ToList();

            var model = new Tuple<IEnumerable<Player>, IEnumerable<Room>>(players, rooms);
            return View(model);
        }




    }
}