using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class Room
    {
        [Key]
        public Guid RoomId { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        public bool IsAvailable { get; set; }

        [Required]
        public bool IsDelete { get; set; }
        [Required]
        public Guid GameId { get; set; }

        public Game Game { get; set; }
        public ICollection<Player> Players { get; set; }
        public ICollection<Logs> Logs { get; set; }
        public ICollection<ManageRoom> ManageRooms { get; set; }
    }
}
