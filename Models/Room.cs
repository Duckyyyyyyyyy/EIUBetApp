using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EIUBetApp.Models
{
    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
        public ICollection<Logs> Logs { get; set; }
        public ICollection<ManageRoom> ManageRooms { get; set; }
    }
}
