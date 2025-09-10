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
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<AppointmentDto>>> Query([FromQuery] AppointmentQueryParameters query,
        CancellationToken ct)
    {
        var result = await appointmentService.QueryAsync(query, ct);

        return result.Match<ActionResult<PagedResultDto<AppointmentDto>>>(
            pagedResult => Ok(pagedResult),
            invalidDateRangeError => ValidationProblem(
                new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["dateRange"] =
                    [
                        $"The 'from' value ({invalidDateRangeError.From:u}) must be less than or equal to the 'to' value ({invalidDateRangeError.To:u})."
                    ]
                })
                {
                    Title = "Invalid date range",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                }),
            pageSizeError => ValidationProblem(
                new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["pageSize"] =
                    [
                        $"Requested page size {pageSizeError.Requested} is not in the required range: [{pageSizeError.Min}, {pageSizeError.Max}]"
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
            return Problem(
                $"Appointment with id {id} does not exist or does not belong to the current user",
                HttpContext.Request.Path,
                StatusCodes.Status404NotFound,
                "Appointment not found"
            );
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
            created => CreatedAtAction(nameof(GetById), new { id = created.AppointmentDto.Id }, created.AppointmentDto),
            patientNotFound => Problem(
                $"Patient with id {patientNotFound.PatientId} does not exist or does not belong to the current user",
                HttpContext.Request.Path,
                StatusCodes.Status404NotFound,
                "Patient not found"
            ),
            timeConflict => Problem(
                $"The requested time {timeConflict.RequestedStart:t}–{timeConflict.RequestedEnd:t} conflicts with {timeConflict.Conflicts.Count} existing appointment(s).",
                HttpContext.Request.Path,
                StatusCodes.Status409Conflict,
                "Requested time slot conflicts with existing appointments",
                extensions: new Dictionary<string, object?>
                {
                    ["conflicts"] = timeConflict.Conflicts
                }
            ),
            durationLessThanZero => ValidationProblem(
                new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["duration"] =
                    [
                        $"The 'durationInMinutes' value ({durationLessThanZero.Duration}) must be bigger than zero."
                    ]
                })
                {
                    Title = "Duration is zero or less",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                }));
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchAppointmentDto dto, CancellationToken ct)
    {
        var result = await appointmentService.PatchAsync(id, dto, ct);

        return result.Match<IActionResult>(
            _ => NoContent(),
            appointmentNotFound => Problem(
                $"Appointment with id {appointmentNotFound.AppointmentId} does not exist or does not belong to the current user",
                HttpContext.Request.Path,
                StatusCodes.Status404NotFound,
                "Appointment not found"
            ),
            patientNotFound => Problem(
                $"Patient with id {patientNotFound.PatientId} does not exist or does not belong to the current user",
                HttpContext.Request.Path,
                StatusCodes.Status404NotFound,
                "Patient not found"),
            durationLessThanZero => ValidationProblem(
                new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["duration"] =
                    [
                        $"The 'durationInMinutes' value ({durationLessThanZero.Duration}) must be bigger than zero."
                    ]
                })
                {
                    Title = "Duration is zero or less",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                }),
            timeConflict => Problem(
                $"The requested time {timeConflict.RequestedStart:t}–{timeConflict.RequestedEnd:t} conflicts with {timeConflict.Conflicts.Count} existing appointment(s).",
                HttpContext.Request.Path,
                StatusCodes.Status409Conflict,
                "Requested time slot conflicts with existing appointments",
                extensions: new Dictionary<string, object?>
                {
                    ["conflicts"] = timeConflict.Conflicts
                }
            ));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAppointment(int id, CancellationToken ct)
    {
        var deleted = await appointmentService.DeleteAsync(id, ct);

        if (!deleted)
        {
            return Problem(
                $"Appointment with id {id} does not exist or does not belong to the current user",
                HttpContext.Request.Path,
                StatusCodes.Status404NotFound,
                "Appointment not found"
            );
        }

        return NoContent();
    }
}