namespace EventsWebApplication.Dtos;

public class ShortEventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string Location { get; init; }
    public string? Category { get; init; }
    public int MaxParticipantsCount { get; init; }
    public string? ImageFileId { get; init; }
}