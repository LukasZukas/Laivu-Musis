using Microsoft.AspNetCore.Identity;

namespace WarshipBattle.Models
{
    public class FriendRequest
    {
        public int Id { get; set; }
        public string SenderUserId { get; set; }
        public string ReceiverUserId { get; set; }
        public DateTime SentDate { get; set; }
        public bool IsAccepted { get; set; }

        public virtual IdentityUser Sender { get; set; }
        public virtual IdentityUser Receiver { get; set; }
    }
}
