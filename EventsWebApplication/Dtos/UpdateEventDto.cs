using FluentValidation;

namespace EventsWebApplication.Dtos;

public class UpdateEventDto
{
    public string Title { get; init; }
    public string Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string Location { get; init; }
    public string? Category { get; init; }
    public int MaxParticipantsCount { get; init; }
    public string? ImageUrl { get; init; }
}

public class UpdateEventDtoValidator : AbstractValidator<UpdateEventDto>
{
    public UpdateEventDtoValidator()
    {
        RuleFor(updateEventDto => updateEventDto.Title).NotEmpty().MaximumLength(250);
        RuleFor(updateEventDto => updateEventDto.Description).NotEmpty().MaximumLength(10000);
        RuleFor(updateEventDto => updateEventDto.StartAt).LessThan(createEventDto => createEventDto.EndAt);
        RuleFor(updateEventDto => updateEventDto.Location).NotEmpty().MaximumLength(250);
        RuleFor(updateEventDto => updateEventDto.Category).MaximumLength(100);
        RuleFor(updateEventDto => updateEventDto.MaxParticipantsCount).GreaterThan(0).LessThan(10000);
        RuleFor(updateEventDto => updateEventDto.ImageUrl).MaximumLength(2048);
    }
}