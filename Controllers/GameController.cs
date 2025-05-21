using EIUBetApp.Data;
using Microsoft.AspNetCore.Mvc;

namespace EIUBetApp.Controllers
{
    public class GameController : Controller
    {
        private readonly EIUBetAppContext _context;
        public GameController(EIUBetAppContext context)
        {
            _context = context;
        }
        public IActionResult Index(string room)
        {
            ViewBag.RoomName = room;
            return View();
        }
        public IActionResult BauCua(string room)
        {
            ViewBag.RoomName = room;
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

