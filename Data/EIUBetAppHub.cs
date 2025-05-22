using Microsoft.AspNetCore.SignalR;

namespace EIUBetApp.Data
{
    public class EIUBetAppHub : Hub
    {
        public async Task JoinRoom(Guid roomId, Guid playerId)
        {
            await Clients.All.SendAsync("PlayerJoinNotifycation");
        }

        //connection.on("PlayerJoinNotifycation", (playerId) =>{
            
        //    });
    }
}
