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

            // ✅ Gọi thông qua method trung gian (HubExtensions)
            await HubExtensions.NotifyNewRoomCreated(_hubContext, newRoom, game);

            return RedirectToAction("RoomManager");
        }
        //[HttpPost]
        //public async Task<IActionResult> DeleteRoom(Guid roomId)
        //{
        //    var room = await _context.Room.FindAsync(roomId);
        //    if (room != null)
        //    {
        //        _context.Room.Remove(room);
        //        await _context.SaveChangesAsync();

        //        // Gửi signalR thông báo xóa phòng
        //        await HubExtensions.NotifyRoomDeleted(_hubContext, roomId);
        //    }

        //    return RedirectToAction("RoomManager");
        //}

        [HttpPost]
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
