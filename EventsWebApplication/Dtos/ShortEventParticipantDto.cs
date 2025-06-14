using Microsoft.AspNetCore.Identity;

namespace EventsWebApplication.Dtos;

public class ShortEventParticipantDto
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string UserName { get; init; }
    public DateOnly RegistrationDate { get; init; }
}