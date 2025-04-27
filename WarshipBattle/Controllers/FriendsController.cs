using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarshipBattle.Data;
using WarshipBattle.Models;

namespace WarshipBattle.Controllers
{
    public class FriendsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FriendsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Siųsti kvietimą
        public async Task<IActionResult> SendRequest(string username)
        {
            var senderId = _userManager.GetUserId(User);
            var receiver = await _userManager.FindByNameAsync(username);
            if (receiver == null)
            {
                return NotFound("Vartotojas nerastas.");
            }

            // Check if the receiver is already a friend
            var isAlreadyFriend = await _context.Friendships
            .AnyAsync(f => (f.User1Id == senderId && f.User2Id == receiver.Id) ||
            (f.User1Id == receiver.Id && f.User2Id == senderId));

            if (isAlreadyFriend)
            {
                return BadRequest("Šis vartotojas jau yra jūsų draugų sąraše.");
            }

            // Check if a friend request has already been sent
            var existingRequest = await _context.FriendRequests
            .FirstOrDefaultAsync(fr => fr.SenderUserId == senderId && fr.ReceiverUserId == receiver.Id);

            if (existingRequest != null)
            {
                return BadRequest("Jūs jau išsiuntėte kvietimą šiam vartotojui.");
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

            return Ok("Kvietimas išsiųstas.");
        }

        // Priimti kvietimą
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var request = await _context.FriendRequests.FindAsync(requestId);
            if (request == null || request.ReceiverUserId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            request.IsAccepted = true;
            var friendship = new Friendship
            {
                User1Id = request.SenderUserId,
                User2Id = request.ReceiverUserId
            };
            _context.Friendships.Add(friendship);
            _context.FriendRequests.Update(request);
            await _context.SaveChangesAsync();

            return Ok("Draugystė patvirtinta.");
        }

        // Atmesti kvietimą
        public async Task<IActionResult> DeclineRequest(int requestId)
        {
            var request = await _context.FriendRequests.FindAsync(requestId);
            if (request == null || request.ReceiverUserId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();

            return Ok("Kvietimas atmestas.");
        }

        // Gauti draugų sąrašą
        public async Task<IActionResult> GetFriends()
        {
            var userId = _userManager.GetUserId(User);
            var friends = await _context.Friendships
            .Where(f => f.User1Id == userId || f.User2Id == userId)
            .Select(f => new
            {
                FriendId = f.User1Id == userId ? f.User2Id : f.User1Id
            })
            .ToListAsync();

            return Ok(friends);
        }
    }
}
