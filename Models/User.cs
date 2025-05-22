using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class User
    {
        [Key]
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Phone]
        [StringLength(10)]
        public string Phone { get; set; }

        [Required]
        [StringLength(255)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        public bool IsDelete { get; set; }

        [Required]
        public int Role { get; set; }

        public Player? Player { get; set; }
        public Admin? Admin { get; set; }
        public ICollection<ManageRoom> ?RoomManagements { get; set; }
    }
}
