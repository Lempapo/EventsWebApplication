using System.ComponentModel.DataAnnotations;
using AutoMapper;
using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventsWebApplication.Controllers;

[ApiController]
public class EventsController : ControllerBase
{
    private readonly EventsDbContext dbContext;
    private readonly UpdateEventDtoValidator updateEventDtoValidator;
    private readonly CreateEventDtoValidator createEventDtoValidator;
    private readonly IMapper mapper;
    private readonly UserManager<ApplicationUser> userManager;
    
    public EventsController(
        EventsDbContext dbContext, 
        CreateEventDtoValidator createEventDtoValidator, 
        UpdateEventDtoValidator updateEventDtoValidator,
        IMapper mapper,
        UserManager<ApplicationUser> userManager)
    {
        this.dbContext = dbContext;
        this.createEventDtoValidator = createEventDtoValidator;
        this.updateEventDtoValidator = updateEventDtoValidator;
        this.mapper = mapper;
        this.userManager = userManager;
    }
    
    [HttpGet("/events")]
    public async Task<IActionResult> GetEvents(
        string? title, 
        string? location, 
        string? category, 
        DateOnly? date,
        [Required][Range(1, int.MaxValue)] int pageNumber, 
        [Required][Range(1, 50)] int pageSize)
    {
        var filteredEventsQuery = dbContext.Events
            .Where(@event => string.IsNullOrEmpty(title) || EF.Functions.ILike(@event.Title, $"%{title}%"))
            .Where(@event => string.IsNullOrEmpty(location) || EF.Functions.ILike(@event.Location, $"%{location}%"))
            .Where(@event => string.IsNullOrEmpty(category) || @event.Category != null && EF.Functions.ILike(@event.Category, $"%{category}%"))
            .Where(@event => date == null || DateOnly.FromDateTime(@event.StartAt) <= date && DateOnly.FromDateTime(@event.EndAt) >= date);
        
        var eventsCount = await filteredEventsQuery.CountAsync();
        
        var events = await filteredEventsQuery
            .Include(@event => @event.EventRegistrations)
            .OrderBy(@event => @event.StartAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        var eventDtos = mapper.Map<List<ShortEventDto>>(events);
        
        var pageDto = new PageDto<ShortEventDto>
        {
            Items = eventDtos.ToList(),
            TotalItemsCount = eventsCount,
            PageSize = pageSize,
            PagesCount = (int)Math.Ceiling((double)eventsCount / pageSize)
        };
        
        return Ok(pageDto);
    }

    [HttpGet("/events/{eventId:guid}")]
    public async Task<IActionResult> GetEventById(Guid eventId)
    {
        var @event = await dbContext.Events
            .Include(@event => @event.EventRegistrations)
            .SingleOrDefaultAsync(@event => @event.Id == eventId);

        if (@event is null)
        {
            return NotFound();
        }

        var fullEventDto = mapper.Map<FullEventDto>(@event);
        
        return Ok(fullEventDto);
    }

    [HttpPost("/events")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> CreateEvent(CreateEventDto createEventDto)
    {
        var createEventDtoValidationResult = createEventDtoValidator.Validate(createEventDto);

        if (!createEventDtoValidationResult.IsValid)
        {
            return BadRequest(createEventDtoValidationResult.Errors);
        }

        if (createEventDto.ImageFileId is not null)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", createEventDto.ImageFileId);
        
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }
        }

        var newEvent = mapper.Map<Event>(createEventDto);
        
        dbContext.Add(newEvent);
        await dbContext.SaveChangesAsync();
        
        var newFullEventDto = mapper.Map<FullEventDto>(newEvent);
        
        return Ok(newFullEventDto);
    }
    
    [HttpPut("/events/{eventId:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> EditEvent(Guid eventId, UpdateEventDto updateEventDto)
    {
        var updateEventValidatorResult = updateEventDtoValidator.Validate(updateEventDto);
        
        if (!updateEventValidatorResult.IsValid)
        {
            return BadRequest(updateEventValidatorResult.Errors);
        }
        
        var eventToUpdate = await dbContext.Events.SingleOrDefaultAsync(@event => @event.Id == eventId);
        
        if (eventToUpdate is null)
        {
            return NotFound();
        }
        
        if (updateEventDto.ImageFileId is not null && eventToUpdate.ImageFileId != updateEventDto.ImageFileId)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", updateEventDto.ImageFileId);
        
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }
        }
        
        // Scenario:
        // 1. Event with max participants count = 50 created.
        // 2. 50 users registered for the event.
        // 3. User changed max participants count from 50 to 20.
        // 4. Now we have event with max participants count = 20 and current participants count = 50.
        
        // Scenario:
        // 1. Admin #1 creates a new event.
        // 2. Admin #2 edits this event.
        // 3. Admin #1 doesn't like it and edits it back.
        // 4. Admin #2 edits this event again.
        // We can forbid admins from editing other admin's events.
        
        mapper.Map(updateEventDto, eventToUpdate);
        
        await dbContext.SaveChangesAsync();
        
        return NoContent();
    }
    
