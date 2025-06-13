using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EIUBetApp.Models
{
    public class Logs
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid LogId { get; set; }

        [Required]
        public Guid PlayerId { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string GameResult { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime TimeAt { get; set; }
        public Guid RoomId { get; set; }
        public bool IsDelete { get; set; }
        public Guid GameId { get; set; }

        //Navigation
        public Player Player { get; set; }
        public Room? Room { get; set; }
        public Game? Game { get; set; }
    }
}
