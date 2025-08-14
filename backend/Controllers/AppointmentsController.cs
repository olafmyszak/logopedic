using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(LogopedicContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAllAppointments()
    {
        var appointments = await context.Appointments
            .Include(a => a.Patient)
            .ToListAsync();

        if (appointments.Count == 0)
        {
            return NotFound("No appointments found");
        }

        var result = appointments.Select(a => new AppointmentDto
        {
            Id = a.Id,
            StartTime = a.StartTime,
            DurationInMinutes = a.DurationInMinutes,
            Type = a.Type.ToString(),
            Status = a.Status.ToString()
        }).ToList();

        return Ok(result);
    }
}