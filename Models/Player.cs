using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class Player
    {
        [Key]
        public Guid PlayerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        [Required]
        public bool ReadyStatus { get; set; }

        [Required]
        public bool OnlineStatus { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public Room Room { get; set; }
        public User User { get; set; }
    }
}
