using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class Admin
    {
        [Key]
        public Guid AdminId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public User User { get; set; }
    }
}
