using System.ComponentModel.DataAnnotations;
using AutoMapper;
using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;
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
    private readonly UserManager<ApplicationUser> userManager;
    private readonly EventsService eventsService;

    public EventsController( 
        CreateEventDtoValidator createEventDtoValidator, 
        UpdateEventDtoValidator updateEventDtoValidator,
        UserManager<ApplicationUser> userManager,
        EventsService eventsService)
    {
        this.createEventDtoValidator = createEventDtoValidator;
        this.updateEventDtoValidator = updateEventDtoValidator;
        this.userManager = userManager;
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
        var pageDto = await eventsService.GetEvents(title, location, category, date, pageNumber, pageSize);
        
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
        
        var newFullEventDto = await eventsService.CreateEvent(createEventDto);
        
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
        
        await eventsService.EditEvent(eventId, updateEventDto);
        
        return NoContent();
    }

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
        var eventRegistrationDtos = await eventsService.GetEventParticipants(eventId);

        return Ok(eventRegistrationDtos);
    }
    
    [HttpDelete("/events/{eventId:guid}/registrations")]
    [Authorize]
    public async Task<IActionResult> UnregisterFromEvent(Guid eventId)
    {
        var currentUser = await userManager.GetUserAsync(User);
        
        await eventsService.UnregisterForEvent(eventId, currentUser!.Id);
        
        return Ok();
    }
    
    [HttpGet("/user/events")]
    [Authorize]
    public async Task<IActionResult> GetUserEvents()
    {
        var user = await userManager.GetUserAsync(User);

        var events = await eventsService.GetUserEvents(user!.Id);
        
        return Ok(events);
    }
    
    [HttpGet("/events/{eventId:guid}/participants/{participantId}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetEventParticipant(Guid eventId, string participantId)
    {
        var userDto = await eventsService.GetEventParticipant(eventId, participantId);
        
        return Ok(userDto);
    }
}