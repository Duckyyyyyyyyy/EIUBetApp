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
            // Get current user id
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get current player (single)
            var currentPlayer = _context.Player
                .Include(p => p.User) // Include User to access username, etc.
                .FirstOrDefault(p => p.UserId == currentUserId);

            ViewBag.CurrentPlayer = currentPlayer;

            // Get all available players and rooms, user is not deleted
            var players = _context.Player.Include(p => p.User).Where(p => p.IsAvailable == true && p.User.IsDeleted == false).ToList();
            var rooms = _context.Room.Where(r => r.GameId == gameId && r.IsAvailable == true).ToList();

            var model = new Tuple<IEnumerable<Player>, IEnumerable<Room>>(players, rooms);
            return View(model);
        }



    }
}
