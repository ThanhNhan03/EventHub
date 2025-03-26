using EventHub.Models;
using Microsoft.AspNetCore.SignalR;

namespace EventHub.Hubs
{
    public class EventHubCommunicationHub : Hub
    {
        private static int _activeUsers = 0;
        private static readonly Dictionary<string, string> _userConnections = new();

        public override async Task OnConnectedAsync()
        {
            _activeUsers++;
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                _userConnections[Context.ConnectionId] = username;
            }
            await Clients.All.SendAsync("UpdateActiveUsers", _activeUsers);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _activeUsers--;
            _userConnections.Remove(Context.ConnectionId);
            await Clients.All.SendAsync("UpdateActiveUsers", _activeUsers);
            await base.OnDisconnectedAsync(exception);
        }

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

        public async Task GetActiveUsers()
        {
            await Clients.Caller.SendAsync("UpdateActiveUsers", _activeUsers);
        }

        public async Task GetOnlineUsers()
        {
            var onlineUsers = _userConnections.Values.Distinct().ToList();
            await Clients.Caller.SendAsync("UpdateOnlineUsers", onlineUsers);
        }
    }
}