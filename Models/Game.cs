using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class Game
    {
        [Key]
        public Guid GameId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public ICollection<Room> Rooms { get; set; }
        public ICollection<Logs> Logs { get; set; }

    }
}
