using Microsoft.AspNetCore.SignalR;

namespace EventHub.Hubs
{
    public class ActiveUsersHub : Hub
    {
        private static int _activeUsers = 0;

        public override async Task OnConnectedAsync()
        {
            _activeUsers++;
            await Clients.All.SendAsync("UpdateActiveUsers", _activeUsers);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _activeUsers--;
            await Clients.All.SendAsync("UpdateActiveUsers", _activeUsers);
            await base.OnDisconnectedAsync(exception);
        }
    }
}