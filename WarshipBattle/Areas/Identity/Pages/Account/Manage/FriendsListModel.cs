using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WarshipBattle.Data;

namespace WarshipBattle.Areas.Identity.Pages.Account.Manage
{
    public class FriendsListModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public FriendsListModel(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<IdentityUser> Friends { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                Friends = await _context.Friendships
                    .Where(f => f.User1Id == userId || f.User2Id == userId)
                    .Select(f => f.User1Id == userId ? f.User2 : f.User1)
                    .ToListAsync();
            }
        }
        public async Task<IActionResult> OnPostRemoveFriendAsync(string friendId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(friendId))
                return RedirectToPage();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.User1Id == userId && f.User2Id == friendId) ||
                                          (f.User1Id == friendId && f.User2Id == userId));

            if (friendship != null)
            {
                _context.Friendships.Remove(friendship);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
