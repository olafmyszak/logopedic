using LogopedicBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogopedicBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(AdminService adminService) : ControllerBase
{
    [HttpDelete("user/{email}")]
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