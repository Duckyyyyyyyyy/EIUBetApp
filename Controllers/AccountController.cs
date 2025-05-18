using Microsoft.AspNetCore.Mvc;
using EIUBetApp.Models;

namespace EIUBetApp.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                //integrate after be is done
                if (model.Email == "admin@eiubet.com" && model.Password == "admin")
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO: Save user data
                return RedirectToAction("Login");
            }

            return View(model);
        }
    }
}
