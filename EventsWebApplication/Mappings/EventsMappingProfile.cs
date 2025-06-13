using AutoMapper;
using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;

namespace EventsWebApplication.Mappings;

public class EventsMappingProfile : Profile 
{
    public EventsMappingProfile() 
    {
        CreateMap<Event, FullEventDto>();
        
        CreateMap<Event, ShortEventDto>().ForMember(
            shortEventDto => shortEventDto.CurrentParticipantsCount,
            options => options.MapFrom(@event => @event.EventRegistrations.Count)
        );
        
        CreateMap<UpdateEventDto, Event>();
        
        CreateMap<CreateEventDto, Event>().ForMember(
            @event => @event.Id,
            options => options.MapFrom(_ => Guid.NewGuid())
        );
    }
}