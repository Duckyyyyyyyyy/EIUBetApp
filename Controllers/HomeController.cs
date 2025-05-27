using EIUBetApp.Data;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace EIUBetApp.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly EIUBetAppContext _context;

        public HomeController(EIUBetAppContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var games = _context.Game.ToList();

            return View(games);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
