using AutoMapper;
using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;

namespace EventsWebApplication.Mappings;

public class EventsMappingProfile : Profile 
{
    public EventsMappingProfile() 
    {
        CreateMap<Event, ShortEventDto>().ForMember(
            shortEventDto => shortEventDto.CurrentParticipantsCount,
            options => options.MapFrom(@event => @event.EventRegistrations.Count)
        );

        CreateMap<Event, FullEventDto>().ForMember(
            fullEventDto => fullEventDto.CurrentParticipantsCount,
            options => options.MapFrom(@event => @event.EventRegistrations.Count)
        );
        
        CreateMap<UpdateEventDto, Event>();
        
        CreateMap<CreateEventDto, Event>().ForMember(
            @event => @event.Id,
            options => options.MapFrom(_ => Guid.NewGuid())
        );

        CreateMap<EventRegistration, EventParticipantDto>()
            .ForMember(
                eventParticipantDto => eventParticipantDto.FirstName,
                options => options.MapFrom(eventRegistration => eventRegistration.User.UserName))
            .ForMember(
                eventParticipantDto => eventParticipantDto.LastName,
                options => options.MapFrom(eventRegistration => eventRegistration.User.LastName))
            .ForMember(
                eventParticipantDto => eventParticipantDto.UserName,
                options => options.MapFrom(eventRegistration => eventRegistration.User.UserName)
            );
    }
}