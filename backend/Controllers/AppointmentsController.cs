using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using LogopedicBackend.Models;
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
            Type = a.Type,
            Status = a.Status
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointmentById(int id)
    {
        var appointment = await context.Appointments.FindAsync(id);

        if (appointment is null)
        {
            return NotFound();
        }

        var result = new AppointmentDto
        {
            Id = appointment.Id,
            StartTime = appointment.StartTime,
            DurationInMinutes = appointment.DurationInMinutes,
            Type = appointment.Type,
            Status = appointment.Status
        };

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment(CreateAppointmentDto dto)
    {
        // TODO: therapist and patient id validation
        var appointment = new Appointment
        {
            StartTime = dto.StartTime,
            DurationInMinutes = dto.DurationInMinutes,
            Type = dto.Type,
            Status = dto.Status,
            TherapistId = dto.TherapistId,
            PatientId = dto.PatientId
        };

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var result = new AppointmentDto
        {
            Id = appointment.Id,
            StartTime = appointment.StartTime,
            DurationInMinutes = appointment.DurationInMinutes,
            Type = appointment.Type,
            Status = appointment.Status
        };

        return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.Id }, result);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateAppointment(int id, UpdateAppointmentDto dto)
    {
        var appointment = await context.Appointments.FindAsync(id);

        if (appointment is null)
        {
            return NotFound();
        }

        if (dto.StartTime is not null)
        {
            appointment.StartTime = dto.StartTime.Value;
        }

        if (dto.DurationInMinutes is not null)
        {
            appointment.DurationInMinutes = dto.DurationInMinutes.Value;
        }

        if (dto.Type is not null)
        {
            appointment.Type = dto.Type.Value;
        }

        if (dto.Status is not null)
        {
            appointment.Status = dto.Status.Value;
        }

        // TODO: patient id validation
        if (dto.PatientId is not null)
        {
            appointment.PatientId = dto.PatientId.Value;
        }

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var appointment = await context.Appointments.FindAsync(id);

        if (appointment is null)
        {
            return NotFound();
        }

        context.Appointments.Remove(appointment);
        await context.SaveChangesAsync();

        return NoContent();
    }
}