using LogopedicBackend.Constants;
using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace LogopedicBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccountController(
    LogopedicContext context,
    UserManager<User> userManager,
    SignInManager<User> signInManager) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterAsync(RegisterDto dto)
    {
        User user = new() { UserName = dto.Email, Email = dto.Email, EmailConfirmed = false };

        IdentityResult create = await userManager.CreateAsync(user, dto.Password);
        if (!create.Succeeded)
        {
            return BadRequest(create.Errors);
        }

        await userManager.AddToRoleAsync(user, AppRoles.Therapist);

        Therapist therapist = new() { FullName = dto.FullName, UserId = user.Id, User = user };

        context.Therapists.Add(therapist);
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync(LoginDto dto)
    {
        User? user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        SignInResult result = await signInManager.PasswordSignInAsync(user, dto.Password, dto.IsPersistent, false);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        return Ok();
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LogoutAsync()
    {
        await signInManager.SignOutAsync();

        return Ok();
    }
}
