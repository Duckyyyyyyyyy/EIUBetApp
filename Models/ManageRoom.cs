using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class ManageRoom
    {
        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public Guid PlayerId { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime JoinAt { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? LeaveAt { get; set; }
        public Room Room { get; set; }
        public Player Player { get; set; }
    }
}
