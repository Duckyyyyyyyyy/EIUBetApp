using EIUBetApp.Data;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static EIUBetApp.Data.EIUBetAppHub;

namespace EIUBetApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly EIUBetAppContext _context;
        private readonly IHubContext<EIUBetAppHub> _hubContext;
        public AdminController(EIUBetAppContext context, IHubContext<EIUBetAppHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Dashboard()
        {
            if (!User.IsInRole("Admin"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }


        public IActionResult RoomManager()
        {
            var games = _context.Game.ToList();
            var rooms = _context.Room.Include(r => r.Game).ToList();
            ViewBag.Games = games;
            return View(rooms);
        }

        public IActionResult PlayerManager()
        {
            var players = _context.Player.Include(p => p.User).ToList();
            return View(players);
        }

        // tao phong
        [HttpPost]
        public async Task<IActionResult> CreateRoom(string RoomName, int Capacity, Guid GameId)
        {
            var newRoom = new Room
            {
                RoomId = Guid.NewGuid(),
                RoomName = RoomName,
                Capacity = Capacity,
                GameId = GameId,
                IsAvailable = true,
                IsDeleted = false
            };

            _context.Room.Add(newRoom);
            await _context.SaveChangesAsync();

            var game = await _context.Game.FindAsync(GameId);
            await HubExtensions.NotifyNewRoomCreated(_hubContext, newRoom, game);

            return RedirectToAction("RoomManager");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleRoomStatus(Guid roomId, bool isDeleted)
        {
            var room = await _context.Room.FindAsync(roomId);
            if (room != null)
            {
                room.IsDeleted = isDeleted;
                await _context.SaveChangesAsync();

                if (isDeleted)
                {
                    await HubExtensions.NotifyRoomVisibilityChanged(_hubContext, room.RoomId, true);
                }
                else
                {
                    var game = await _context.Game.FindAsync(room.GameId);
                    await HubExtensions.NotifyRoomVisibilityChanged(_hubContext, room.RoomId, false, room, game);
                }
            }

            return RedirectToAction("RoomManager");
        }

        // add balance
        [HttpPost]
        public async Task<IActionResult> AddBalance(Guid playerId, decimal amount)
        {
           
            if (amount <= 0) return BadRequest("Amount must be positive.");

            var player = await _context.Player.FindAsync(playerId);
            if (player is null) return NotFound();

            player.Balance += amount;
            await _context.SaveChangesAsync();

            // push real-time update to every client
            await _hubContext.Clients.All.SendAsync(
                "UpdatePlayerBalance",
                new { playerId, newBalance = player.Balance });

            return Ok(new { playerId, player.Balance });
        }

    }
}
