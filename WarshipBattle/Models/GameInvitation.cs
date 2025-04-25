using System;
using System.ComponentModel.DataAnnotations;

namespace WarshipBattle.Models
{
    public class GameInvitation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }  // Kas siuntė pakvietimą

        [Required]
        public string ReceiverId { get; set; }  // Kas gavo pakvietimą

        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;  // Kada buvo išsiųsta

        public bool IsAccepted { get; set; } = false;  // Ar pakvietimas priimtas
        public string Status { get; internal set; }
    }
}

