﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EventHub.Models;

namespace EventHub.DataAccess.Data
{
    public class EventManagementSystemDbContext : IdentityDbContext<User>
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }

        public EventManagementSystemDbContext(DbContextOptions<EventManagementSystemDbContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<TicketType>()
                .HasOne(tt => tt.Event)
                .WithMany(e => e.TicketTypes)
                .HasForeignKey(tt => tt.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<User>()
                .HasOne(u => u.UserAddress)
                .WithOne(ua => ua.User);

            builder.Entity<User>()
                .HasMany(u => u.Events)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            //builder.Entity<Ticket>()
            //    .HasOne(t => t.User)
            //    .WithMany(u => u.Tickets)
            //    .HasForeignKey(t => t.UserId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //builder.Entity<Ticket>()
            //    .HasOne(t => t.TicketType)
            //    .WithMany(tt => tt.Tickets)
            //    .HasForeignKey(t => t.TicketTypeId)
            //    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
