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
public class PatientsController(IPatientService patientService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<PatientDto>>> GetAll([FromQuery] PatientQueryParameters query,
        CancellationToken ct)
    {
        var result = await patientService.QueryAsync(query, ct);

        return result.Match<ActionResult<PagedResultDto<PatientDto>>>(
            pagedResult => Ok(pagedResult),
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
    public async Task<ActionResult<PatientDto>> GetPatientById(int id, CancellationToken ct)
    {
        var patient = await patientService.GetPatientDtoByIdAsync(id, ct);

        if (patient is null)
        {
            return Problem(
                $"Patient with id {id} does not exist or does not belong to the current user",
                HttpContext.Request.Path,
                StatusCodes.Status404NotFound,
                "Patient not found"
            );
        }

        return Ok(patient);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<PatientDto>> CreatePatient(CreatePatientDto dto, CancellationToken ct)
    {
        var result = await patientService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetPatientById), new { id = result.Patient.Id }, result.Patient);
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePatient(int id, UpdatePatientDto dto, CancellationToken ct)
    {
        var result = await patientService.UpdateAsync(id, dto, ct);

        return result.Match<IActionResult>(
            _ => NoContent(),
            patientNotFound => Problem(
                $"Patient with id {patientNotFound.PatientId} does not exist or does not belong to the current user",
                HttpContext.Request.Path,
                StatusCodes.Status404NotFound,
                "Patient not found"
            ));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePatient(int id, CancellationToken ct)
    {
        var deleted = await patientService.DeleteAsync(id, ct);

        if (!deleted)
        {
            return Problem(
                $"Patient with id {id} does not exist or does not belong to the current user",
                HttpContext.Request.Path,
                StatusCodes.Status404NotFound,
                "Patient not found"
            );
        }

        return NoContent();
    }
}