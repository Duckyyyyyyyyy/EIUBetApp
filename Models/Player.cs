﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EIUBetApp.Models
{
    public class Player
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid PlayerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        public bool? ReadyStatus { get; set; }

        public bool? OnlineStatus { get; set; }

        [Required]
        public bool? IsAvailable { get; set; }

        [Required]
        public Guid UserId { get; set; }

        //Navigation
        public User? User { get; set; }
        public ICollection<ManageRoom>? ManageRooms { get; set; }
        public ICollection<Logs>? Logs { get; set; }
    }
}
