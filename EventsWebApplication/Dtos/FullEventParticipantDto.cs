namespace EventsWebApplication.Dtos;

public class FullEventParticipantDto
{
    public string Id { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public DateOnly Birthday { get; init; }
    public DateOnly RegistrationDate { get; init; }
}