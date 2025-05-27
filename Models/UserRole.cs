using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class UserRole
    {
        [Key]
        public Guid UserRoleId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int RoleId { get; set; }

        //Navigation
        public Role? Role { get; set; }

        public User? User { get; set; }

    }
}
