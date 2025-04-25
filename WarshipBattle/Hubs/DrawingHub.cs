using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WarshipBattle.Data;

namespace WarshipBattle.Hubs
{
    public class DrawingHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public DrawingHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendDrawing(int roomId, float x, float y, float lastX, float lastY)
        {
            // Siunčiame piešimo informaciją visiems, kas yra grupėje
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveDrawing", x, y, lastX, lastY);
        }

        public async Task JoinGameRoom(int roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
            await Clients.Group(roomId.ToString()).SendAsync("GameRoomJoined", "A player has joined the room.");
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                var invitation = _context.GameInvitations.FirstOrDefault(i => i.SenderId == userId || i.ReceiverId == userId);

                if (invitation != null)
                {
                    string groupName = invitation.Id.ToString();
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                    // Patikrinti, ar grupė tuščia
                    var usersInGroup = await _context.Users
                        .Where(u => _context.GameInvitations.Any(i => (i.SenderId == u.Id || i.ReceiverId == u.Id) && i.Id == invitation.Id))
                        .ToListAsync();

                    if (usersInGroup.Count == 1) // Jeigu tik vienas liko, ištrinam
                    {
                        _context.GameInvitations.Remove(invitation);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
        public async Task LeaveRoom(int roomId)
        {
            string groupName = roomId.ToString();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("UserLeft", "A user has left the room.");
        }
    }
}
