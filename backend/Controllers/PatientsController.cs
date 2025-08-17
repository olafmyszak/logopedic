using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController(LogopedicContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetAllPatients()
    {
        var patients = await context.Patients.ToListAsync();

        if (patients.Count == 0)
        {
            return NotFound();
        }

        var result = patients.Select(p => new PatientDto
        {
            Id = p.Id,
            FullName = p.FullName,
            DateOfBirth = p.DateOfBirth,
            ContactInfo = p.ContactInfo,
            Notes = p.Notes,
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PatientDto>> GetPatientById(int id)
    {
        var patient = await context.Patients.FindAsync(id);

        if (patient is null)
        {
            return NotFound();
        }

        var result = new PatientDto
        {
            Id = patient.Id,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            ContactInfo = patient.ContactInfo,
            Notes = patient.Notes
        };

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PatientDto>> CreatePatient(CreatePatientDto dto)
    {
        var patient = new Patient
        {
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            ContactInfo = dto.ContactInfo,
            Notes = dto.Notes,
            TherapistId = dto.TherapistId
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var result = new PatientDto
        {
            Id = patient.Id,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            ContactInfo = patient.ContactInfo,
            Notes = patient.Notes
        };

        return CreatedAtAction(nameof(GetPatientById), new { id = patient.Id }, result);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdatePatient(int id, UpdatePatientDto dto)
    {
        var patient = await context.Patients.FindAsync(id);

        if (patient is null)
        {
            return NotFound();
        }

        if (dto.FullName is not null)
        {
            patient.FullName = dto.FullName;
        }

        if (dto.DateOfBirth.HasValue)
        {
            patient.DateOfBirth = dto.DateOfBirth.Value;
        }

        if (dto.ContactInfo is not null)
        {
            patient.ContactInfo = dto.ContactInfo;
        }

        if (dto.Notes is not null)
        {
            patient.Notes = dto.Notes;
        }

        if (dto.TherapistId is not null)
        {
            var therapistExists = await context.Therapists.AnyAsync(t => t.Id == dto.TherapistId);

            if (!therapistExists)
            {
                return BadRequest($"Therapist does not exist");
            }

            patient.TherapistId = dto.TherapistId.Value;
        }

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePatient(int id)
    {
        var patient = await context.Patients.FindAsync(id);

        if (patient is null)
        {
            return NotFound();
        }

        context.Patients.Remove(patient);
        await context.SaveChangesAsync();

        return NoContent();
    }
}