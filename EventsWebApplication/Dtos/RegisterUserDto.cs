namespace EventsWebApplication.Dtos;

public class RegisterUserDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    
    public string FirstName { get; set; }
    
    public string LastName { get; set; }
    
    public DateOnly Birthday { get; set; }
}