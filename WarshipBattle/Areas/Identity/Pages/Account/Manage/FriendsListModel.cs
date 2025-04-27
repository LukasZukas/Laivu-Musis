using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WarshipBattle.Data;
using WarshipBattle.Models;

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

        [BindProperty]
        public string Username { get; set; }

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

        public async Task<IActionResult> OnPostSendRequestAsync()
        {
            var senderId = _userManager.GetUserId(User);
            var currentUser = await _userManager.GetUserAsync(User);
            var receiver = await _userManager.FindByNameAsync(Username);

            // Reload friends list to ensure it persists on error
            if (senderId != null)
            {
                Friends = await _context.Friendships
                .Where(f => f.User1Id == senderId || f.User2Id == senderId)
                .Select(f => f.User1Id == senderId ? f.User2 : f.User1)
                .ToListAsync();
            }

            // Check if trying to send request to self
            if (receiver != null && receiver.Id == senderId)
            {
                ViewData["ErrorMessage"] = "Negalite siųsti draugo kvietimo sau.";
                return Page();
            }

            // Check if user exists
            if (receiver == null)
            {
                ViewData["ErrorMessage"] = "Vartotojas nerastas.";
                return Page();
            }

            // Check if the receiver is already a friend
            var isAlreadyFriend = await _context.Friendships
            .AnyAsync(f => (f.User1Id == senderId && f.User2Id == receiver.Id) ||
            (f.User1Id == receiver.Id && f.User2Id == senderId));

            if (isAlreadyFriend)
            {
                ViewData["ErrorMessage"] = "Šis vartotojas jau yra jūsų draugų sąraše.";
                return Page();
            }

            // Check if a friend request has already been sent
            var existingRequest = await _context.FriendRequests
            .FirstOrDefaultAsync(fr => fr.SenderUserId == senderId && fr.ReceiverUserId == receiver.Id);

            if (existingRequest != null)
            {
                ViewData["ErrorMessage"] = "Jūs jau išsiuntėte kvietimą šiam vartotojui.";
                return Page();
            }

            var friendRequest = new FriendRequest
            {
                SenderUserId = senderId,
                ReceiverUserId = receiver.Id,
                SentDate = DateTime.Now,
                IsAccepted = false
            };

            _context.FriendRequests.Add(friendRequest);
            await _context.SaveChangesAsync();

            return RedirectToPage();
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
