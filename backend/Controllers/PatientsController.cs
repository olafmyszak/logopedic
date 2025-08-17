using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
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
            return NotFound("No patients found");
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
}