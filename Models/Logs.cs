using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class Logs
    {
        [Key]
        public Guid LogId { get; set; }

        [Required]
        public Guid PlayerId { get; set; }

        [Required]
        [StringLength(50)]
        public string Choice { get; set; }

        [Required]
        [StringLength(20)]
        public string GameResult { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public bool IsDelete { get; set; }

        [Required]
        public Guid GameId { get; set; }

        public Player Player { get; set; }
        public Room Room { get; set; }

        public Game Game { get; set; }
    }
}
