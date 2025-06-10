using EIUBetApp.Data;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EIUBetApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly EIUBetAppContext _context;
        public AdminController(EIUBetAppContext context)
        {
            _context = context;
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
        public IActionResult CreateRoom(string RoomName, int Capacity, Guid GameId)
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
            _context.SaveChanges();

            return RedirectToAction("RoomManager");
        }

        // ban may thk lol hack
        //[HttpPost]
        //public async Task<IActionResult> TogglePlayerAvailability(Guid playerId, bool status)
        //{
        //    var player = await _context.Player.FindAsync(playerId);
        //    if (player != null)
        //    {
        //        player.IsAvailable = status;
        //        await _context.SaveChangesAsync();

        //        // Gửi signalR nếu muốn thông báo realtime
        //        await _context.Clients.All.SendAsync("PlayerBanned", playerId, !status); // true = bị cấm
        //    }

        //    return RedirectToAction("PlayerManager");
        //}

    }
}
