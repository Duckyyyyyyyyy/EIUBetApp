using Microsoft.AspNetCore.SignalR;
using EIUBetApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EIUBetApp.Data
{
    public class EIUBetAppHub : Hub
    {
        private readonly EIUBetAppContext _context;

        // Map connectionId -> (roomId, playerId)
        private static ConcurrentDictionary<string, (Guid roomId, Guid playerId)> _connections = new();

        public EIUBetAppHub(EIUBetAppContext context)
        {
            _context = context;
        }

        public async Task JoinRoom(string roomId, string playerId)
        {
            var connectionId = Context.ConnectionId;
            var parsedRoomId = Guid.Parse(roomId);
            var parsedPlayerId = Guid.Parse(playerId);

            // Save mapping
            _connections[connectionId] = (parsedRoomId, parsedPlayerId);

            // Find existing ManageRoom row for this player in this room, even if LeaveAt is not null
            var existingEntry = await _context.ManageRoom
                .Where(r => r.RoomId == parsedRoomId && r.PlayerId == parsedPlayerId)
                .OrderByDescending(r => r.JoinAt) // latest join record
                .FirstOrDefaultAsync();

            if (existingEntry == null)
            {
                // No record, create new
                var join = new ManageRoom
                {
                    PlayerId = parsedPlayerId,
                    RoomId = parsedRoomId,
                    JoinAt = DateTime.UtcNow,
                    LeaveAt = null
                };

                _context.ManageRoom.Add(join);
            }
            else if (existingEntry.LeaveAt != null)
            {
                // Player had left before, "reactivate" this entry by clearing LeaveAt and updating JoinAt
                existingEntry.JoinAt = DateTime.UtcNow;
                existingEntry.LeaveAt = null;
                _context.ManageRoom.Update(existingEntry);
            }
            // else: existing entry is active (LeaveAt == null), do nothing (already joined)

            await _context.SaveChangesAsync();

            await Groups.AddToGroupAsync(connectionId, roomId);

            await SendPlayerListUpdate(parsedRoomId, roomId);
        }


        public async Task LeaveRoom(string roomId, string playerId)
        {
            var connectionId = Context.ConnectionId;
            var parsedRoomId = Guid.Parse(roomId);
            var parsedPlayerId = Guid.Parse(playerId);

            // Remove mapping for this connection
            _connections.TryRemove(connectionId, out _);

            var manageRoomEntry = await _context.ManageRoom
                .Where(r => r.RoomId == parsedRoomId
                            && r.PlayerId == parsedPlayerId
                            && r.LeaveAt == null)
                .FirstOrDefaultAsync();

            if (manageRoomEntry != null)
            {
                manageRoomEntry.LeaveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            await Groups.RemoveFromGroupAsync(connectionId, roomId);

            await SendPlayerListUpdate(parsedRoomId, roomId);
        }

        // Override disconnect handler
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            if (_connections.TryRemove(connectionId, out var roomPlayer))
            {
                var (roomId, playerId) = roomPlayer;

                // Mark LeaveAt for the player in this room
                var manageRoomEntry = await _context.ManageRoom
                    .Where(r => r.RoomId == roomId
                                && r.PlayerId == playerId
                                && r.LeaveAt == null)
                    .FirstOrDefaultAsync();

                if (manageRoomEntry != null)
                {
                    manageRoomEntry.LeaveAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                await Groups.RemoveFromGroupAsync(connectionId, roomId.ToString());

                await SendPlayerListUpdate(roomId, roomId.ToString());
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task SendPlayerListUpdate(Guid parsedRoomId, string roomId)
        {
            var players = await _context.ManageRoom
                .Where(r => r.RoomId == parsedRoomId && r.LeaveAt == null)
                .Include(r => r.Player)
                .Select(r => new
                {
                    PlayerId = r.PlayerId,
                    Balance = r.Player.Balance
                })
                .ToListAsync();

            await Clients.Group(roomId).SendAsync("UpdatePlayerList", players);
        }
    }
}
