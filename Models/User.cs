using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EIUBetApp.Models
{
    public class User
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Phone]
        [StringLength(10)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public bool IsDelete { get; set; }

        [Required]
        public int Role { get; set; }

        public Player? Player { get; set; }
        public Admin? Admin { get; set; }
    }
}
