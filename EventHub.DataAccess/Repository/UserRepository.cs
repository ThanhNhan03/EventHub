﻿using EventHub.DataAccess.Data;
using EventHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace EventHub.DataAccess.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly EventManagementSystemDbContext _eventManagementSystemDbContext;
        private readonly UserManager<User> _userManager;

        public UserRepository(EventManagementSystemDbContext eventManagementSystemDbContext, UserManager<User> userManager)
        {
            _eventManagementSystemDbContext = eventManagementSystemDbContext;
            _userManager = userManager;
        }

        public async Task<IdentityResult> ChangeDisplayNameAsync(int userId, string newDisplayName)
        {
            var user = await _eventManagementSystemDbContext.Users.FindAsync(userId.ToString());
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            user.DisplayName = newDisplayName;
            _eventManagementSystemDbContext.Users.Update(user);
            await _eventManagementSystemDbContext.SaveChangesAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> ChangeEmailAsync(int userId, string newEmail)
        {
            var user = await _eventManagementSystemDbContext.Users.FindAsync(userId.ToString());
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            user.Email = newEmail;
            user.NormalizedEmail = newEmail.ToUpper();
            _eventManagementSystemDbContext.Users.Update(user);
            await _eventManagementSystemDbContext.SaveChangesAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> ChangePasswordAsync(int userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return await _userManager.ResetPasswordAsync(user, token, newPassword);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _eventManagementSystemDbContext.Users.ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetAllEventAsync(int userId, int ticketId, Period period = Period.Month)
        {
            var user = await _eventManagementSystemDbContext.Users
                .Include(u => u.Tickets)
                .ThenInclude(t => t.TicketType)
                .ThenInclude(tt => tt.Event)
                .FirstOrDefaultAsync(u => u.Id == userId.ToString());

            if (user == null || user.Tickets == null)
            {
                return Enumerable.Empty<Event>();
            }

            var now = DateTime.Now;
            var events = user.Tickets
                .Where(t => ticketId == 0 || t.Id == ticketId)
                .Select(t => t.TicketType?.Event)
                .Where(e => e != null)
                .Cast<Event>();

            switch (period)
            {
                case Period.Day:
                    return events.Where(e => e.StartDate.ToDateTime(TimeOnly.MinValue).Date == now.Date);
                case Period.Month:
                    return events.Where(e => e.StartDate.ToDateTime(TimeOnly.MinValue).Month == now.Month && 
                                            e.StartDate.ToDateTime(TimeOnly.MinValue).Year == now.Year);
                case Period.Year:
                    return events.Where(e => e.StartDate.ToDateTime(TimeOnly.MinValue).Year == now.Year);
                case Period.Future:
                    return events.Where(e => e.StartDate.ToDateTime(TimeOnly.MinValue) > now);
                case Period.Past:
                    return events.Where(e => e.EndDate.ToDateTime(TimeOnly.MinValue) < now);
                default:
                    return events;
            }
        }

        public async Task<IEnumerable<Ticket>> GetAllTicketAsync(int userId)
        {
            var user = await _eventManagementSystemDbContext.Users
                .Include(u => u.Tickets)
                .ThenInclude(t => t.TicketType)
                .FirstOrDefaultAsync(u => u.Id == userId.ToString());

            return user?.Tickets ?? Enumerable.Empty<Ticket>();
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _eventManagementSystemDbContext.Users
                .Include(u => u.Tickets)
                .Include(u => u.Events)
                .Include(u => u.UserAddress)
                .FirstOrDefaultAsync(u => u.Id == userId.ToString());
        }

        public async Task<User?> GetByNameAsync(string name)
        {
            return await _eventManagementSystemDbContext.Users
                .Include(u => u.Tickets)
                .Include(u => u.Events)
                .Include(u => u.UserAddress)
                .FirstOrDefaultAsync(u => u.UserName == name || u.DisplayName == name);
        }

        public async Task<Event?> GetEventByTicketAsync(int userId, int ticketId)
        {
            var ticket = await _eventManagementSystemDbContext.Tickets
                .Include(t => t.TicketType)
                .ThenInclude(tt => tt.Event)
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId.ToString());

            return ticket?.TicketType?.Event;
        }

        public async Task<Ticket?> GetTicketByIdAsync(int userId, int ticketId)
        {
            return await _eventManagementSystemDbContext.Tickets
                .Include(t => t.TicketType)
                .ThenInclude(tt => tt.Event)
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId.ToString());
        }

        public async Task<int> UpdateUserAsync(User user)
        {
            var existingUser = await _eventManagementSystemDbContext.Users.FindAsync(user.Id);
            if (existingUser is not null)
            {
                existingUser.Tickets = user.Tickets;
                _eventManagementSystemDbContext.Users.Update(existingUser);
                return await _eventManagementSystemDbContext.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("User not found.");
            }
        }

        public async Task<IEnumerable<Event>> GetRegisteredEventsAsync(string userId)
        {
            var user = await _eventManagementSystemDbContext.Users
                .Include(u => u.Tickets)
                    .ThenInclude(t => t.TicketType)
                        .ThenInclude(tt => tt.Event)
                .FirstOrDefaultAsync(u => u.Id == userId);
        
            if (user?.Tickets == null)
                return Enumerable.Empty<Event>();
        
            return user.Tickets
                .Select(t => t.TicketType?.Event)
                .Where(e => e != null)
                .Cast<Event>()
                .Distinct();
        }
    }
}
