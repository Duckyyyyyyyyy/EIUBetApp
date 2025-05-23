using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EIUBetApp.Models
{
    public class Admin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid AdminId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public User User { get; set; }
    }
}
