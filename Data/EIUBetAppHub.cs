using Microsoft.AspNetCore.SignalR;
using EIUBetApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EIUBetApp.Data
{
    public class EIUBetAppHub : Hub
    {
        private readonly EIUBetAppContext _context;

        public EIUBetAppHub(EIUBetAppContext context)
        {
            _context = context;
        }

        public async Task JoinRoom(string roomId, string playerId)
        {
            var connectionId = Context.ConnectionId;

            var join = new ManageRoom
            {
                PlayerId = Guid.Parse(playerId),
                RoomId = Guid.Parse(roomId),
                JoinAt = DateTime.UtcNow,
                LeaveAt = null
            };

            _context.ManageRoom.Add(join);
            await _context.SaveChangesAsync();

            await Groups.AddToGroupAsync(connectionId, roomId);

            var players = await _context.ManageRoom
                .Where(r => r.RoomId == Guid.Parse(roomId) && r.LeaveAt == null)
                .Include(r => r.Player) 
                .Select(r => new
                {
                    PlayerId = r.PlayerId,
                    Balance = r.Player.Balance
                })
                .ToListAsync();



            await Clients.Group(roomId).SendAsync("UpdatePlayerList", players);
        }

        public async Task LeaveRoom(string roomId, string playerId)
        {
            var connectionId = Context.ConnectionId;

            var manageRoomEntry = await _context.ManageRoom
                .Where(r => r.RoomId == Guid.Parse(roomId)
                            && r.PlayerId == Guid.Parse(playerId)
                            && r.LeaveAt == null)
                .FirstOrDefaultAsync();

            if (manageRoomEntry != null)
            {
                manageRoomEntry.LeaveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            await Groups.RemoveFromGroupAsync(connectionId, roomId);

            var players = await _context.ManageRoom
                .Where(r => r.RoomId == Guid.Parse(roomId) && r.LeaveAt == null)
                .Select(r => new
                {
                    PlayerId = r.PlayerId,
                    //Username = r.Player.Username,
                    Balance = r.Player.Balance
                })
                .ToListAsync();

            await Clients.Group(roomId).SendAsync("UpdatePlayerList", players);
        }


    }
}
