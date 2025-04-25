using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarshipBattle.Data;
using WarshipBattle.Models;
using Microsoft.AspNetCore.SignalR;
using WarshipBattle.Hubs;

namespace WarshipBattle.Controllers
{
    public class GameInvitationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHubContext<GameHub> _hubContext;

        public GameInvitationController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IHubContext<GameHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> SendInvitation(string receiverId)
        {
            var senderId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId) || senderId == receiverId)
            {
                return BadRequest("Invalid request.");
            }

            var invitation = new GameInvitation
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = "Pending"
            };

            _context.GameInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Invitation sent from {senderId} to {receiverId}");

            // Informuojame gavėją apie kvietimą
            await _hubContext.Clients.All.SendAsync("ReceiveGameInvitation", new
            {
                invitationId = invitation.Id,
                message = "You have a game invitation!",
                receiverId = receiverId
            });

            return Ok(new { message = "Invitation sent!", invitationId = invitation.Id });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptInvitation(int invitationId)
        {
            var invitation = await _context.GameInvitations.FindAsync(invitationId);
            if (invitation == null)
            {
                return NotFound(new { message = "Invitation not found." });
            }
            if (invitation.IsAccepted)
            {
                return BadRequest(new { message = "Invitation is already accepted." });
            }

            invitation.IsAccepted = true;
            await _context.SaveChangesAsync();

            // Gavėjas prisijungia prie kambario
            await _hubContext.Clients.All.SendAsync("JoinGameRoom", invitationId);

            return Ok(new { message = "Invitation accepted. Starting the game!" });
        }

        [HttpPost]
        public async Task<IActionResult> RejectInvitation(int invitationId)
        {
            var invitation = await _context.GameInvitations.FindAsync(invitationId);
            if (invitation != null)
            {
                _context.GameInvitations.Remove(invitation);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Invitation rejected." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllInvitations()
        {
            var allInvitations = _context.GameInvitations;
            _context.GameInvitations.RemoveRange(allInvitations);
            await _context.SaveChangesAsync();
            return Ok(new { message = "All invitations deleted." });
        }
    }
}