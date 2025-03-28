﻿using EventHub.DataAccess.Data;
using EventHub.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EventHub.DataAccess.Repository
{
    public class EventRepository : IEventRepository
    {
        private readonly EventManagementSystemDbContext _eventManagementSystemDbContext;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public EventRepository(EventManagementSystemDbContext eventManagementSystemDbContext)
        {
            _eventManagementSystemDbContext = eventManagementSystemDbContext;
        }        

        public async Task<Event?> GetByIdAsync(int eventId)
        {
            return await _eventManagementSystemDbContext.Events
                .AsNoTracking()
                .Include(e => e.TicketTypes)
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == eventId);
        }

        public IEnumerable<Event> GetAll()
        {
            return _eventManagementSystemDbContext.Events
                .AsNoTracking()
                .Include(e => e.TicketTypes)
                .Include(e => e.User)
                .OrderBy(e => e.Id)
                .ToList();
        }

        public async Task<IEnumerable<Event>> GetAllAsync()
        {
            return await _eventManagementSystemDbContext.Events
                .AsNoTracking()
                .OrderBy(e => e.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetAllByTypeAsync(string? typeName)
        {
            var toDay = DateOnly.FromDateTime(DateTime.Now);
            if (Enum.TryParse(typeof(Category), typeName, true, out var existingCategory))
            {
                return await _eventManagementSystemDbContext.Events
                    .AsNoTracking()
                    .Where(e => e.Category.Equals((Category)existingCategory))
                    .Where(e => e.StartDate >= toDay)
                    .ToListAsync();
            }
            return await GetAllAsync();
        }        

        // Use on test.
        public async Task<int> UpdateTicketTypeAsync(Event updateEvent)
        {
            bool ticketTypeWithSameName = _eventManagementSystemDbContext.TicketTypes
                .AsEnumerable()
                .Any(tt => updateEvent.TicketTypes
                .Any(ett => ett.Name == tt.Name && ett.Id != tt.Id && ett.EventId == tt.EventId));

            if (ticketTypeWithSameName)
                throw new Exception("A Tickket type with the same name already exists");

            var eventToUpdate = await _eventManagementSystemDbContext.Events.FindAsync(updateEvent.Id);
            if (eventToUpdate is not null)
            {
                eventToUpdate.TicketTypes = updateEvent.TicketTypes;
                _eventManagementSystemDbContext.Events.Update(eventToUpdate);
                return await _eventManagementSystemDbContext.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("The event to update can't be find");
            }
        }

        public async Task<int> UpdateVisitorCountAsync(int eventId)
        {
            var eventToUpdate = await _eventManagementSystemDbContext.Events.FindAsync(eventId);
            if (eventToUpdate is not null)
            {
                eventToUpdate.PageVisitorCount++;
                _eventManagementSystemDbContext.Update(eventToUpdate);
                return await _eventManagementSystemDbContext.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("The event to update can't be find");
            }
        }

        public IEnumerable<Event> SearchEventByType(string searchQuery, string? typeName, List<Event>? tempEvents)
        {
            if (tempEvents is not null && tempEvents.Count != 0)
            {
                return tempEvents
                    .Where(e => e.Title.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase) ||
                    e.ShortDescription.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase) ||
                    e.Description.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase) ||
                    e.Country.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase) ||
                    e.Address.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase));
            }
            else if (Enum.TryParse(typeof(Category), typeName, true, out var existingCategory))
            {
                return _eventManagementSystemDbContext.Events
                .AsNoTracking()
                .Where(e => e.Category.Equals((Category)existingCategory))
                .Where(e => e.Title.Contains(searchQuery) ||
                    e.ShortDescription.Contains(searchQuery) ||
                    e.Description.Contains(searchQuery) ||
                    e.Country.Contains(searchQuery) ||
                    e.Address.Contains(searchQuery));
            }
            else
            {
                return _eventManagementSystemDbContext.Events
                .AsNoTracking()
                .Where(e => e.Title.Contains(searchQuery) ||
                    e.ShortDescription.Contains(searchQuery) ||
                    e.Description.Contains(searchQuery) ||
                    e.Country.Contains(searchQuery) ||
                    e.Address.Contains(searchQuery));
            }
        }

        public IEnumerable<Event> GetAllByType(string? typeName)
        {
            if (Enum.TryParse(typeof(Category), typeName, true, out var existingCategory))
            {
                return _eventManagementSystemDbContext.Events
                .AsNoTracking()
                .Where(e => e.Category.Equals((Category)existingCategory));
            }
            else
            {
                return _eventManagementSystemDbContext.Events
                .AsNoTracking();
            }
        }

        public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(int count = 3)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return await _eventManagementSystemDbContext.Events
                .AsNoTracking()
                .Include(e => e.TicketTypes)
                .Where(e => e.StartDate >= today)
                .OrderBy(e => e.StartDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> AddFeedbackAsync(Feedback feedback)
        {
            await _eventManagementSystemDbContext.Feedbacks.AddAsync(feedback);
            return await _eventManagementSystemDbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<Feedback>> GetEventFeedbacksAsync(int eventId)
        {
            return await _eventManagementSystemDbContext.Feedbacks
                .AsNoTracking()
                .Include(f => f.User)
                .Where(f => f.EventId == eventId)
                .OrderByDescending(f => f.Id)
                .ToListAsync();
        }
    }
}
