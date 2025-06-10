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
        private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> _disconnectTokens = new();

        public EIUBetAppHub(EIUBetAppContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var playerIdStr = httpContext?.Request.Query["playerId"];

            if (Guid.TryParse(playerIdStr, out Guid playerId))
            {
                _playerConnections[playerId] = Context.ConnectionId;

                // Cancel any pending offline task
                if (_disconnectTokens.TryRemove(playerId, out var cts))
                    cts.Cancel();

                var player = await _context.Player.FindAsync(playerId);
                if (player != null)
                {
                    player.OnlineStatus = true;
                    await _context.SaveChangesAsync();

                    await Clients.All.SendAsync("PlayerOnlineStatusChanged", playerId, true);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            Guid? playerId = null;

            // Try to get playerId from _connections (players in rooms)
            if (_connections.TryGetValue(connectionId, out var roomPlayer))
                playerId = roomPlayer.playerId;
            else
            {
                // If not in _connections, try from _playerConnections (players not yet in rooms)
                var playerEntry = _playerConnections.FirstOrDefault(p => p.Value == connectionId);
                if (playerEntry.Key != Guid.Empty)
                    playerId = playerEntry.Key;
            }

            if (playerId != null)
            {
                var cts = new CancellationTokenSource();
                _disconnectTokens[playerId.Value] = cts;

                try
                {
                    await Task.Delay(5000, cts.Token); // Wait 5 seconds to allow reconnect
                }
                catch (TaskCanceledException)
                {
                    // Reconnected, so do nothing and exit
                    return;
                }

                _disconnectTokens.TryRemove(playerId.Value, out _);

                // Check if the player reconnected with a different connection ID
                if (_playerConnections.TryGetValue(playerId.Value, out var currentConnectionId))
                {
                    if (currentConnectionId != connectionId)
                    {
                        // The player reconnected with a new connection ID,
                        // so don't mark offline for this old connection
                        return;
                    }
                }

                // Remove the disconnected connectionId from _connections (if exists)
                _connections.TryRemove(connectionId, out var removedRoomPlayer);

                // Remove player from _playerConnections only if this is the latest connection
                _playerConnections.TryRemove(playerId.Value, out _);

                // Update database player online status
                var player = await _context.Player.FindAsync(playerId.Value);
                if (player != null)
                {
                    player.OnlineStatus = false;
                    player.ReadyStatus = false;
                }

                if (removedRoomPlayer != default)
                {
                    // Player was in a room
                    var (roomId, _) = removedRoomPlayer;

                    var manageRoomEntry = await _context.ManageRoom
                        .FirstOrDefaultAsync(r => r.RoomId == roomId && r.PlayerId == playerId && r.LeaveAt == null);

                    if (manageRoomEntry != null)
                        manageRoomEntry.LeaveAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    await Groups.RemoveFromGroupAsync(connectionId, roomId.ToString());
                    await SendPlayerListUpdate(roomId, roomId.ToString());
                }
                else
                {
                    // Player was not in any room
                    await _context.SaveChangesAsync();
                }

                await Clients.All.SendAsync("PlayerOnlineStatusChanged", playerId.Value, false);
            }

            await base.OnDisconnectedAsync(exception);
        }

        //public async Task JoinRoom(string roomId, string playerId)
        //{
        //    var connectionId = Context.ConnectionId;
        //    var parsedRoomId = Guid.Parse(roomId);
        //    var parsedPlayerId = Guid.Parse(playerId);

        //    var maxCapacity = await _context.Room
        //        .Where(r => r.RoomId == parsedRoomId)
        //        .Select(r => r.Capacity)
        //        .FirstOrDefaultAsync();

        //    var currentCount = await _context.ManageRoom
        //        .CountAsync(mr => mr.RoomId == parsedRoomId && mr.LeaveAt == null);

        //    if (currentCount >= (maxCapacity -1))
        //        throw new HubException("Room is full. Please try another room.");

        //    _connections[connectionId] = (parsedRoomId, parsedPlayerId);
        //    _playerConnections[parsedPlayerId] = connectionId;

        //    var existingEntry = await _context.ManageRoom
        //        .Where(r => r.RoomId == parsedRoomId && r.PlayerId == parsedPlayerId)
        //        .OrderByDescending(r => r.JoinAt)
        //        .FirstOrDefaultAsync();

        //    if (existingEntry == null)
        //    {
        //        _context.ManageRoom.Add(new ManageRoom
        //        {
        //            PlayerId = parsedPlayerId,
        //            RoomId = parsedRoomId,
        //            JoinAt = DateTime.UtcNow
        //        });
        //    }
        //    else if (existingEntry.LeaveAt != null)
        //    {
        //        existingEntry.JoinAt = DateTime.UtcNow;
        //        existingEntry.LeaveAt = null;
        //        _context.ManageRoom.Update(existingEntry);
        //    }

        //    await _context.SaveChangesAsync();
        //    await Groups.AddToGroupAsync(connectionId, roomId);
        //    await SendPlayerListUpdate(parsedRoomId, roomId);

        //    // 🟡 Return room data to caller
        //    var room = await _context.Room
        //        .Where(r => r.RoomId == parsedRoomId)
        //        .Select(r => new
        //        {
        //            RoomId = r.RoomId,
        //            GameId = r.GameId,
        //            GameName = r.Game.Name
        //        })
        //        .FirstOrDefaultAsync();

        //    await Clients.Caller.SendAsync("ReceiveRoomInfo", room);
        //}

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

            if (currentCount >= (maxCapacity - 1))
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

            // 🟡 Return room data to caller
            var room = await _context.Room
                .Where(r => r.RoomId == parsedRoomId)
                .Select(r => new
                {
                    RoomId = r.RoomId,
                    GameId = r.GameId,
                    GameName = r.Game.Name
                })
                .FirstOrDefaultAsync();

            await Clients.Caller.SendAsync("ReceiveRoomInfo", room);
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
            var fromUser = await _context.User.FirstOrDefaultAsync(u => u.Username == fromUsername);
            var toUser = await _context.User.FirstOrDefaultAsync(u => u.Username == toUsername);

            if (fromUser == null || toUser == null)
            {
                await Clients.Caller.SendAsync("InviteFailed", toUsername, "User not found.");
                return;
            }

            var fromPlayer = await _context.Player.FirstOrDefaultAsync(p => p.UserId == fromUser.UserId);
            var toPlayer = await _context.Player.FirstOrDefaultAsync(p => p.UserId == toUser.UserId);

            if (fromPlayer == null || toPlayer == null)
            {
                await Clients.Caller.SendAsync("InviteFailed", toUsername, "Player not found.");
                return;
            }

            if (!_playerConnections.TryGetValue(toPlayer.PlayerId, out var targetConnection) ||
                !_playerConnections.TryGetValue(fromPlayer.PlayerId, out var senderConnection))
            {
                await Clients.Caller.SendAsync("InviteFailed", toUsername, "One of the players is not online.");
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

            // Add fromPlayer to room if not already in
            var fromEntry = await _context.ManageRoom
                .FirstOrDefaultAsync(mr => mr.PlayerId == fromPlayer.PlayerId && mr.RoomId == availableRoom.RoomId && mr.LeaveAt == null);

            if (fromEntry == null)
            {
                _context.ManageRoom.Add(new ManageRoom
                {
                    PlayerId = fromPlayer.PlayerId,
                    RoomId = availableRoom.RoomId,
                    JoinAt = DateTime.UtcNow,
                    LeaveAt = null,
                });
            }

            await _context.SaveChangesAsync();

            // Update tracking for inviter only
            _connections[senderConnection] = (availableRoom.RoomId, fromPlayer.PlayerId);
            _playerConnections[fromPlayer.PlayerId] = senderConnection;

            // Add inviter to SignalR group
            await Groups.AddToGroupAsync(senderConnection, availableRoom.RoomId.ToString());

            // Notify invitee (don't join room yet)
            await Clients.Client(targetConnection).SendAsync("ReceiveInvite", fromUsername, availableRoom.RoomId.ToString());

            // Notify inviter
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

        // tao phong moi
        public static class HubExtensions
        {
            public static async Task NotifyNewRoomCreated(IHubContext<EIUBetAppHub> hubContext, Room room, Game game)
            {
                await hubContext.Clients.All.SendAsync("NewRoomAdded", new
                {
                    roomId = room.RoomId,
                    roomName = room.RoomName,
                    capacity = room.Capacity,
                    gameName = game?.Name ?? "Unknown"
                });
            }

            //public static async Task NotifyRoomDeleted(IHubContext<EIUBetAppHub> hubContext, Guid roomId)
            //{
            //    await hubContext.Clients.All.SendAsync("RoomDeleted", roomId);
            //}

            public static async Task NotifyRoomVisibilityChanged(IHubContext<EIUBetAppHub> hubContext, Guid roomId, bool isDeleted, Room? room = null, Game? game = null)
            {
                if (isDeleted)
                {
                    // Chỉ cần gửi ID để ẩn phòng
                    await hubContext.Clients.All.SendAsync("RoomVisibilityChanged", roomId, true);
                }
                else
                {
                    // Gửi lại đầy đủ dữ liệu để hiện phòng lại
                    await hubContext.Clients.All.SendAsync("RoomVisibilityChanged", roomId, false, new
                    {
                        roomId = room?.RoomId,
                        roomName = room?.RoomName,
                        capacity = room?.Capacity,
                        gameName = game?.Name ?? "Unknown"
                    });
                }
            }

        }


    }
}
