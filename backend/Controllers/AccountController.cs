using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LogopedicBackend.Controllers;

[ApiController] 
[Route("api/[controller]")]
public class AccountController(LogopedicContext context, UserManager<User> userManager, SignInManager<User> signInManager)
    : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(RegisterDto dto)
    {
        var user = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = false
        };

        var create = await userManager.CreateAsync(user, dto.Password);
        if (!create.Succeeded)
        {
            return BadRequest(create.Errors);
        }

        var therapist = new Therapist
        {
            FullName = dto.FullName,
            UserId = user.Id,
            User = user
        };

        context.Therapists.Add(therapist);
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await signInManager.PasswordSignInAsync(user, dto.Password, dto.IsPersistent, false);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        return Ok();
    }
}