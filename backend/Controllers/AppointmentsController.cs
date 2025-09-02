using LogopedicBackend.Constants;
using LogopedicBackend.Dtos;
using LogopedicBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogopedicBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Therapist)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[Produces("application/json")]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<AppointmentDto>>> GetAll([FromQuery] AppointmentQueryParameters query,
        CancellationToken ct)
    {
        var result = await appointmentService.QueryAsync(query, ct);

        return result.Match<ActionResult<PagedResultDto<AppointmentDto>>>(
            pagedResult => Ok(pagedResult),
            invalidRange => Problem(
                $"The 'from' value ({invalidRange.From:u}) must be less than or equal to the 'to' value ({invalidRange.To:u}).",
                HttpContext.Request.Path,
                StatusCodes.Status400BadRequest,
                "Invalid date range"),
            pageSizeError => ValidationProblem(
                new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["pageSize"] =
                    [
                        $"Requested page size {pageSizeError.Requested} is not in the required range: [{pageSizeError.Min}. {pageSizeError.Max}"
                    ]
                })
                {
                    Title = "Invalid page size",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                })
        );
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(int id, [FromBody] UpdateAppointmentDto dto, CancellationToken ct)
    {
        var result = await appointmentService.UpdateAsync(id, dto, ct);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound($"Appointment id {id} not found"),
            _ => NotFound($"Patient id {dto.PatientId} not found or not associated with current therapist"));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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