using AutoMapper;
using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;

namespace EventsWebApplication.Mappings;

public class EventsMappingProfile : Profile 
{
    public EventsMappingProfile() 
    {
        CreateMap<Event, FullEventDto>();
        CreateMap<Event, ShortEventDto>();
        CreateMap<UpdateEventDto, Event>();
        CreateMap<CreateEventDto, Event>().ForMember(
            @event => @event.Id,
            options => options.MapFrom(_ => Guid.NewGuid())
        );
    }
}