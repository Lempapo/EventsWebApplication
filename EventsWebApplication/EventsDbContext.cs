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
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder
            .Entity<EventRegistration>()
            .HasIndex(eventRegistration => new
            {
                eventRegistration.EventId, 
                eventRegistration.UserId 
            })
            .IsUnique();
    }
}