using Microsoft.AspNetCore.SignalR;
using EIUBetApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EIUBetApp.Data
{
    public class EIUBetAppHub : Hub
    {
        private readonly EIUBetAppContext _context;

        // Track connected users by username
        private static readonly Dictionary<string, string> _userConnections = new();

        public EIUBetAppHub(EIUBetAppContext context)
        {
            _context = context;
        }

        public override Task OnConnectedAsync()
        {
            var username = Context.GetHttpContext()?.Request.Query["username"].ToString();
            if (!string.IsNullOrEmpty(username))
            {
                _userConnections[username] = Context.ConnectionId;
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userToRemove = _userConnections.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId).Key;
            if (userToRemove != null)
            {
                _userConnections.Remove(userToRemove);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task JoinRoom(string roomId, string playerId)
        {
            var connectionId = Context.ConnectionId;
            var roomGuid = Guid.Parse(roomId);
            var playerGuid = Guid.Parse(playerId);

            var existing = await _context.ManageRoom
                .FirstOrDefaultAsync(r => r.RoomId == roomGuid && r.PlayerId == playerGuid && r.LeaveAt == null);

            if (existing == null)
            {
                var join = new ManageRoom
                {
                    PlayerId = playerGuid,
                    RoomId = roomGuid,
                    JoinAt = DateTime.UtcNow,
                    LeaveAt = null
                };

                _context.ManageRoom.Add(join);
                await _context.SaveChangesAsync();
            }

            await Groups.AddToGroupAsync(connectionId, roomId);

            var players = await _context.ManageRoom
                .Where(r => r.RoomId == roomGuid && r.LeaveAt == null)
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
            var roomGuid = Guid.Parse(roomId);
            var playerGuid = Guid.Parse(playerId);

            var manageRoomEntry = await _context.ManageRoom
                .FirstOrDefaultAsync(r => r.RoomId == roomGuid && r.PlayerId == playerGuid && r.LeaveAt == null);

            if (manageRoomEntry != null)
            {
                manageRoomEntry.LeaveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            await Groups.RemoveFromGroupAsync(connectionId, roomId);

            var players = await _context.ManageRoom
                .Where(r => r.RoomId == roomGuid && r.LeaveAt == null)
                .Include(r => r.Player)
                .Select(r => new
                {
                    PlayerId = r.PlayerId,
                    Balance = r.Player.Balance
                })
                .ToListAsync();

            await Clients.Group(roomId).SendAsync("UpdatePlayerList", players);
        }

        public async Task SendInvite(string fromUsername, string toUsername)
        {
            if (_userConnections.TryGetValue(toUsername, out var toConnectionId))
            {
                await Clients.Client(toConnectionId).SendAsync("ReceiveInvite", fromUsername);
            }
        }
    }
}