    // Event deletion options:
    // a) Notify registered users.
    // b) Don't delete event if there are users registered for it.
    // c) Delete event without notifying users.

    [HttpPost("/events/{eventId:guid}/registrations")]
    [Authorize]
    public async Task<IActionResult> RegisterForEvent(Guid eventId)
    {
        var @event = await dbContext.Events
            .Where(@event => @event.Id == eventId)
            .SingleOrDefaultAsync();
    
        if (@event is null)
        {
            return NotFound();
        }

        var currentUser = await userManager.GetUserAsync(User);

        var isUserRegistered = await dbContext.EventRegistrations
            .Where(eventRegistration => eventRegistration.UserId == currentUser!.Id)
            .Where(eventRegistration => eventRegistration.EventId == eventId)
            .AnyAsync();

        if (isUserRegistered)
        {
            return BadRequest();
        }
        
        var eventRegistrationsCount = await dbContext.EventRegistrations
            .Where(eventRegistration => eventRegistration.EventId == eventId)
            .CountAsync();

        if (eventRegistrationsCount >= @event.MaxParticipantsCount)
        {
            return BadRequest();
        }
    
        var eventRegistration = new EventRegistration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = currentUser!.Id,
            RegistrationDate = DateOnly.FromDateTime(DateTime.Today)
        };
        
        dbContext.EventRegistrations.Add(eventRegistration);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }
    
    [HttpGet("/events/{eventId:guid}/participants")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetEventParticipants(Guid eventId)
    {
        var @event = await dbContext.Events
            .Include(@event => @event.EventRegistrations)
            .ThenInclude(eventRegistration => eventRegistration.User)
            .SingleOrDefaultAsync(@event => @event.Id == eventId);

        if (@event is null)
        {
            return NotFound();
        }
        
        var eventRegistrations = await dbContext.EventRegistrations
            .Include(eventRegistration => eventRegistration.User)
            .Where(eventRegistration => eventRegistration.EventId == eventId)
            .ToListAsync();
        
        var eventRegistrationDtos = eventRegistrations.Select(eventRegistration => mapper.Map<ShortEventParticipantDto>(eventRegistration));

        return Ok(eventRegistrationDtos);
    }
    
    [HttpDelete("/events/{eventId:guid}/registrations")]
    [Authorize]
    public async Task<IActionResult> UnregisterFromEvent(Guid eventId)
    {
        var @event = await dbContext.Events
            .Where(@event => @event.Id == eventId)
            .SingleOrDefaultAsync();
        
        if (@event is null)
        {
            return NotFound();
        }
        
        var currentUser = await userManager.GetUserAsync(User);
        
        var eventRegistration = await dbContext.EventRegistrations
            .Where(eventRegistration => eventRegistration.EventId == eventId)
            .Where(eventRegistration => eventRegistration.UserId == currentUser!.Id)
            .SingleOrDefaultAsync();

        if (eventRegistration is null)
        {
            return BadRequest();
        }
        
        dbContext.EventRegistrations.Remove(eventRegistration);
        await dbContext.SaveChangesAsync();
        
        return Ok();
    }
    
    [HttpGet("/user/events")]
    [Authorize]
    public async Task<IActionResult> GetUserEvents()
    {
        var user = await userManager.GetUserAsync(User);
        
        var events = await dbContext.EventRegistrations
            .Where(@event => @event.UserId == user!.Id)
            .Select(@event => @event.Event)
            .ToListAsync();
        
        return Ok(events);
    }
    
    [HttpGet("/events/{eventId:guid}/participants/{participantId}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetEventParticipant(Guid eventId, string participantId)
    {
        var @event = await dbContext.Events
            .Where(@event => @event.Id == eventId)
            .Include(@event => @event.EventRegistrations)
            .ThenInclude(eventRegistration => eventRegistration.User)
            .SingleOrDefaultAsync();

        if (@event is null)
        {
            return NotFound();
        }

        var eventRegistration = @event.EventRegistrations
            .Where(eventRegistration => eventRegistration.UserId == participantId)
            .SingleOrDefault();

        if (eventRegistration is null)
        {
            return NotFound();
        }
        
        var userDto = mapper.Map<FullEventParticipantDto>(eventRegistration);
        
        return Ok(userDto);
    }
}