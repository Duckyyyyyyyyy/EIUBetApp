using EIUBetApp.Data;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EIUBetApp.Controllers
{
    [Authorize(Roles ="Player,Admin")]
    public class GameController : Controller
    {
        private readonly EIUBetAppContext _context;
        public GameController(EIUBetAppContext context)
        {
            _context = context;
        }
      
        public IActionResult Index()
        {
            // Access claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // as string
            var email = User.FindFirstValue(ClaimTypes.Email);
            var username = User.FindFirstValue("Username");
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            // Convert userId to Guid if needed
            var userGuid = Guid.Parse(userId);

            // You can also query more data from DB using userGuid
            var player = _context.Player.FirstOrDefault(p => p.UserId == userGuid);

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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var player = _context.Player.SingleOrDefault(p => p.UserId == Guid.Parse(userId));
            if (player == null) return NotFound();

            ViewBag.RoomId = room.RoomId;
            ViewBag.PlayerId = player.PlayerId;

            return View();
        }

        private static int winCount = 0;
        private static int lossCount = 0;
        private static int balance = 100;

        [HttpPost]
        public JsonResult Spin(string prediction)
        {
            var rand = new Random();

            string GetResult(int sum)
            {
                if (sum >= 11 && sum <= 17) return "tai";
                if (sum >= 4 && sum <= 10) return "xiu";
                return "hoa";
            }

            int[] dice;
            int sum;
            string result;

            bool shouldWin;
            bool overWinLimit;

            do
            {
                dice = new[] { rand.Next(1, 7), rand.Next(1, 7), rand.Next(1, 7) };
                sum = dice.Sum();
                result = GetResult(sum);

                shouldWin = result == prediction;
                overWinLimit = winCount >= lossCount;

                // If player "should win" but has already won more or equal times, generate again (fake a loss)
            } while (shouldWin && overWinLimit && result != "hoa");

            // Update counters & balance based on final result
            if (result == prediction)
            {
                winCount++;
                balance += 10;
            }
            else if (result == "hoa")
            {
                // no change
            }
            else
            {
                lossCount++;
                balance -= 10;
            }

            return Json(new
            {
                dice,
                sum,
                result,
                balance,
                winCount,
                lossCount
            });
        }
    }
}

