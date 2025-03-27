using EventHub.Models;
using Microsoft.Extensions.Logging;

namespace EventHub.DataAccess.Repository
{
    public interface IEventRepository
    {
        Task<Event?> GetByIdAsync(int eventId);
        IEnumerable<Event> GetAll();
        Task<IEnumerable<Event>> GetAllAsync();
        Task<IEnumerable<Event>> GetAllByTypeAsync(string? typeName);
        Task<int> UpdateTicketTypeAsync(Event updateEvent); // Use on test.
        Task<int> UpdateVisitorCountAsync(int eventId);
        IEnumerable<Event> SearchEventByType(string searchQuery, string? typeName, List<Event>? tempEvents);
        IEnumerable<Event> GetAllByType(string? typeName);
        Task<IEnumerable<Event>> GetUpcomingEventsAsync(int count = 3);
        Task<IEnumerable<Feedback>> GetEventFeedbacksAsync(int eventId);
        Task<int> AddFeedbackAsync(Feedback feedback);
    }
}
