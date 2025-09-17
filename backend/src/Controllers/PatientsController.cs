using LogopedicBackend.Constants;
using LogopedicBackend.Dtos;
using LogopedicBackend.Services;
using LogopedicBackend.Services.Results.Common.NotFound;
using LogopedicBackend.Services.Results.Common.Paging;
using LogopedicBackend.Services.Results.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;

namespace LogopedicBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Therapist)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
[Produces("application/json")]
public class PatientsController(IPatientService patientService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<PatientDto>>> Query([FromQuery] PatientQueryParameters query,
        CancellationToken ct)
    {
        OneOf<PagedResultDto<PatientDto>, InvalidPageSizeError> result = await patientService.QueryAsync(query, ct);

        return result.Match<ActionResult<PagedResultDto<PatientDto>>>(pagedResult => Ok(pagedResult),
            pageSizeError => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
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
            }));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> GetById(int id, CancellationToken ct)
    {
        PatientDto? patient = await patientService.GetPatientDtoByIdAsync(id, ct);

        if (patient is null)
        {
            return Problem($"Patient with id {id} does not exist or does not belong to the current user",
                HttpContext.Request.Path,
                StatusCodes.Status404NotFound,
                "Patient not found");
        }

        return Ok(patient);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientDto dto, CancellationToken ct)
    {
        OneOf<PatientCreated, EmptyFullNameError, InvalidDateOfBirthError> result =
            await patientService.CreateAsync(dto, ct);
        return result.Match<ActionResult<PatientDto>>(
            created => CreatedAtAction(nameof(GetById), new { id = created.Patient.Id }, created.Patient),
            emptyFullNameError => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["fullName"] =
                [
                    "Requested full name is empty"
                ]
            })
            {
                Title = "Invalid date of birth",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            }),
            invalidDateOfBirthError => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["dateOfBirth"] =
                [
                    $"Requested date of birth {invalidDateOfBirthError.Requested} is not in the required range: [{invalidDateOfBirthError.Min}, {invalidDateOfBirthError.Max}]"
                ]
            })
            {
                Title = "Invalid date of birth",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            }));
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdatePatientDto dto, CancellationToken ct)
    {
        OneOf<PatientUpdated, PatientNotFound> result = await patientService.UpdateAsync(id, dto, ct);

        return result.Match<IActionResult>(_ => NoContent(),
            patientNotFound => Problem(
                $"Patient with id {patientNotFound.PatientId} does not exist or does not belong to the current user",
                HttpContext.Request.Path,
                StatusCodes.Status404NotFound,
                "Patient not found"));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        bool deleted = await patientService.DeleteAsync(id, ct);

        if (!deleted)
        {
            return Problem($"Patient with id {id} does not exist or does not belong to the current user",
                HttpContext.Request.Path,
                StatusCodes.Status404NotFound,
                "Patient not found");
        }

        return NoContent();
    }
}
