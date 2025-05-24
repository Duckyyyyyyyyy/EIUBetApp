namespace EIUBetApp.Models
{
        public class AppUser
        {
            public string Id { get; set; } // unique ID
            public string Email { get; set; }
            public string PasswordHash { get; set; }
            public string Role { get; set; } // "Player" or "Admin"
        }
}
