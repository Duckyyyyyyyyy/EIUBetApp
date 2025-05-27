using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class RegisterViewModel
    {

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Phone]
        public string? Phone { get; set; }
        [Required]
        public string UserName { get; set; }
    }

}
