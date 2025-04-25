using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WarshipBattle.Data;
using WarshipBattle.Models;
using System.Linq;
using System.Threading.Tasks;

namespace WarshipBattle.Areas.Identity.Pages.Account.Manage
{
    public class FriendRequestsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FriendRequestsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<FriendRequestViewModel> Requests { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            Requests = await _context.FriendRequests
                .Where(r => r.ReceiverUserId == user.Id && !r.IsAccepted)
                .Select(r => new FriendRequestViewModel
                {
                    Id = r.Id,
                    SenderUserName = _context.Users.Where(u => u.Id == r.SenderUserId).Select(u => u.UserName).FirstOrDefault()
                })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAcceptRequestAsync(int requestId)
        {
            var request = await _context.FriendRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            var friendship = new Friendship
            {
                User1Id = request.SenderUserId,
                User2Id = request.ReceiverUserId
            };

            _context.Friendships.Add(friendship);
            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeclineRequestAsync(int requestId)
        {
            var request = await _context.FriendRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }

    public class FriendRequestViewModel
    {
        public int Id { get; set; }
        public string SenderUserName { get; set; }
    }
}
