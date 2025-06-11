namespace EventsWebApplication.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Location { get; set; }
    public string? Category { get; set; }
    public int MaxParticipantsCount { get; set; }
    public string? ImageFileId { get; set; }
}