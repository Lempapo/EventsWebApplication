using EventsWebApplication.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventsWebApplication;

public class EventsDbContext : DbContext
{
    public DbSet<Event> Events { get; set; }
    
    public EventsDbContext(DbContextOptions<EventsDbContext> options) 
        : base(options) 
    { }
}