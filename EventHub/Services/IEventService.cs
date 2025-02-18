using EventHub.Models;

namespace EventHub.Services
{
    public interface IEventService
    {
        IEnumerable<Event> Events { get; }
        void SetEvents();
    }
}
