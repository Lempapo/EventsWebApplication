using EventsWebApplication;
using EventsWebApplication.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

builder.Services.AddSingleton<CreateEventDtoValidator>();
builder.Services.AddSingleton<UpdateEventDtoValidator>();

builder.Services.AddDbContext<EventsDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddAutoMapper(typeof(Program).Assembly);

builder.Services.AddAuthorization();

builder.Services
    .AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<EventsDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapIdentityApi<IdentityUser>();

app.MapControllers();

app.Run();
