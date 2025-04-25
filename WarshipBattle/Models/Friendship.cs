using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarshipBattle.Models
{
    public class Friendship
    {
        public int Id { get; set; }
        public string User1Id { get; set; }
        public string User2Id { get; set; }

        public virtual IdentityUser User1 { get; set; }
        public virtual IdentityUser User2 { get; set; }
    }
}
