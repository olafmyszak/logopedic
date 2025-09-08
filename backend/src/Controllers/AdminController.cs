using System.ComponentModel.DataAnnotations;
using LogopedicBackend.Constants;
using LogopedicBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogopedicBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[Produces("application/json")]
public class AdminController(AdminService adminService) : ControllerBase
{
    [HttpDelete("user/{email}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string email)
    {
        var success = await adminService.DeleteUserAndDomainDataAsync(email);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}