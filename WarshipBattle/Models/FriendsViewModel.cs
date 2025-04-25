using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace WarshipBattle.ViewModels
{
    public class FriendsViewModel
    {
        public FriendsViewModel(List<IdentityUser> friends)
        {
            Friends = friends;
        }

        public FriendsViewModel()
        {
        }

        public List<IdentityUser> Friends { get; set; } = new();
    }
}
