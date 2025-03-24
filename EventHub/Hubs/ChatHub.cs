using Microsoft.AspNetCore.SignalR;

namespace EventHub.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task SendPrivateMessage(string toUser, string message, string fromUser)
        {
            await Clients.User(toUser).SendAsync("ReceivePrivateMessage", fromUser, message);
        }

        public async Task JoinAdminGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
        }
    }
}