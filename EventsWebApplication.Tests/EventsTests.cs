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

    [Fact]
    public async Task GetEvents_WhenNoEventsExist_ShouldReturnEmpty()
    {
        var eventsPage = await eventsService.GetEvents(
            title: null,
            location: null,
            category: null,
            date: null,
            pageNumber: 1,
            pageSize: 1
        );
        
        Assert.Equal([], eventsPage.Items);
        Assert.Equal(0, eventsPage.TotalItemsCount);
        Assert.Equal(1, eventsPage.PageSize);
        Assert.Equal(0, eventsPage.PagesCount);
    }

    [Fact]
    public async Task GetEvents_WhenManyEventsExist_ShouldReturnPaginatedEvents()
    {
        var events = new List<Event>()
        { 
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 1",
                Description = "Event Description 1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(1),
                Location = "Location 1",
                Category = "Category 1",
                MaxParticipantsCount = 1,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 2",
                Description = "Event Description 2",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2),
                Location = "Location 2",
                Category = "Category 2",
                MaxParticipantsCount = 2,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 3",
                Description = "Event Description 3",
                StartAt = DateTime.Now.AddDays(2),
                EndAt = DateTime.Now.AddDays(3),
                Location = "Location 3",
                Category = "Category 3",
                MaxParticipantsCount = 3,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 4",
                Description = "Event Description 4",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(4),
                Location = "Location 4",
                Category = "Category 4",
                MaxParticipantsCount = 4,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 5",
                Description = "Event Description 5",
                StartAt = DateTime.Now.AddDays(4),
                EndAt = DateTime.Now.AddDays(5),
                Location = "Location 5",
                Category = "Category 5",
                MaxParticipantsCount = 5,
                ImageFileId = null
            }
        };
        
        dbContext.Events.AddRange(events);
        await dbContext.SaveChangesAsync();

        var paginatedEvents = await eventsService.GetEvents(
            title: null,
            location: null,
            category: null,
            date: null,
            pageNumber: 2,
            pageSize: 2
        );

        var thirdEvent = events[2];
        var thirdEventDto = new ShortEventDto()
        {
            Id = thirdEvent.Id,
            Title = thirdEvent.Title,
            StartAt = thirdEvent.StartAt,
            EndAt = thirdEvent.EndAt,
            Location = thirdEvent.Location,
            Category = thirdEvent.Category,
            MaxParticipantsCount = thirdEvent.MaxParticipantsCount,
            CurrentParticipantsCount = 0,
            ImageFileId = thirdEvent.ImageFileId
        };
        
        var fourthEvent = events[3];
        var fourthEventDto = new ShortEventDto
        {
            Id = fourthEvent.Id,
            Title = fourthEvent.Title,
            StartAt = fourthEvent.StartAt,
            EndAt = fourthEvent.EndAt,
            Location = fourthEvent.Location,
            Category = fourthEvent.Category,
            MaxParticipantsCount = fourthEvent.MaxParticipantsCount,
            CurrentParticipantsCount = 0,
            ImageFileId = fourthEvent.ImageFileId
        };

        var expectedPaginatedEventDtos = new List<ShortEventDto>
        {
            thirdEventDto,
            fourthEventDto
        };
        
        Assert.Equal(expectedPaginatedEventDtos.Count, paginatedEvents.Items.Count);
        Assert.Equivalent(expectedPaginatedEventDtos, paginatedEvents.Items);
        Assert.Equal(5, paginatedEvents.TotalItemsCount);
        Assert.Equal(2, paginatedEvents.PageSize);
        Assert.Equal(3, paginatedEvents.PagesCount);
    }

    [Fact]
    public async Task GetEvents_WhenTitleFilterSpecified_ShouldReturnFilteredByTitle()
    {
         var events = new List<Event>()
        {
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 1",
                Description = "Event Description 1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(1),
                Location = "Location 1",
                Category = "Category 1",
                MaxParticipantsCount = 1,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 2",
                Description = "Event Description 2",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2),
                Location = "Location 2",
                Category = "Category 2",
                MaxParticipantsCount = 2,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 3",
                Description = "Event Description 3",
                StartAt = DateTime.Now.AddDays(2),
                EndAt = DateTime.Now.AddDays(3),
                Location = "Location 3",
                Category = "Category 3",
                MaxParticipantsCount = 3,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 4",
                Description = "Event Description 4",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(4),
                Location = "Location 4",
                Category = "Category 4",
                MaxParticipantsCount = 4,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 5",
                Description = "Event Description 5",
                StartAt = DateTime.Now.AddDays(4),
                EndAt = DateTime.Now.AddDays(5),
                Location = "Location 5",
                Category = "Category 5",
                MaxParticipantsCount = 5,
                ImageFileId = null
            }
        };
         
         dbContext.Events.AddRange(events);
         await dbContext.SaveChangesAsync();

         var paginatedEvents = await eventsService.GetEvents(
             title: "3",
             location: null,
             category: null,
             date: null,
             pageNumber: 1,
             pageSize: 2
         );
         
         var thirdEvent = events[2];

         var thirdEventDto = new ShortEventDto()
         {
             Id = thirdEvent.Id,
             Title = thirdEvent.Title,
             StartAt = thirdEvent.StartAt,
             EndAt = thirdEvent.EndAt,
             Location = thirdEvent.Location,
             Category = thirdEvent.Category,
             MaxParticipantsCount = thirdEvent.MaxParticipantsCount,
             CurrentParticipantsCount = 0,
             ImageFileId = thirdEvent.ImageFileId
         };

         var expectedFilteredEventDtos = new List<ShortEventDto> { thirdEventDto };
         
         Assert.Equal(expectedFilteredEventDtos.Count, paginatedEvents.Items.Count);
         Assert.Equivalent(expectedFilteredEventDtos, paginatedEvents.Items);
         Assert.Equal(1, paginatedEvents.TotalItemsCount);
         Assert.Equal(2, paginatedEvents.PageSize);
         Assert.Equal(1, paginatedEvents.PagesCount);
    }
    
    [Fact]
    public async Task GetEvents_WhenLocationFilterSpecified_ShouldReturnFilteredByLocation()
    {
         var events = new List<Event>()
        {
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 1",
                Description = "Event Description 1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(1),
                Location = "Location 1",
                Category = "Category 1",
                MaxParticipantsCount = 1,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 2",
                Description = "Event Description 2",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2),
                Location = "Location 2",
                Category = "Category 2",
                MaxParticipantsCount = 2,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 3",
                Description = "Event Description 3",
                StartAt = DateTime.Now.AddDays(2),
                EndAt = DateTime.Now.AddDays(3),
                Location = "Location 3",
                Category = "Category 3",
                MaxParticipantsCount = 3,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 4",
                Description = "Event Description 4",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(4),
                Location = "Location 4",
                Category = "Category 4",
                MaxParticipantsCount = 4,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 5",
                Description = "Event Description 5",
                StartAt = DateTime.Now.AddDays(4),
                EndAt = DateTime.Now.AddDays(5),
                Location = "Location 5",
                Category = "Category 5",
                MaxParticipantsCount = 5,
                ImageFileId = null
            }
        };
         
         dbContext.Events.AddRange(events);
         await dbContext.SaveChangesAsync();

         var paginatedEvents = await eventsService.GetEvents(
             title: null,
             location: "3",
             category: null,
             date: null,
             pageNumber: 1,
             pageSize: 2
         );
         
         var thirdEvent = events[2];

         var thirdEventDto = new ShortEventDto()
         {
             Id = thirdEvent.Id,
             Title = thirdEvent.Title,
             StartAt = thirdEvent.StartAt,
             EndAt = thirdEvent.EndAt,
             Location = thirdEvent.Location,
             Category = thirdEvent.Category,
             MaxParticipantsCount = thirdEvent.MaxParticipantsCount,
             CurrentParticipantsCount = 0,
             ImageFileId = thirdEvent.ImageFileId
         };

         var expectedFilteredEventDtos = new List<ShortEventDto> { thirdEventDto };
         
         Assert.Equal(expectedFilteredEventDtos.Count, paginatedEvents.Items.Count);
         Assert.Equivalent(expectedFilteredEventDtos, paginatedEvents.Items);
         Assert.Equal(1, paginatedEvents.TotalItemsCount);
         Assert.Equal(2, paginatedEvents.PageSize);
         Assert.Equal(1, paginatedEvents.PagesCount);
    }
    
    [Fact]
    public async Task GetEvents_WhenCategoryFilterSpecified_ShouldReturnFilteredByCategory()
    {
         var events = new List<Event>()
        {
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 1",
                Description = "Event Description 1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(1),
                Location = "Location 1",
                Category = "Category 1",
                MaxParticipantsCount = 1,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 2",
                Description = "Event Description 2",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2),
                Location = "Location 2",
                Category = "Category 2",
                MaxParticipantsCount = 2,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 3",
                Description = "Event Description 3",
                StartAt = DateTime.Now.AddDays(2),
                EndAt = DateTime.Now.AddDays(3),
                Location = "Location 3",
                Category = "Category 3",
                MaxParticipantsCount = 3,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 4",
                Description = "Event Description 4",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(4),
                Location = "Location 4",
                Category = "Category 4",
                MaxParticipantsCount = 4,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 5",
                Description = "Event Description 5",
                StartAt = DateTime.Now.AddDays(4),
                EndAt = DateTime.Now.AddDays(5),
                Location = "Location 5",
                Category = "Category 5",
                MaxParticipantsCount = 5,
                ImageFileId = null
            }
        };
         
         dbContext.Events.AddRange(events);
         await dbContext.SaveChangesAsync();

         var paginatedEvents = await eventsService.GetEvents(
             title: null,
             location: null,
             category: "3",
             date: null,
             pageNumber: 1,
             pageSize: 2
         );
         
         var thirdEvent = events[2];

         var thirdEventDto = new ShortEventDto()
         {
             Id = thirdEvent.Id,
             Title = thirdEvent.Title,
             StartAt = thirdEvent.StartAt,
             EndAt = thirdEvent.EndAt,
             Location = thirdEvent.Location,
             Category = thirdEvent.Category,
             MaxParticipantsCount = thirdEvent.MaxParticipantsCount,
             CurrentParticipantsCount = 0,
             ImageFileId = thirdEvent.ImageFileId
         };

         var expectedFilteredEventDtos = new List<ShortEventDto> { thirdEventDto };
         
         Assert.Equal(expectedFilteredEventDtos.Count, paginatedEvents.Items.Count);
         Assert.Equivalent(expectedFilteredEventDtos, paginatedEvents.Items);
         Assert.Equal(1, paginatedEvents.TotalItemsCount);
         Assert.Equal(2, paginatedEvents.PageSize);
         Assert.Equal(1, paginatedEvents.PagesCount);
    }
    
    [Fact]
    public async Task GetEvents_WhenDateFilterSpecified_ShouldReturnFilteredByDate()
    {
         var events = new List<Event>()
        {
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 1",
                Description = "Event Description 1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(1),
                Location = "Location 1",
                Category = "Category 1",
                MaxParticipantsCount = 1,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 2",
                Description = "Event Description 2",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2),
                Location = "Location 2",
                Category = "Category 2",
                MaxParticipantsCount = 2,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 3",
                Description = "Event Description 3",
                StartAt = DateTime.Now.AddDays(2),
                EndAt = DateTime.Now.AddDays(3),
                Location = "Location 3",
                Category = "Category 3",
                MaxParticipantsCount = 3,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 4",
                Description = "Event Description 4",
                StartAt = DateTime.Now.AddDays(3),
                EndAt = DateTime.Now.AddDays(4),
                Location = "Location 4",
                Category = "Category 4",
                MaxParticipantsCount = 4,
                ImageFileId = null
            },
            new Event()
            {
                Id = Guid.NewGuid(),
                Title = "Event Title 5",
                Description = "Event Description 5",
                StartAt = DateTime.Now.AddDays(4),
                EndAt = DateTime.Now.AddDays(5),
                Location = "Location 5",
                Category = "Category 5",
                MaxParticipantsCount = 5,
                ImageFileId = null
            }
        };
         
         dbContext.Events.AddRange(events);
         await dbContext.SaveChangesAsync();

         var paginatedEvents = await eventsService.GetEvents(
             title: null,
             location: null,
             category: null,
             date: DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
             pageNumber: 1,
             pageSize: 2
         );
         
         var secondEvent = events[1];

         var secondEventDto = new ShortEventDto()
         {
             Id = secondEvent.Id,
             Title = secondEvent.Title,
             StartAt = secondEvent.StartAt,
             EndAt = secondEvent.EndAt,
             Location = secondEvent.Location,
             Category = secondEvent.Category,
             MaxParticipantsCount = secondEvent.MaxParticipantsCount,
             CurrentParticipantsCount = 0,
             ImageFileId = secondEvent.ImageFileId
         };
         
         var thirdEvent = events[2];

         var thirdEventDto = new ShortEventDto()
         {
             Id = thirdEvent.Id,
             Title = thirdEvent.Title,
             StartAt = thirdEvent.StartAt,
             EndAt = thirdEvent.EndAt,
             Location = thirdEvent.Location,
             Category = thirdEvent.Category,
             MaxParticipantsCount = thirdEvent.MaxParticipantsCount,
             CurrentParticipantsCount = 0,
             ImageFileId = thirdEvent.ImageFileId
         };

         var expectedFilteredEventDtos = new List<ShortEventDto>
         {
             secondEventDto,
             thirdEventDto
         };
         
         Assert.Equal(expectedFilteredEventDtos.Count, paginatedEvents.Items.Count);
         Assert.Equivalent(expectedFilteredEventDtos, paginatedEvents.Items);
         Assert.Equal(2, paginatedEvents.TotalItemsCount);
         Assert.Equal(2, paginatedEvents.PageSize);
         Assert.Equal(1, paginatedEvents.PagesCount);
    }
}