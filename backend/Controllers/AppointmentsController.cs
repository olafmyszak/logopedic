using LogopedicBackend.Constants;
using LogopedicBackend.Dtos;
using LogopedicBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogopedicBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Therapist)]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAll(CancellationToken ct)
    {
        var result = await appointmentService.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppointmentDto>> GetById(int id, CancellationToken ct)
    {
        var appointment = await appointmentService.GetByIdAsync(id, ct);

        if (appointment is null)
        {
            return NotFound();
        }

        return Ok(appointment);
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentDto dto, CancellationToken ct)
    {
        var result = await appointmentService.CreateAsync(dto, ct);

        return result.Match<ActionResult<AppointmentDto>>(
            created => CreatedAtAction(nameof(GetById), new { id = created.Appointment.Id }, created.Appointment),
            _ => NotFound($"Patient id {dto.PatientId} not found or not associated with current therapist"),
            _ => Conflict("Requested time slot is already taken")
        );
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Patch(int id, [FromBody] UpdateAppointmentDto dto, CancellationToken ct)
    {
        var result = await appointmentService.UpdateAsync(id, dto, ct);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound($"Appointment id {id} not found"),
            _ => NotFound($"Patient id {dto.PatientId} not found or not associated with current therapist"));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAppointment(int id, CancellationToken ct)
    {
        var deleted = await appointmentService.DeleteAsync(id, ct);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}