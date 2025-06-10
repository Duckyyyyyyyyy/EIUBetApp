using EIUBetApp.Data;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;

namespace EIUBetApp.Controllers
{
    [Authorize(Roles = "Player")]
    public class GameController : Controller
    {
        private readonly EIUBetAppContext _context;
        private static readonly Random rand = new();
        private static int winCount = 0;
        private static int lossCount = 0;

        public GameController(EIUBetAppContext context)
        {
            _context = context;
        }

        public IActionResult Index(Guid RoomId)
        {
            var room = _context.Room.SingleOrDefault(r => r.RoomId == RoomId);
            if (room == null) return NotFound();

            var playerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var username = User.FindFirstValue("Username");
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            var playerGuid = Guid.Parse(playerId);
            var player = _context.Player.FirstOrDefault(p => p.PlayerId == playerGuid);

            ViewBag.Username = username;
            ViewBag.Email = email;
            ViewBag.Balance = player?.Balance ?? 0;
            ViewBag.Roles = roles;

            return View();
        }

        public IActionResult BauCua(Guid RoomId)
        {
            var room = _context.Room.SingleOrDefault(r => r.RoomId == RoomId);
            if (room == null) return NotFound();            

            var playerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var username = User.FindFirstValue("Username");
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            
            var player = _context.Player.SingleOrDefault(p => p.PlayerId == Guid.Parse(playerId));
            if (player == null) return NotFound();

            ViewBag.RoomId = room.RoomId;
            ViewBag.PlayerId = player.PlayerId;
            ViewBag.Username = username;
            ViewBag.Email = email;
            ViewBag.Balance = player?.Balance ?? 0;
            ViewBag.Roles = roles;

            return View();
        }

        [HttpPost]
        public JsonResult Spin(string prediction, int betAmount)
        {
            if (prediction != "tai" && prediction != "xiu")
            {
                return Json(new { error = "Invalid prediction. Must be 'tai' or 'xiu'." });
            }

            var playerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var player = _context.Player.SingleOrDefault(p => p.PlayerId == Guid.Parse(playerId));
            if (player == null) return Json(new { error = "Player not found." });

            int[] dice;
            int sum;
            string result;

            string GetResult(int s) => s >= 11 && s <= 17 ? "tai" : (s >= 4 && s <= 10 ? "xiu" : "hoa");

            do
            {
                dice = new[] { rand.Next(1, 7), rand.Next(1, 7), rand.Next(1, 7) };
                sum = dice.Sum();
                result = GetResult(sum);

            } while (result == prediction && winCount >= lossCount && result != "hoa");

            if (result == prediction)
            {
                winCount++;
                player.Balance += betAmount;
            }
            else if (result == "hoa")
            {
                // no change
            }
            else
            {
                lossCount++;
                player.Balance -= betAmount;
            }

            _context.SaveChanges();

            return Json(new
            {
                dice,
                sum,
                result,
                balance = player.Balance,
                winCount,
                lossCount
            });
        }
    }
}
