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

        if (patient == null)
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
}