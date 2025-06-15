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

    public EventsService(
        IMapper mapper,
        EventsRepository eventsRepository)
    {
        this.mapper = mapper;
        this.eventsRepository = eventsRepository;
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
}