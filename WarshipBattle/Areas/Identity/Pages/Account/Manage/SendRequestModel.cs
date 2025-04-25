using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using WarshipBattle.Data;
using WarshipBattle.Models;
using Microsoft.EntityFrameworkCore;

namespace WarshipBattle.Areas.Identity.Pages.Account.Manage
{
    public class SendRequestModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SendRequestModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public string Username { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var senderId = _userManager.GetUserId(User);
            var receiver = await _userManager.FindByNameAsync(Username);

            if (receiver == null)
            {
                ModelState.AddModelError(string.Empty, "Vartotojas nerastas.");
                return Page();
            }

            var existingRequest = await _context.FriendRequests
                .FirstOrDefaultAsync(fr => fr.SenderUserId == senderId && fr.ReceiverUserId == receiver.Id);

            if (existingRequest != null)
            {
                ModelState.AddModelError(string.Empty, "Jūs jau išsiuntėte kvietimą šiam vartotojui.");
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

            return RedirectToPage("./FriendRequests");
        }
    }

}
