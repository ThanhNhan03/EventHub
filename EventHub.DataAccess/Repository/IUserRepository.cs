using EventHub.Models;
using Microsoft.AspNetCore.Identity;

namespace EventHub.DataAccess.Repository
{
    public interface IUserRepository
    {
        public Task<User?> GetByIdAsync(string userId);
        public Task<User?> GetByNameAsync(string name);
        public Task<IEnumerable<User>> GetAllAsync();
        public Task<int> UpdateUserAsync(User user);
        public Task<Ticket?> GetTicketByIdAsync(string userId, int ticketId);
        public Task<IEnumerable<Ticket>> GetAllTicketAsync(string userId);
        public Task<Event?> GetEventByTicketAsync(string userId, int ticketId);
        public Task<IEnumerable<Event>> GetAllEventAsync(string userId, int ticketId, Period period = Period.Month);
        public Task<IdentityResult> ChangeDisplayNameAsync(string userId, string newDisplayName);
        public Task<IdentityResult> ChangeEmailAsync(string userId, string newEmail);
        public Task<IdentityResult> ChangePasswordAsync(string userId, string newPassword);
        public Task<IEnumerable<Event>> GetRegisteredEventsAsync(string userId);
        public Task<IdentityResult> UnregisterEventAsync(string userId, int eventId);
    }
}
