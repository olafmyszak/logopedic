using LogopedicBackend.Constants;
using LogopedicBackend.Dtos;
using LogopedicBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogopedicBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Therapist)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[Produces("application/json")]
public class PatientsController(IPatientService patientService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetAllPatients(CancellationToken ct)
    {
        var result = await patientService.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> GetPatientById(int id, CancellationToken ct)
    {
        var patient = await patientService.GetPatientDtoByIdAsync(id, ct);

        if (patient is null)
        {
            return NotFound();
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
            _ => NotFound());
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePatient(int id, CancellationToken ct)
    {
        var deleted = await patientService.DeleteAsync(id, ct);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}