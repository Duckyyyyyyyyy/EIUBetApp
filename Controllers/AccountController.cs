using EIUBetApp.Data;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EIUBetApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly EIUBetAppContext _context;

        public AccountController(EIUBetAppContext context)
        {
            _context = context;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if email already exists
            var existingUser = await _context.User.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already in use.");
                return View(model);
            }

            // Create new user
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = model.Email,
                Username = model.UserName,
                Phone = model.Phone,
                IsDeleted = false,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            // Assign default role "Player"
            var defaultRole = await _context.Role.FirstOrDefaultAsync(r => r.RoleName == "Player");
            if (defaultRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.UserId,
                    RoleId = defaultRole.RoleId
                };
                _context.UserRole.Add(userRole);
                await _context.SaveChangesAsync();
            }

            // Create a Player profile after registration
            var player = new Player
            {
                PlayerId = Guid.NewGuid(),
                UserId = user.UserId,
                Balance = 0m,              
                IsAvailable = true,         // Set to default availability
                ReadyStatus = false,        // Optional default
                OnlineStatus = false        // Optional default
            };

            _context.Player.Add(player);
            await _context.SaveChangesAsync();
            return RedirectToAction("Login", "Account");
        }

        //check unique username
        [HttpGet]
        public async Task<IActionResult> CheckUsername(string username)
        {
            var exists = await _context.User.AnyAsync(u => u.Username == username);
            return Json(new { exists });
        }

        //Check email unique
        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            var exists = await _context.User.AnyAsync(u => u.Email == email);
            return Json(new { exists });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // Get user roles
            var roles = await _context.UserRole
                .Where(ur => ur.UserId == user.UserId)
                .Join(_context.Role,
                      ur => ur.RoleId,
                      r => r.RoleId,
                      (ur, r) => r.RoleName)
                .ToListAsync();

            // Create claims
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("Username", user.Username ?? "Unkown Player"),
             };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));               
            }
           
            // Create identity and principal
            var identity = new ClaimsIdentity(claims, "Login");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

       

    }
}
