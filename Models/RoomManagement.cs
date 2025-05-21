using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class RoomManagement
    {
        [Key]
        public Guid RoomId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int NumberPlayers { get; set; }

        public Room Room { get; set; }
        public User User { get; set; }
    }
}
