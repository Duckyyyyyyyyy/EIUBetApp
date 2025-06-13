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
        // Dictionary<RoomId, Dictionary<PlayerId, (prediction, betAmount)>>
        private static readonly Dictionary<string, Dictionary<string, (int prediction, int betAmount)>> RoomPredictions
            = new Dictionary<string, Dictionary<string, (int, int)>>();

        private static readonly object _lock = new object();

        public EIUBetAppHub(EIUBetAppContext context)
        {
            _context = context;
        }
        // When a client connects to the hub
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var playerIdStr = httpContext?.Request.Query["playerId"];

            if (Guid.TryParse(playerIdStr, out Guid playerId))
            {
                _playerConnections[playerId] = Context.ConnectionId;

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

        // When a client disconnects from the hub
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            Guid? playerId = null;

            if (_connections.TryGetValue(connectionId, out var roomPlayer))
                playerId = roomPlayer.playerId;
            else
            {
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
                    await Task.Delay(2000, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                _disconnectTokens.TryRemove(playerId.Value, out _);

                if (_playerConnections.TryGetValue(playerId.Value, out var currentConnectionId) && currentConnectionId != connectionId)
                    return;

                _connections.TryRemove(connectionId, out var removedRoomPlayer);
                _playerConnections.TryRemove(playerId.Value, out _);

                var player = await _context.Player.FindAsync(playerId.Value);
                if (player != null)
                {
                    player.OnlineStatus = false;
                    player.ReadyStatus = false;
                }

                if (removedRoomPlayer != default)
                {
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
                    await _context.SaveChangesAsync();
                }

                await Clients.All.SendAsync("PlayerOnlineStatusChanged", playerId.Value, false);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Player joins a room
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

        // Send updated player list in the room
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

        // Player leaves a room
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
        public async Task SubmitPredictionAndBet(string roomId, string playerId, int prediction, int betAmount)
        {
            lock (_lock)
            {
                if (!RoomPredictions.ContainsKey(roomId))
                    RoomPredictions[roomId] = new Dictionary<string, (int, int)>();

                RoomPredictions[roomId][playerId] = (prediction, betAmount);
            }

            await Clients.Caller.SendAsync("PredictionSubmitted");

            // Check if all players in room have submitted their predictions
            var totalPlayersInRoom = await _context.ManageRoom
                .Where(mr => mr.RoomId == Guid.Parse(roomId) && mr.LeaveAt == null)
                .CountAsync();

            bool allSubmitted = false;
            lock (_lock)
            {
                allSubmitted = RoomPredictions[roomId].Count == totalPlayersInRoom;
            }

            if (allSubmitted)
            {
                // Start the spin for the room
                await SpinDice(roomId);
            }
        }


        public async Task SpinDice(string roomId)
        {
            Dictionary<string, (int prediction, int betAmount)> playersInRoom = null;

            // Lock access to copy data safely
            lock (_lock)
            {
                if (!RoomPredictions.TryGetValue(roomId, out var roomData) || roomData.Count == 0)
                {
                    playersInRoom = null;
                }
                else
                {
                    // Copy and clear for next round
                    playersInRoom = new Dictionary<string, (int, int)>(roomData);
                    RoomPredictions[roomId].Clear();
                }
            }

            if (playersInRoom == null || playersInRoom.Count == 0)
            {
                await Clients.Group(roomId).SendAsync("SpinResult", new { error = "No players have submitted predictions or bets." });
                return;
            }

            // Spin dice once
            Random rand = new Random();
            int[] diceResults = new int[3];
            for (int i = 0; i < 3; i++)
                diceResults[i] = rand.Next(1, 7);

            await Clients.Group(roomId).SendAsync("StartSpin", diceResults);
            await Task.Delay(2000); // simulate spinning animation

            foreach (var kvp in playersInRoom)
            {
                string playerId = kvp.Key;
                var (prediction, betAmount) = kvp.Value;

                int matchCount = diceResults.Count(val => val == prediction);
                int winnings = matchCount * betAmount;

                var parsedPlayerId = Guid.Parse(playerId);
                var player = await _context.Player.FindAsync(parsedPlayerId);
                if (player == null) continue;

                player.Balance -= betAmount;
                if (winnings > 0)
                    player.Balance += (winnings + betAmount);

                // Log the game result
                var log = new Logs
                {
                    LogId = Guid.NewGuid(),
                    PlayerId = parsedPlayerId,
                    Action = prediction.ToString(),
                    GameResult = $"{string.Join(",", diceResults)}; Matches: {matchCount}; Winnings: {winnings}",
                    TimeAt = DateTime.UtcNow,
                    RoomId = Guid.Parse(roomId),
                    IsDelete = false,
                    GameId = Guid.Parse("868773BA-CDEF-40CA-8F39-81AF12910B70")
                };

                _context.Logs.Add(log);

                var result = new
                {
                    diceResults,
                    matchCount,
                    winnings,
                    playerId,
                    message = matchCount == 0 ? "Không trúng." : $"Trúng {matchCount} lần! Thắng: ${winnings}"
                };

                // Send result to the specific player
                await Clients.User(playerId).SendAsync("SpinResult", result); // Requires mapping userId to connectionId via OnConnectedAsync

                var updatedBalance = new
                {
                    playerId,
                    newBalance = player.Balance
                };

                await Clients.Group(roomId).SendAsync("UpdatePlayerBalance", updatedBalance);
            }

            await _context.SaveChangesAsync();
            await SendPlayerListUpdate(Guid.Parse(roomId), roomId);
        }


       
        //Reset all players' ready status in a room
        //public async Task ResetAllPlayersReadyStatus(string roomId)
        //{
        //    var parsedRoomId = Guid.Parse(roomId);

        //    var playersInRoom = await _context.ManageRoom
        //        .Where(mr => mr.RoomId == parsedRoomId && mr.LeaveAt == null)
        //        .Include(mr => mr.Player)
        //        .Select(mr => mr.Player)
        //        .ToListAsync();

        //    foreach (var player in playersInRoom)
        //        player.ReadyStatus = false;

        //    await _context.SaveChangesAsync();

        //    await SendPlayerListUpdate(parsedRoomId, roomId);
        //}



        // Set player's ready status
        //public async Task SetReadyStatus(string roomId, string playerId, bool isReady)
        //{
        //    var parsedRoomId = Guid.Parse(roomId);
        //    var parsedPlayerId = Guid.Parse(playerId);

        //    var player = await _context.Player.FindAsync(parsedPlayerId);
        //    if (player != null)
        //    {
        //        player.ReadyStatus = isReady;
        //        await _context.SaveChangesAsync();
        //        await SendPlayerListUpdate(parsedRoomId, roomId);
        //    }
        //}

        // Send invite from one user to another
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
                throw new HubException("No available rooms to invite player.");

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

            _connections[senderConnection] = (availableRoom.RoomId, fromPlayer.PlayerId);
            _playerConnections[fromPlayer.PlayerId] = senderConnection;

            await Groups.AddToGroupAsync(senderConnection, availableRoom.RoomId.ToString());

            await Clients.Client(targetConnection).SendAsync("ReceiveInvite", fromUsername, availableRoom.RoomId.ToString());

            await Clients.Caller.SendAsync("InviteSentSuccess", toUsername, availableRoom.RoomId.ToString());
        }

        // Update player's online status manually
        public async Task UpdateOnlineStatus(string playerIdStr, bool isOnline)
        {
            if (!Guid.TryParse(playerIdStr, out Guid playerId))
                throw new HubException("Invalid player ID.");

            var player = await _context.Player.FindAsync(playerId);
            if (player == null)
                throw new HubException("Player not found.");

            player.OnlineStatus = isOnline;
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("PlayerOnlineStatusChanged", playerId, isOnline);
        }

        // Send chat message to all in a room
        public async Task SendMessageToRoom(string roomId, string username, string message)
        {
            await Clients.Group(roomId).SendAsync("ReceiveMessage", username, message, DateTime.UtcNow.ToString("HH:mm"));
        }

        // Static helper methods for notifying clients about room changes
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

            public static async Task NotifyRoomVisibilityChanged(IHubContext<EIUBetAppHub> hubContext, Guid roomId, bool isDeleted, Room? room = null, Game? game = null)
            {
                if (isDeleted)
                {
                    await hubContext.Clients.All.SendAsync("RoomVisibilityChanged", roomId, true);
                }
                else
                {
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
