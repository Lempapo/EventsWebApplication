using AutoMapper;
using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;
using EventsWebApplication.Exceptions;
using EventsWebApplication.Repositories;
using EventsWebApplication.Services;
using Microsoft.EntityFrameworkCore;

namespace EventsWebApplication.Tests;

public class EventsTests
{
    private readonly EventsService eventsService;
    private readonly EventsDbContext dbContext;

    public EventsTests()
    {
        var dbContextOptions = new DbContextOptionsBuilder<EventsDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        
        dbContext = new EventsDbContext(dbContextOptions);

        var eventsRepository = new EventsRepository(dbContext);
        var eventRegistrationsRepository = new EventRegistrationsRepository(dbContext);

        var mapperConfiguration = new MapperConfiguration(options => options.AddMaps(typeof(EventsService).Assembly));
        var mapper = mapperConfiguration.CreateMapper();
        
        eventsService = new EventsService(mapper, eventsRepository, eventRegistrationsRepository);
    }
    
    [Fact]
    public async Task GetEventById_WhenEventDoesNotExist_ShouldThrowNotFoundException()
    {
        var eventId = Guid.NewGuid();
        var act = async () => await eventsService.GetEventById(eventId);
        await Assert.ThrowsAsync<ResourceNotFoundException>(act);
    }

    [Fact]
    public async Task GetEventById_WhenEventExists_ShouldReturnFullEventDto()
    {
        var @event = new Event()
        {
            Id = Guid.NewGuid(),
            Title = "Test Event Title",
            Description = "Test Event Description",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddDays(1),
            Location = "Test Location",
            Category = null,
            MaxParticipantsCount = 5,
            ImageFileId = null,
        };
        
        dbContext.Events.Add(@event);
        await dbContext.SaveChangesAsync();
        
        var fullEventDto = await eventsService.GetEventById(@event.Id);
        
        Assert.Equal(@event.Id, fullEventDto.Id);
        Assert.Equal(@event.Title, fullEventDto.Title);
        Assert.Equal(@event.Description, fullEventDto.Description);
        Assert.Equal(@event.StartAt, fullEventDto.StartAt);
        Assert.Equal(@event.EndAt, fullEventDto.EndAt);
        Assert.Equal(@event.Location, fullEventDto.Location);
        Assert.Equal(@event.Category, fullEventDto.Category);
        Assert.Equal(@event.MaxParticipantsCount, fullEventDto.MaxParticipantsCount);
        Assert.Equal(0, fullEventDto.CurrentParticipantsCount);
        Assert.Equal(@event.ImageFileId, fullEventDto.ImageFileId);
    }

    [Fact]
    public async Task CreateEvent_ShouldCreateEventSuccessfully()
    {
        var createEventDto = new CreateEventDto()
        {
            Title = "Test Event Title",
            Description = "Test Event Description",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddDays(1),
            Location = "Test Location",
            Category = null,
            MaxParticipantsCount = 5,
            ImageFileId = null
        };
        
        var createdEventDto = await eventsService.CreateEvent(createEventDto);
        
        var persistedEvent = await dbContext.Events
            .Include(@event => @event.EventRegistrations)
            .SingleAsync(@event => @event.Id == createdEventDto.Id);
        
        Assert.Equal(createEventDto.Title, createdEventDto.Title);
        Assert.Equal(createEventDto.Description, createdEventDto.Description);
        Assert.Equal(createEventDto.StartAt, createdEventDto.StartAt);
        Assert.Equal(createEventDto.EndAt, createdEventDto.EndAt);
        Assert.Equal(createEventDto.Location, createdEventDto.Location);
        Assert.Equal(createEventDto.Category, createdEventDto.Category);
        Assert.Equal(createEventDto.MaxParticipantsCount, createdEventDto.MaxParticipantsCount);
        Assert.Equal(0, createdEventDto.CurrentParticipantsCount);
        Assert.Equal(createEventDto.ImageFileId, createdEventDto.ImageFileId);
        
        Assert.Equal(createdEventDto.Id, persistedEvent.Id);
        Assert.Equal(createdEventDto.Title, persistedEvent.Title);
        Assert.Equal(createdEventDto.Description, persistedEvent.Description);
        Assert.Equal(createdEventDto.StartAt, persistedEvent.StartAt);
        Assert.Equal(createdEventDto.EndAt, persistedEvent.EndAt);
        Assert.Equal(createdEventDto.Location, persistedEvent.Location);
        Assert.Equal(createdEventDto.Category, persistedEvent.Category);
        Assert.Equal(createdEventDto.MaxParticipantsCount, persistedEvent.MaxParticipantsCount);
        Assert.Empty(persistedEvent.EventRegistrations);
        Assert.Equal(createdEventDto.ImageFileId, persistedEvent.ImageFileId);
    }

    [Fact]
    public async Task EditEvent_WhenEventExists_ShouldUpdateEventSuccessfully()
    {
        var @event = new Event()
        {
            Id = Guid.NewGuid(),
            Title = "Event Title",
            Description = "Event Description",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddDays(1),
            Location = "Location",
            Category = null,
            MaxParticipantsCount = 5,
            ImageFileId = null
        };
        
        dbContext.Events.Add(@event);
        await dbContext.SaveChangesAsync();

        var updateEventDto = new UpdateEventDto()
        {
            Title = "New Event Title",
            Description = "New Event Description",
            StartAt = DateTime.Now.AddDays(1),
            EndAt = DateTime.Now.AddDays(2),
            Location = "New Location",
            Category = "New Category",
            MaxParticipantsCount = 6,
            ImageFileId = null
        };
        
        await eventsService.EditEvent(@event.Id, updateEventDto);
        
        var updatedEvent = await dbContext.Events.SingleOrDefaultAsync(updatedEvent => updatedEvent.Id == @event.Id);
        
        Assert.NotNull(updatedEvent);
        
        Assert.Equal(updateEventDto.Title, updatedEvent.Title);
        Assert.Equal(updateEventDto.Description, updatedEvent.Description);
        Assert.Equal(updateEventDto.StartAt, updatedEvent.StartAt);
        Assert.Equal(updateEventDto.EndAt, updatedEvent.EndAt);
        Assert.Equal(updateEventDto.Location, updatedEvent.Location);
        Assert.Equal(updateEventDto.Category, updatedEvent.Category);
        Assert.Equal(updateEventDto.MaxParticipantsCount, updatedEvent.MaxParticipantsCount);
        Assert.Equal(updateEventDto.ImageFileId, updatedEvent.ImageFileId);
    }
    
    [Fact]
    public async Task EditEvent_WhenEventDoesNotExist_ShouldThrowNotFoundException()
    {
        var updateEventDto = new UpdateEventDto()
        {
            Title = "New Event Title",
            Description = "New Event Description",
            StartAt = DateTime.Now.AddDays(1),
            EndAt = DateTime.Now.AddDays(2),
            Location = "New Location",
            Category = "New Category",
            MaxParticipantsCount = 6,
            ImageFileId = null
        };

        var notExistingEventId = Guid.NewGuid();
        
        var act = async () => await eventsService.EditEvent(notExistingEventId, updateEventDto);
        await Assert.ThrowsAsync<ResourceNotFoundException>(act);
    }
}