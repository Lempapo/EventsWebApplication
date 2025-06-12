namespace EventsWebApplication.Entities;

public class EventRegistration
{
    public Guid Id { get; init; }
    
    public Guid EventId { get; set; }
    public Event Event { get; set; }
    
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    
    public DateOnly RegistrationDate { get; set; }
}