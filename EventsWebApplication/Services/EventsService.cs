using AutoMapper;
using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;
using EventsWebApplication.Exceptions;
using EventsWebApplication.Repositories;

namespace EventsWebApplication.Services;

public class EventsService
{
    private readonly IMapper mapper;
    private readonly EventsRepository eventsRepository;
    private readonly EventRegistrationsRepository eventRegistrationsRepository;

    public EventsService(
        IMapper mapper,
        EventsRepository eventsRepository,
        EventRegistrationsRepository eventRegistrationsRepository)
    {
        this.mapper = mapper;
        this.eventsRepository = eventsRepository;
        this.eventRegistrationsRepository = eventRegistrationsRepository;
    }
    
    public async Task<FullEventDto> GetEventById(Guid eventId)
    {
        var @event = await eventsRepository.GetEventByIdOrDefaultAsync(eventId);
        
        if (@event is null)
        {
            throw new ResourceNotFoundException($"Event with ID:{eventId} doesn't exist");
        }

        var fullEventDto = mapper.Map<FullEventDto>(@event);
        
        return fullEventDto;
    }

    public async Task RegisterForEvent(Guid eventId, string currentUserId)
    {
        var @event = await eventsRepository.GetEventByIdOrDefaultAsync(eventId);
    
        if (@event is null)
        {
            throw new ResourceNotFoundException($"Event with ID:{eventId} doesn't exist");
        }
        
        var isUserRegistered = await eventRegistrationsRepository.IsUserRegisteredForEventAsync(eventId, currentUserId);

        if (isUserRegistered)
        {
            throw new BusinessRuleViolationException("Current user is already registered for this event");
        }
       
        var eventRegistrationsCount = await eventRegistrationsRepository.GetEventRegistrationsCountAsync(eventId);

        if (eventRegistrationsCount >= @event.MaxParticipantsCount)
        {
            throw new BusinessRuleViolationException("Event has reached the maximum number of participants");
        }
    
        var eventRegistration = new EventRegistration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = currentUserId,
            RegistrationDate = DateOnly.FromDateTime(DateTime.Today)
        };

        await eventRegistrationsRepository.InsertEventRegistrationAsync(eventRegistration);
    }

    public async Task UnregisterForEvent(Guid eventId, string currentUserId)
    {
        var @event = await eventsRepository.GetEventByIdOrDefaultAsync(eventId);
        
        if (@event is null)
        {
            throw new ResourceNotFoundException($"Event with ID:{eventId} doesn't exist");
        }
       
        var eventRegistration = await eventRegistrationsRepository.GetEventRegistrationOrDefaultAsync(eventId, currentUserId);

        if (eventRegistration is null)
        {
           throw new BusinessRuleViolationException("Current user is not registered for this event");
        }
        
        await eventRegistrationsRepository.DeleteFromEventRegistrationsAsync(eventRegistration);
    }

    public async Task<List<ShortEventParticipantDto>> GetEventParticipants(Guid eventId) 
    {
        var @event = await eventsRepository.GetEventByIdOrDefaultAsync(eventId);

        if (@event is null)
        {
            throw new ResourceNotFoundException($"Event with ID:{eventId} doesn't exist");
        }
        
        var eventRegistrations = await eventRegistrationsRepository.GetEventRegistrationsAsync(eventId);
        
        var eventRegistrationDtos = eventRegistrations
            .Select(eventRegistration => mapper.Map<ShortEventParticipantDto>(eventRegistration))
            .ToList();

        return eventRegistrationDtos;
    }

    public async Task<List<Event>> GetUserEvents(string userId)
    {
        var events = await eventRegistrationsRepository.GetUserEventsAsync(userId);

        return events;
    }

    public async Task<FullEventParticipantDto> GetEventParticipant(Guid eventId, string participantId)
    {
        var @event = await eventsRepository.GetEventByIdOrDefaultAsync(eventId);

        if (@event is null)
        {
            throw new ResourceNotFoundException($"Event with ID: {eventId} doesn't exist");
        }

        var eventRegistration = @event.EventRegistrations
            .Where(eventRegistration => eventRegistration.UserId == participantId)
            .SingleOrDefault();

        if (eventRegistration is null)
        {
            throw new ResourceNotFoundException($"Participant with ID: {participantId} doesn't exist");
        }
        
        var userDto = mapper.Map<FullEventParticipantDto>(eventRegistration);
        
        return userDto;
    }

    public async Task<FullEventDto> CreateEvent(CreateEventDto createEventDto)
    {
        var newEvent = mapper.Map<Event>(createEventDto);
        
        await eventsRepository.InsertEventAsync(newEvent);
        
        var newFullEventDto = mapper.Map<FullEventDto>(newEvent);

        return newFullEventDto;
    }

    public async Task EditEvent(Guid eventId, UpdateEventDto updateEventDto)
    {
        var eventToUpdate = await eventsRepository.GetEventByIdOrDefaultAsync(eventId);
        
        if (eventToUpdate is null)
        {
            throw new ResourceNotFoundException($"Event with ID: {eventId} doesn't exist");
        }
        
        if (updateEventDto.ImageFileId is not null && eventToUpdate.ImageFileId != updateEventDto.ImageFileId)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", updateEventDto.ImageFileId);
        
            if (!File.Exists(filePath))
            {
                throw new ResourceNotFoundException("File doesn't exist");
            }
        }
        
        mapper.Map(updateEventDto, eventToUpdate);
        
        await eventsRepository.UpdateEventAsync(eventToUpdate);
        
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
    }
    
    // Event deletion options:
    // a) Notify registered users.
    // b) Don't delete event if there are users registered for it.
    // c) Delete event without notifying users.

    public async Task<PageDto<ShortEventDto>> GetEvents(
        string? title, 
        string? location, 
        string? category, 
        DateOnly? date,
        int pageNumber, 
        int pageSize)
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

        return pageDto;
    }
}