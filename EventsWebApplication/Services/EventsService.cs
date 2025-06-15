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
}