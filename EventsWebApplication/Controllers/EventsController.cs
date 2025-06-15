using System.ComponentModel.DataAnnotations;
using AutoMapper;
using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;
using EventsWebApplication.Repositories;
using EventsWebApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EventsWebApplication.Controllers;

[ApiController]
public class EventsController : ControllerBase
{
    private readonly UpdateEventDtoValidator updateEventDtoValidator;
    private readonly CreateEventDtoValidator createEventDtoValidator;
    private readonly IMapper mapper;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly EventsRepository eventsRepository;
    private readonly EventRegistrationsRepository eventRegistrationsRepository;
    private readonly EventsService eventsService;

    public EventsController( 
        CreateEventDtoValidator createEventDtoValidator, 
        UpdateEventDtoValidator updateEventDtoValidator,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        EventsRepository eventsRepository,
        EventRegistrationsRepository eventRegistrationsRepository,
        EventsService eventsService)
    {
        this.createEventDtoValidator = createEventDtoValidator;
        this.updateEventDtoValidator = updateEventDtoValidator;
        this.mapper = mapper;
        this.userManager = userManager;
        this.eventsRepository = eventsRepository;
        this.eventRegistrationsRepository = eventRegistrationsRepository;
        this.eventsService = eventsService;
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
        var (paginatedEvents, totalEventsCount) = await eventsRepository.GetPaginatedEventsAsync(
            title,
            location,
            category,
            date,
            pageNumber,
            pageSize
        );
        
        var eventDtos = mapper.Map<List<ShortEventDto>>(paginatedEvents);
        
        var pageDto = new PageDto<ShortEventDto>
        {
            Items = eventDtos.ToList(),
            TotalItemsCount = totalEventsCount,
            PageSize = pageSize,
            PagesCount = (int)Math.Ceiling((double)totalEventsCount / pageSize)
        };
        
        return Ok(pageDto);
    }

    [HttpGet("/events/{eventId:guid}")]
    public async Task<IActionResult> GetEventById(Guid eventId)
    {
        var @event = await eventsService.GetEventById(eventId);
        return Ok(@event);
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
        
        await eventsRepository.InsertEventAsync(newEvent);
        
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

        var eventToUpdate = await eventsRepository.GetEventByIdOrDefaultAsync(eventId);
        
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
        
        await eventsRepository.UpdateEventAsync(eventToUpdate);
        
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
        var currentUser = await userManager.GetUserAsync(User);

        await eventsService.RegisterForEvent(eventId, currentUser!.Id);
        
        return NoContent();
    }
    
    [HttpGet("/events/{eventId:guid}/participants")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetEventParticipants(Guid eventId)
    {
        var @event = await eventsRepository.GetEventByIdOrDefaultAsync(eventId);

        if (@event is null)
        {
            return NotFound();
        }
        
        var eventRegistrations = await eventRegistrationsRepository.GetEventRegistrationsAsync(eventId);
        
        var eventRegistrationDtos = eventRegistrations.Select(eventRegistration => mapper.Map<ShortEventParticipantDto>(eventRegistration));

        return Ok(eventRegistrationDtos);
    }
    
    [HttpDelete("/events/{eventId:guid}/registrations")]
    [Authorize]
    public async Task<IActionResult> UnregisterFromEvent(Guid eventId)
    {
        var @event = await eventsRepository.GetEventByIdOrDefaultAsync(eventId);
        
        if (@event is null)
        {
            return NotFound();
        }
        
        var currentUser = await userManager.GetUserAsync(User);
       
        var eventRegistration = await eventRegistrationsRepository.GetEventRegistrationOrDefaultAsync(eventId, currentUser!.Id);

        if (eventRegistration is null)
        {
            return BadRequest();
        }
        
        await eventRegistrationsRepository.DeleteFromEventRegistrationsAsync(eventRegistration);
        
        return Ok();
    }
    
    [HttpGet("/user/events")]
    [Authorize]
    public async Task<IActionResult> GetUserEvents()
    {
        var user = await userManager.GetUserAsync(User);
        
        var events = await eventRegistrationsRepository.GetUserEventsAsync(user!.Id);
        
        return Ok(events);
    }
    
    [HttpGet("/events/{eventId:guid}/participants/{participantId}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetEventParticipant(Guid eventId, string participantId)
    {
        var @event = await eventsRepository.GetEventByIdOrDefaultAsync(eventId);

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