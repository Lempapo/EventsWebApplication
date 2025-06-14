using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventsWebApplication.Repositories;

public class EventsRepository
{
    private readonly EventsDbContext dbContext;

    public EventsRepository(EventsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<(List<Event> PaginatedEvents, int TotalEventsCount)> GetPaginatedEventsAsync(
        string? title, 
        string? location, 
        string? category, 
        DateOnly? date,
        int pageNumber, 
        int pageSize)
    {
        var filteredEventsQuery = dbContext.Events
            .Where(@event => string.IsNullOrEmpty(title) || EF.Functions.ILike(@event.Title, $"%{title}%"))
            .Where(@event => string.IsNullOrEmpty(location) || EF.Functions.ILike(@event.Location, $"%{location}%"))
            .Where(@event => string.IsNullOrEmpty(category) || @event.Category != null && EF.Functions.ILike(@event.Category, $"%{category}%"))
            .Where(@event => date == null || DateOnly.FromDateTime(@event.StartAt) <= date && DateOnly.FromDateTime(@event.EndAt) >= date);

        var totalEventsCount = await filteredEventsQuery.CountAsync();
        
        var paginatedEvents = await filteredEventsQuery
            .Include(@event => @event.EventRegistrations)
            .OrderBy(@event => @event.StartAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (paginatedEvents, totalEventsCount);
    }
    
    public async Task<Event?> GetEventByIdOrDefaultAsync(Guid eventId)
    {
        var @event = await dbContext.Events
            .Include(@event => @event.EventRegistrations)
            .ThenInclude(eventRegistration => eventRegistration.User)
            .SingleOrDefaultAsync(@event => @event.Id == eventId);
        
        return @event;
    }

    public async Task InsertEventAsync(Event newEvent)
    {
        dbContext.Events.Add(newEvent);
        await dbContext.SaveChangesAsync();
    }
    
    public async Task UpdateEventAsync(Event eventToUpdate)
    {
        dbContext.Events.Update(eventToUpdate);
        await dbContext.SaveChangesAsync();
    }
}