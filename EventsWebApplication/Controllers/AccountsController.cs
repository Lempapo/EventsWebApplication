using AutoMapper;
using EventsWebApplication.Dtos;
using EventsWebApplication.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EventsWebApplication.Controllers;

[ApiController]
public class AccountsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IMapper mapper;

    public AccountsController(
        UserManager<ApplicationUser> userManager, 
        IMapper mapper)
    {
        this.userManager = userManager;
        this.mapper = mapper;
    }

    [HttpPost("/users/register")]
    public async Task<IActionResult> Register(RegisterDto registerUserDto)
    {
        var user = mapper.Map<ApplicationUser>(registerUserDto);

        var result = await userManager.CreateAsync(user, registerUserDto.Password);
        if (result.Succeeded)
        {
            return Ok();
        }

        return BadRequest(result);
    }
}