using System.Security.Claims;
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
    public async Task<IActionResult> Register(RegisterUserDto registerUserDto)
    {
        var user = mapper.Map<ApplicationUser>(registerUserDto);

        var result = await userManager.CreateAsync(user, registerUserDto.Password);
        if (result.Succeeded)
        {
            return Ok();
        }

        return BadRequest(result);
    }
    
    [HttpPost("/admins/register")]
    public async Task<IActionResult> RegisterAdmin(RegisterUserDto registerUserDto)
    {
        var admin = mapper.Map<ApplicationUser>(registerUserDto);
        
        var result = await userManager.CreateAsync(admin, registerUserDto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }
        
        await userManager.AddClaimAsync(admin, new Claim("CanManageEvents", "true"));

        return Ok();
    }
}