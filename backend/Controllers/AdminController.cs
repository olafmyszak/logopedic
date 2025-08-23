using LogopedicBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogopedicBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController(AdminService adminService) : ControllerBase
{
    [HttpDelete("user/{email}")]
    // [Authorize(Roles = "Admin")] // 🔒 only admins
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