using EventsWebApplication.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventsWebApplication;

public class EventsDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Event> Events { get; set; }
    public DbSet<EventRegistration> EventRegistrations { get; set; }
    
    public EventsDbContext(DbContextOptions<EventsDbContext> options) 
        : base(options) 
    { }
}