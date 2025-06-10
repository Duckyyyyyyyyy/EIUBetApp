using EIUBetApp.Data;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Data;
using System.Security.Claims;

namespace EIUBetApp.Controllers
{
    [Authorize(Roles = "Player")]
    public class GameController : Controller
    {
        private readonly EIUBetAppContext _context;
        private readonly IHubContext<EIUBetAppHub> _hubContext;
        private static readonly Random rand = new();

        public GameController(EIUBetAppContext context, IHubContext<EIUBetAppHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
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
        public async Task<JsonResult> BauCuaSpin(int prediction, int betAmount, Guid roomId)
        {

            if (prediction < 1 || prediction > 6)
            {
                return Json(new { error = "Invalid prediction. Must be between 1 and 6." });
            }

            if (betAmount <= 0)
            {
                return Json(new { error = "Bet amount must be greater than 0." });
            }

            var playerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var player = _context.Player.SingleOrDefault(p => p.PlayerId == Guid.Parse(playerId));
            if (player == null)
                return Json(new { error = "Player not found." });

            if (player.Balance < betAmount)
            {
                return Json(new { error = "Insufficient balance." });
            }

            // Generate dice results
            int[] diceResults = new int[3];
            for (int i = 0; i < 3; i++)
            {
                diceResults[i] = rand.Next(1, 7);
            }

            // Count matches
            int matchCount = diceResults.Count(dice => dice == prediction);

            // Calculate winnings based on traditional Bau Cua rules
            int winnings = 0;
            if (matchCount > 0)
            {
                // Player wins: gets back bet + (bet * match count)
                winnings = betAmount + (betAmount * matchCount);
                player.Balance = player.Balance - betAmount + winnings;
            }
            else
            {
                // Player loses the bet
                player.Balance -= betAmount;
            }

            // Save changes to database
            await _context.SaveChangesAsync();

            // Notify all players in the room about balance update
            await _hubContext.Clients.Group(roomId.ToString()).SendAsync("PlayerBalanceUpdated",
                player.PlayerId, player.Balance);

            // Map numbers to names for display
            var nameMap = new Dictionary<int, string>
            {
                { 1, "Bầu" },
                { 2, "Nai" },
                { 3, "Cua" },
                { 4, "Cá" },
                { 5, "Tôm" },
                { 6, "Gà" }
            };

            var resultNames = diceResults.Select(num => nameMap[num]).ToArray();
            var predictionName = nameMap[prediction];

            return Json(new
            {
                success = true,
                diceResults = diceResults,
                resultNames = resultNames,
                prediction = prediction,
                predictionName = predictionName,
                matchCount = matchCount,
                betAmount = betAmount,
                winnings = winnings,
                newBalance = player.Balance,
                won = matchCount > 0
            });
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

            dice = new[] { rand.Next(1, 7), rand.Next(1, 7), rand.Next(1, 7) };
            sum = dice.Sum();
            result = GetResult(sum);

            if (result == prediction)
            {
                player.Balance += betAmount;
            }
            else if (result == "hoa")
            {
                // no change
            }
            else
            {
                player.Balance -= betAmount;
            }

            _context.SaveChanges();

            return Json(new
            {
                dice,
                sum,
                result,
                balance = player.Balance
            });
        }
    }
}