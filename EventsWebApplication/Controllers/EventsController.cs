using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsWebApplication.Controllers;

[ApiController]

public class EventsController : ControllerBase
{
    private readonly EventsDbContext dbContext;
    
    public EventsController(EventsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet("/events")]
    public async Task<IActionResult> GetEvents(string? title, string? location, string? category, DateOnly? date)
    {
        var events = await dbContext.Events
            .Where(@event => string.IsNullOrEmpty(title) || EF.Functions.ILike(@event.Title, $"%{title}%"))
            .Where(@event => string.IsNullOrEmpty(location) || EF.Functions.ILike(@event.Location, $"%{location}%"))
            .Where(@event => string.IsNullOrEmpty(category) || EF.Functions.ILike(@event.Category, $"%{category}%"))
            .Where(@event => date == null || DateOnly.FromDateTime(@event.StartAt) <= date && DateOnly.FromDateTime(@event.EndAt) >= date)
            .ToListAsync();
        
        var eventDtos = events.Select(@event => new ShortEventDto()
        {
            Id = @event.Id,
            Title = @event.Title,
            StartAt = @event.StartAt,
            EndAt = @event.EndAt,
            Location = @event.Location,
            Category = @event.Category,
            MaxParticipantsCount = @event.MaxParticipantsCount,
            ImageUrl = @event.ImageUrl
        });
        
        return Ok(eventDtos);
    }

    [HttpGet("/events/{id:guid}")]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        var @event = await dbContext.Events
            .SingleOrDefaultAsync(@event => @event.Id == id);
        
        if (@event is null)
        {
            return NotFound();
        }

        var fullEventDto = new FullEventDto()
        {
            Id = @event.Id,
            Title = @event.Title,
            Description = @event.Description,
            StartAt = @event.StartAt,
            EndAt = @event.EndAt,
            Location = @event.Location,
            Category = @event.Category,
            MaxParticipantsCount = @event.MaxParticipantsCount,
            ImageUrl = @event.ImageUrl
        };
        
        return Ok(fullEventDto);
    }

    [HttpPost("/events")]
    public async Task<IActionResult> CreateEvent(CreateEventDto createEventDto)
    {
        var newEvent = new Event()
        {
            Id = Guid.NewGuid(),
            Title = createEventDto.Title,
            Description = createEventDto.Description,
            StartAt = createEventDto.StartAt,
            EndAt = createEventDto.EndAt,
            Location = createEventDto.Location,
            Category = createEventDto.Category,
            MaxParticipantsCount = createEventDto.MaxParticipantsCount,
            ImageUrl = createEventDto.ImageUrl
        };
        
        dbContext.Add(newEvent);
        await dbContext.SaveChangesAsync();
        
        var newFullEventDto = new FullEventDto()
        {
            Id = newEvent.Id,
            Title = newEvent.Title,
            Description = newEvent.Description,
            StartAt = newEvent.StartAt,
            EndAt = newEvent.EndAt,
            Location = newEvent.Location,
            Category = newEvent.Category,
            MaxParticipantsCount = newEvent.MaxParticipantsCount,
            ImageUrl = newEvent.ImageUrl
        };
        
        return Ok(newFullEventDto);
    }
    
    [HttpPut("/events/{id:guid}")]
    public async Task<IActionResult> EditEvent(Guid id, UpdateEventDto updateEventDto)
    {
        var eventToUpdate = await dbContext.Events.SingleOrDefaultAsync(@event => @event.Id == id);
        
        if (eventToUpdate is null)
        {
            return NotFound();
        }
        
        eventToUpdate.Title = updateEventDto.Title;
        eventToUpdate.Description = updateEventDto.Description;
        eventToUpdate.StartAt = updateEventDto.StartAt;
        eventToUpdate.EndAt = updateEventDto.EndAt;
        eventToUpdate.Location = updateEventDto.Location;
        eventToUpdate.Category = updateEventDto.Category;
        eventToUpdate.MaxParticipantsCount = updateEventDto.MaxParticipantsCount;
        eventToUpdate.ImageUrl = updateEventDto.ImageUrl;
        
        dbContext.Update(eventToUpdate);
        await dbContext.SaveChangesAsync();
        
        return NoContent();
    }
}