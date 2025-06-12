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
            .Where(@event => string.IsNullOrEmpty(category) || EF.Functions.ILike(@event.Category, $"%{category}%"))
            .Where(@event => date == null || DateOnly.FromDateTime(@event.StartAt) <= date && DateOnly.FromDateTime(@event.EndAt) >= date);
        
        var eventsCount = await filteredEventsQuery.CountAsync();
        
        var events = await filteredEventsQuery
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

    [HttpGet("/events/{id:guid}")]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        var @event = await dbContext.Events
            .SingleOrDefaultAsync(@event => @event.Id == id);
        
        if (@event is null)
        {
            return NotFound();
        }

        var fullEventDto = mapper.Map<FullEventDto>(@event);
        return Ok(fullEventDto);
    }

    [HttpPost("/events")]
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
    
    [HttpPut("/events/{id:guid}")]
    public async Task<IActionResult> EditEvent(Guid id, UpdateEventDto updateEventDto)
    {
        var updateEventValidatorResult = updateEventDtoValidator.Validate(updateEventDto);
        
        if (!updateEventValidatorResult.IsValid)
        {
            return BadRequest(updateEventValidatorResult.Errors);
        }
        
        var eventToUpdate = await dbContext.Events.SingleOrDefaultAsync(@event => @event.Id == id);
        
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
        
        mapper.Map(updateEventDto, eventToUpdate);
        
        await dbContext.SaveChangesAsync();
        
        return NoContent();
    }

    [HttpPost("/events/{eventId:guid}/registrations")]
    [Authorize]
    public async Task<IActionResult> EventRegistration(Guid eventId)
    {
        var events = await dbContext.Events
            .Where(@event => @event.Id == eventId)
            .SingleOrDefaultAsync();
    
        if (events is null)
        {
            return NotFound();
        }

        var currentUser = await userManager.GetUserAsync(User);
        
        var isUserRegistered = await dbContext.EventRegistrations
            .AnyAsync(r => r.UserId == currentUser!.Id && r.EventId == eventId);

        if (isUserRegistered)
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
}