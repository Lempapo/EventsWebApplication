using AutoMapper;
using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;

namespace EventsWebApplication.Mappings;

public class AccountsMappingProfile : Profile
{
    public AccountsMappingProfile()
    {
        CreateMap<RegisterDto, ApplicationUser>().ForMember(
            applicationUser => applicationUser.UserName,
            opt => opt.MapFrom(registerDto => registerDto.Email)
        );

        CreateMap<ApplicationUser, EventParticipantDto>();
    }
}