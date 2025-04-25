using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarshipBattle.Areas.Identity.Pages.Account.Manage;
using WarshipBattle.Data;
using WarshipBattle.Models;
using WarshipBattle.ViewModels;

namespace WarshipBattle.Controllers
{
    public class GameController : Controller
    {
        public IActionResult GameModes()
        {
            return View();
        }
        public IActionResult PlayAgainstBot()
        {
            return View();
        }
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public GameController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> PlayAgainstPlayer()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Home");
            }

            var friends = await _context.Friendships
                .Where(f => f.User1Id == userId || f.User2Id == userId)
                .Select(f => f.User1Id == userId ? f.User2 : f.User1)
                .ToListAsync();

            var model = new FriendsViewModel
            {
                Friends = friends ?? new List<IdentityUser>()
            };

            return View(model);
        }
        public async Task<IActionResult> PrivateRoom(int roomId)
        {
            var userId = _userManager.GetUserId(User);
            var invitation = await _context.GameInvitations.FindAsync(roomId);

            if (invitation == null || (invitation.SenderId != userId && invitation.ReceiverId != userId))
            {
                return RedirectToAction("PlayAgainstPlayer");
            }

            return View("PrivateRoom", roomId);
        }


    }
}
