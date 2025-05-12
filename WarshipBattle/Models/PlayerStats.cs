using System.ComponentModel.DataAnnotations;

namespace WarshipBattle.Models
{
    public class PlayerStats
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public int Victories { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
