using Microsoft.AspNetCore.Identity;

namespace EventsWebApplication.Dtos;

public class EventParticipantDto
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string UserName { get; init; }
}