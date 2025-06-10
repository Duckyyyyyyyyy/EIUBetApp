using Microsoft.AspNetCore.SignalR;
using EIUBetApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EIUBetApp.Data
{
    public class EIUBetAppHub : Hub
    {
        private readonly EIUBetAppContext _context;

        private static readonly ConcurrentDictionary<string, (Guid roomId, Guid playerId)> _connections = new();
        private static readonly ConcurrentDictionary<Guid, string> _playerConnections = new();

        public EIUBetAppHub(EIUBetAppContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            // You may log or track connection here if needed
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            if (_connections.TryRemove(connectionId, out var roomPlayer))
            {
                var (roomId, playerId) = roomPlayer;
                _playerConnections.TryRemove(playerId, out _);

                var manageRoomEntry = await _context.ManageRoom
                    .FirstOrDefaultAsync(r => r.RoomId == roomId && r.PlayerId == playerId && r.LeaveAt == null);

                if (manageRoomEntry != null)
                {
                    manageRoomEntry.LeaveAt = DateTime.UtcNow;

                    var player = await _context.Player.FindAsync(playerId);
                    if (player != null)
                        player.ReadyStatus = false;

                    await _context.SaveChangesAsync();
                }

                await Groups.RemoveFromGroupAsync(connectionId, roomId.ToString());
                await SendPlayerListUpdate(roomId, roomId.ToString());
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinRoom(string roomId, string playerId)
        {
            var connectionId = Context.ConnectionId;
            var parsedRoomId = Guid.Parse(roomId);
            var parsedPlayerId = Guid.Parse(playerId);

            var maxCapacity = await _context.Room
                .Where(r => r.RoomId == parsedRoomId)
                .Select(r => r.Capacity)
                .FirstOrDefaultAsync();

            var currentCount = await _context.ManageRoom
                .CountAsync(mr => mr.RoomId == parsedRoomId && mr.LeaveAt == null);

            if (currentCount >= maxCapacity)
                throw new HubException("Room is full. Please try another room.");

            _connections[connectionId] = (parsedRoomId, parsedPlayerId);
            _playerConnections[parsedPlayerId] = connectionId;

            var existingEntry = await _context.ManageRoom
                .Where(r => r.RoomId == parsedRoomId && r.PlayerId == parsedPlayerId)
                .OrderByDescending(r => r.JoinAt)
                .FirstOrDefaultAsync();

            if (existingEntry == null)
            {
                _context.ManageRoom.Add(new ManageRoom
                {
                    PlayerId = parsedPlayerId,
                    RoomId = parsedRoomId,
                    JoinAt = DateTime.UtcNow
                });
            }
            else if (existingEntry.LeaveAt != null)
            {
                existingEntry.JoinAt = DateTime.UtcNow;
                existingEntry.LeaveAt = null;
                _context.ManageRoom.Update(existingEntry);
            }

            await _context.SaveChangesAsync();
            await Groups.AddToGroupAsync(connectionId, roomId);
            await SendPlayerListUpdate(parsedRoomId, roomId);
        }

        private async Task SendPlayerListUpdate(Guid roomId, string roomIdStr)
        {
            var players = await _context.ManageRoom
                .Where(r => r.RoomId == roomId && r.LeaveAt == null)
                .Include(r => r.Player)
                    .ThenInclude(p => p.User)
                .Select(r => new
                {
                    PlayerId = r.PlayerId,
                    Balance = r.Player.Balance,
                    Username = r.Player.User.Username,
                    ReadyStatus = r.Player.ReadyStatus
                })
                .ToListAsync();

            await Clients.Group(roomIdStr).SendAsync("UpdatePlayerList", players);
        }
        public async Task LeaveRoom(string roomId, string playerId)
        {
            var connectionId = Context.ConnectionId;
            var parsedRoomId = Guid.Parse(roomId);
            var parsedPlayerId = Guid.Parse(playerId);

            _connections.TryRemove(connectionId, out _);
            _playerConnections.TryRemove(parsedPlayerId, out _);

            var manageRoomEntry = await _context.ManageRoom
                .FirstOrDefaultAsync(r => r.RoomId == parsedRoomId && r.PlayerId == parsedPlayerId && r.LeaveAt == null);

            if (manageRoomEntry != null)
            {
                manageRoomEntry.LeaveAt = DateTime.UtcNow;

                var player = await _context.Player.FindAsync(parsedPlayerId);
                if (player != null)
                    player.ReadyStatus = false;

                await _context.SaveChangesAsync();
            }

            await Groups.RemoveFromGroupAsync(connectionId, roomId);
            await SendPlayerListUpdate(parsedRoomId, roomId);
        }

        public async Task SetReadyStatus(string roomId, string playerId, bool isReady)
        {
            var parsedRoomId = Guid.Parse(roomId);
            var parsedPlayerId = Guid.Parse(playerId);

            var player = await _context.Player.FindAsync(parsedPlayerId);
            if (player != null)
            {
                player.ReadyStatus = isReady;
                await _context.SaveChangesAsync();
                await SendPlayerListUpdate(parsedRoomId, roomId);
            }
        }

        public async Task SendInvite(string fromUsername, string toUsername)
        {
            var toUser = await _context.User.FirstOrDefaultAsync(u => u.Username == toUsername);
            if (toUser == null)
            {
                await Clients.Caller.SendAsync("InviteFailed", toUsername, "User not found.");
                return;
            }

            var toPlayer = await _context.Player.FirstOrDefaultAsync(p => p.UserId == toUser.UserId);
            if (toPlayer == null)
            {
                await Clients.Caller.SendAsync("InviteFailed", toUsername, "Player not found.");
                return;
            }

            if (!_playerConnections.TryGetValue(toPlayer.PlayerId, out var targetConnection))
            {
                await Clients.Caller.SendAsync("InviteFailed", toUsername, "Player is not online.");
                return;
            }

            var availableRoom = await _context.Room
                .Where(r => _context.ManageRoom.Count(mr => mr.RoomId == r.RoomId && mr.LeaveAt == null) < r.Capacity)
                .OrderBy(r => r.RoomName)
                .FirstOrDefaultAsync();

            if (availableRoom == null)
            {
                throw new HubException("No available rooms to invite player.");
            }

            var existingEntry = await _context.ManageRoom
                .FirstOrDefaultAsync(mr => mr.PlayerId == toPlayer.PlayerId && mr.RoomId == availableRoom.RoomId && mr.LeaveAt == null);

            if (existingEntry == null)
            {
                _context.ManageRoom.Add(new ManageRoom
                {
                    PlayerId = toPlayer.PlayerId,
                    RoomId = availableRoom.RoomId,
                    JoinAt = DateTime.UtcNow
                });
            }
            else
            {
                existingEntry.JoinAt = DateTime.UtcNow;
                existingEntry.LeaveAt = null;
                _context.ManageRoom.Update(existingEntry);
            }

            await _context.SaveChangesAsync();

            _connections[targetConnection] = (availableRoom.RoomId, toPlayer.PlayerId);
            _playerConnections[toPlayer.PlayerId] = targetConnection;

            await Groups.AddToGroupAsync(targetConnection, availableRoom.RoomId.ToString());
            await Clients.Client(targetConnection).SendAsync("ReceiveInvite", fromUsername, availableRoom.RoomId.ToString());

            await Clients.Caller.SendAsync("InviteSentSuccess", toUsername, availableRoom.RoomId.ToString());
        }

        //update online status
        public async Task UpdateOnlineStatus(string playerIdStr, bool isOnline)
        {
            if (!Guid.TryParse(playerIdStr, out Guid playerId))
                throw new HubException("Invalid player ID.");

            var player = await _context.Player.FindAsync(playerId);
            if (player == null)
                throw new HubException("Player not found.");

            player.OnlineStatus = isOnline;
            await _context.SaveChangesAsync();

            // Notify all clients about this status change
            await Clients.All.SendAsync("PlayerOnlineStatusChanged", playerId, isOnline);
        }

        public async Task SendMessageToRoom(string roomId, string username, string message)
        {
            // gui tin nhan den mn trong room
            await Clients.Group(roomId).SendAsync("ReceiveMessage", username, message, DateTime.UtcNow.ToString("HH:mm"));
        }

    }
}
