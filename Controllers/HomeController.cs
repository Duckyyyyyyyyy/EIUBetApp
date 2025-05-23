using EIUBetApp.Data;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EIUBetApp.Controllers
{
    public class HomeController : Controller
    {
        //private readonly ILogger<HomeController> _logger;
        private readonly EIUBetAppContext _context;
        //private readonly SignInManager<IdentityUser> _signInManager;

        public HomeController(EIUBetAppContext context)
        {
            _context = context;
            //_signInManager = signInManager;
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

        // Login action
        //[HttpPost]
        //public async Task<IActionResult> Login(LoginViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var result = await _signInManager.PasswordSignInAsync(
        //            model.Email,
        //            model.Password,
        //            isPersistent: false,       // set to true if you want "Remember Me"
        //            lockoutOnFailure: false
        //        );

        //        if (result.Succeeded)
        //        {
        //            return RedirectToAction("Index", "Home");
        //        }
        //    }
        //    return View(model);
        //}

        //public async Task<IActionResult> Logout()
        //{
        //    await _signInManager.SignOutAsync();
        //    return RedirectToAction("Index", "Home");
        //}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
