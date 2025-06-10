using FluentValidation;

namespace EventsWebApplication.Dtos;

public class CreateEventDto
{
    public string Title { get; init; }
    public string Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string Location { get; init; }
    public string? Category { get; init; }
    public int MaxParticipantsCount { get; init; }
    public string? ImageFileId { get; init; }
}

public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    public CreateEventDtoValidator()
    {
        RuleFor(createEventDto => createEventDto.Title).NotEmpty().MaximumLength(250);
        RuleFor(createEventDto => createEventDto.Description).NotEmpty().MaximumLength(10000);
        RuleFor(createEventDto => createEventDto.StartAt).LessThan(createEventDto => createEventDto.EndAt);
        RuleFor(createEventDto => createEventDto.Location).NotEmpty().MaximumLength(250);
        RuleFor(createEventDto => createEventDto.Category).MaximumLength(100);
        RuleFor(createEventDto => createEventDto.MaxParticipantsCount).GreaterThan(0).LessThan(10000);
        RuleFor(createEventDto => createEventDto.ImageFileId).MaximumLength(41);
    }
}