using EventsWebApplication.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventsWebApplication.Repositories;

public class EventRegistrationsRepository
{
    private readonly EventsDbContext dbContext;

    public EventRegistrationsRepository(EventsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<bool> IsUserRegisteredForEventAsync(Guid eventId, string userId)
    {
        var isUserRegistered = await dbContext.EventRegistrations
            .Where(eventRegistration => eventRegistration.UserId == userId)
            .Where(eventRegistration => eventRegistration.EventId == eventId)
            .AnyAsync();
        
        return isUserRegistered;
    }
    
    public async Task<int> GetEventRegistrationsCountAsync(Guid eventRegistrationId)
    {
        var eventRegistrationsCount = await dbContext.EventRegistrations
            .Where(eventRegistration => eventRegistration.EventId == eventRegistrationId)
            .CountAsync();

        return eventRegistrationsCount;
    }
    
    public async Task<List<EventRegistration>> GetEventRegistrationsAsync(Guid eventId)
    {
        var eventRegistrations = await dbContext.EventRegistrations
            .Include(eventRegistration => eventRegistration.User)
            .Where(eventRegistration => eventRegistration.EventId == eventId)
            .ToListAsync();

        return eventRegistrations;
    }

    public async Task<EventRegistration?> GetEventRegistrationOrDefaultAsync(Guid eventId, string userId)
    {
        var eventRegistration = await dbContext.EventRegistrations
            .Where(eventRegistration => eventRegistration.EventId == eventId)
            .Where(eventRegistration => eventRegistration.UserId == userId)
            .SingleOrDefaultAsync();

        return eventRegistration;
    }

    public async Task<List<Event>> GetUserEventsAsync(string userId)
    {
        var events = await dbContext.EventRegistrations
            .Where(@event => @event.UserId == userId)
            .Select(@event => @event.Event)
            .ToListAsync();

        return events;
    }
    
    public async Task InsertEventRegistrationAsync(EventRegistration newEventRegistration)
    {
        dbContext.EventRegistrations.Add(newEventRegistration);
        await dbContext.SaveChangesAsync();
    }
    
    
    public async Task DeleteFromEventRegistrationsAsync(EventRegistration eventRegistration)
    {
        dbContext.EventRegistrations.Remove(eventRegistration);
        await dbContext.SaveChangesAsync();
    }

}