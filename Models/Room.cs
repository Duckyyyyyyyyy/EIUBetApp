using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EIUBetApp.Models
{
    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid RoomId { get; set; }
        public string? RoomName { get; set; }

        [Required]
        public int Capacity { get; set; }

        public bool IsAvailable { get; set; }

        public bool IsDeleted { get; set; }
        [Required]
        public Guid GameId { get; set; }

        //Navigation
        public Game Game { get; set; }
        public ICollection<Logs> Logs { get; set; }
        public ICollection<ManageRoom> ManageRooms { get; set; }
    }
}
