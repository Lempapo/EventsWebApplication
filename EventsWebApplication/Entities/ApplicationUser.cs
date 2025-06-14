using Microsoft.AspNetCore.Identity;

namespace EventsWebApplication.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateOnly Birthday { get; set; }
    
    public List<EventRegistration> EventRegistrations { get; set; }
}