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
}