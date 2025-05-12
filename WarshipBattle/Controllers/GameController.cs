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
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public GameController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IActionResult GameModes()
        {
            return View();
        }

        public IActionResult PlayAgainstBot()
        {
            return View();
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

            var playerStats = await _context.PlayerStats.FirstOrDefaultAsync(ps => ps.UserId == userId);
            if (playerStats == null)
            {
                playerStats = new PlayerStats
                {
                    UserId = userId,
                    Victories = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.PlayerStats.Add(playerStats);
                await _context.SaveChangesAsync();
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

        [HttpPost]
        public async Task<IActionResult> RecordVictory(int roomId)
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

            var invitation = await _context.GameInvitations.FindAsync(roomId);
            if (invitation == null || (invitation.SenderId != userId && invitation.ReceiverId != userId))
            {
                return RedirectToAction("PlayAgainstPlayer");
            }

            var playerStats = await _context.PlayerStats.FirstOrDefaultAsync(ps => ps.UserId == userId);
            if (playerStats == null)
            {
                playerStats = new PlayerStats
                {
                    UserId = userId,
                    Victories = 1,
                    LastUpdated = DateTime.UtcNow
                };
                _context.PlayerStats.Add(playerStats);
            }
            else
            {
                playerStats.Victories += 1;
                playerStats.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("PlayAgainstPlayer");
        }

        public async Task<IActionResult> LeaderBoard()
        {
            var allUsers = await _userManager.Users.ToListAsync();

            foreach (var user in allUsers)
            {
                var playerStats = await _context.PlayerStats.FirstOrDefaultAsync(ps => ps.UserId == user.Id);
                if (playerStats == null)
                {
                    playerStats = new PlayerStats
                    {
                        UserId = user.Id,
                        Victories = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.PlayerStats.Add(playerStats);
                }
            }
            await _context.SaveChangesAsync();

            var topPlayers = await _context.PlayerStats
                .OrderByDescending(ps => ps.Victories)
                .Take(10)
                .ToListAsync();

            var leaderboardData = new List<(string Username, int Victories)>();
            foreach (var stats in topPlayers)
            {
                var user = await _userManager.FindByIdAsync(stats.UserId);
                leaderboardData.Add((user?.UserName ?? stats.UserId, stats.Victories));
            }

            return View(leaderboardData);
        }
    }
}
