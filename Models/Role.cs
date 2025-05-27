using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EIUBetApp.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }

        public string? Description { get; set; }

        //Navigation
        public ICollection<UserRole>? UserRoles { get; set; }

    }
}
