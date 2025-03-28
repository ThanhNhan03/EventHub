﻿using EventHub.DataAccess.Data;
using EventHub.Models;
using Microsoft.EntityFrameworkCore;

namespace EventHub.DataAccess.Repository.Admin
{
    public class AdminEventRepository : IAdminEventRepository
    {
        private readonly EventManagementSystemDbContext _eventManagementSystemDbContext;

        public AdminEventRepository(EventManagementSystemDbContext eventManagementSystemDbContext)
        {
            _eventManagementSystemDbContext = eventManagementSystemDbContext;
        }

        public async Task<int> CreateAsync(Event newEvent)
        {
            await _eventManagementSystemDbContext.Events.AddAsync(newEvent);
            return await _eventManagementSystemDbContext.SaveChangesAsync();
        }

        public async Task<int> DeleteAsync(int eventId)
        {
            // Load event và ticket types trong một lần query
            var eventToDelete = await _eventManagementSystemDbContext.Events
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventToDelete is not null)
            {
                if (eventToDelete.TicketTypes?.Any() == true)
                {
                    throw new Exception("Ticket type is exist, Delete Ticket type before deleting the Event.");
                }

                _eventManagementSystemDbContext.Events.Remove(eventToDelete);
                return await _eventManagementSystemDbContext.SaveChangesAsync();
            }
            
            throw new ArgumentException("Event id to deleted not found.");
        }

        public async Task<int> EditAsync(Event editEvent)
        {
            var eventToEdit = await _eventManagementSystemDbContext.Events.FindAsync(editEvent.Id);
            if (eventToEdit is not null)
            {
                List<TicketType> ticketTypes = [];
                foreach (var ticketType in editEvent.TicketTypes!)
                {
                    if (ticketType.Id != 0 && ticketType.EventId == eventToEdit.Id)
                    {
                        var ticketTypeToEdit = await _eventManagementSystemDbContext.TicketTypes
                            .FindAsync(ticketType.Id);

                        if (ticketTypeToEdit is not null)
                        {
                            if (ticketTypeToEdit.TotalTicketsSold > ticketType.MaxCapital)
                            {
                                throw new Exception($"Max capital in ticket type must be greater than the {ticketTypeToEdit.TotalTicketsSold:#,##} old  tickets sold.");
                            }

                            ticketTypeToEdit.Name = ticketType.Name;
                            ticketTypeToEdit.Detail = ticketType.Detail;
                            ticketTypeToEdit.Price = ticketType.Price;
                            ticketTypeToEdit.MaxCapital = ticketType.MaxCapital;

                            ticketTypes.Add(ticketTypeToEdit);
                        }
                    }
                    else
                    {
                        ticketTypes.Add(ticketType);
                    }
                }

                eventToEdit.Title = editEvent.Title;
                eventToEdit.ShortDescription = editEvent.ShortDescription;
                eventToEdit.Description = editEvent.Description;
                eventToEdit.StartDate = editEvent.StartDate;
                eventToEdit.EndDate = editEvent.EndDate;
                eventToEdit.VenueName = editEvent.VenueName;
                eventToEdit.Latitude = editEvent.Latitude;
                eventToEdit.Longitude = editEvent.Longitude;
                eventToEdit.Country = editEvent.Country;
                eventToEdit.Address = editEvent.Address;
                eventToEdit.Image = editEvent.Image;
                eventToEdit.Transports = editEvent.Transports;
                eventToEdit.Category = editEvent.Category;
                eventToEdit.TicketTypes = ticketTypes;

                _eventManagementSystemDbContext.Events.Update(eventToEdit);
                return await _eventManagementSystemDbContext.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("Event to edit not found.");
            }
        }

        public async Task<int> GetAllEventsCountAsync()
        {
            return await _eventManagementSystemDbContext.Events.CountAsync();
        }

        public int GetAllEventsSearchCountAsync(string searchQuery)
        {
            var lowerSearchQuery = searchQuery.ToLower();
            return _eventManagementSystemDbContext.Events
                .Where(e => e.Title.ToLower().Contains(lowerSearchQuery) ||
                    e.ShortDescription.ToLower().Contains(lowerSearchQuery) ||
                    e.Description.ToLower().Contains(lowerSearchQuery) ||
                    e.Country.ToLower().Contains(lowerSearchQuery) ||
                    e.Address.ToLower().Contains(lowerSearchQuery) ||
                    ((int)e.Category).ToString() == lowerSearchQuery)
                .Count();
        }

        public async Task<IEnumerable<Event>> GetEventsSortedPagedAsync(string sortBy, int? pageNumber, int maxItem, string? searchQuery)
        {
            IQueryable<Event> events = _eventManagementSystemDbContext.Events;

            if (!string.IsNullOrEmpty(searchQuery))
            {
                var lowerSearchQuery = searchQuery.ToLower();
                events = events.Where(e => e.Title.ToLower().Contains(lowerSearchQuery) ||
                    e.ShortDescription.ToLower().Contains(lowerSearchQuery) ||
                    e.Description.ToLower().Contains(lowerSearchQuery) ||
                    e.Country.ToLower().Contains(lowerSearchQuery) ||
                    e.Address.ToLower().Contains(lowerSearchQuery) ||
                    ((int)e.Category).ToString() == lowerSearchQuery);
            }

            events = sortBy switch
            {
                "name" => events.OrderBy(e => e.Title),
                "name_desc" => events.OrderByDescending(e => e.Title),
                "category" => events.OrderBy(e => e.Category),
                "category_desc" => events.OrderByDescending(e => e.Category),
                "country" => events.OrderBy(e => e.Country),
                "country_desc" => events.OrderByDescending(e => e.Country),
                "date" => events.OrderBy(e => e.StartDate),
                "date_desc" => events.OrderByDescending(e => e.StartDate),
                "id_desc" => events.OrderByDescending(e => e.Id),
                _ => events.OrderBy(e => e.Id)
            };

            pageNumber ??= 1;
            return await events
                .Skip((pageNumber.Value - 1) * maxItem)
                .Take(maxItem)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
