using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Phone]
        [StringLength(10)]
        public string Phone { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [Required]
        public int Role { get; set; }

        public ICollection<Player> Players { get; set; }
        public Admin Admin { get; set; }
        public ICollection<RoomManagement> RoomManagements { get; set; }
    }
}
