using LogopedicBackend.Data;
using LogopedicBackend.Enums;
using LogopedicBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace Logopedic.UnitTests;

public class TestDataBuilder
{
    private readonly DbContextOptions<LogopedicContext> _options;
    private Appointment? _appointment;
    private Patient? _patient;
    private Therapist? _therapist;
    private User? _user;

    public TestDataBuilder()
    {
        _options = new DbContextOptionsBuilder<LogopedicContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
            .Options;
    }

    public TestDataBuilder WithUser(User? user = null)
    {
        _user = user ?? new User
        {
            Id = Guid.NewGuid().ToString()
        };

        return this;
    }

    public TestDataBuilder WithTherapist(Therapist? therapist = null)
    {
        if (_user is null)
        {
            WithUser();
        }

        _therapist = therapist ?? new Therapist
        {
            Id = new Random().Next(1, 10000),
            FullName = "Mark Smith",
            UserId = _user!.Id,
            User = _user
        };

        return this;
    }

    public TestDataBuilder WithPatient(Patient? patient = null)
    {
        if (_therapist is null)
        {
            WithTherapist();
        }

        _patient = patient ?? new Patient
        {
            Id = new Random().Next(1, 10000),
            FullName = "John Doe",
            DateOfBirth = new DateOnly(2010, 1, 1),
            ContactInfo = "john@example.com",
            TherapistId = _therapist!.Id,
            Therapist = _therapist
        };

        return this;
    }

    public TestDataBuilder WithAppointment(Appointment? appointment = null)
    {
        if (_therapist is null)
        {
            WithTherapist();
        }

        if (_patient is null)
        {
            WithPatient();
        }

        _appointment = appointment ?? new Appointment
        {
            Id = new Random().Next(1, 10000),
            StartTime = DateTimeOffset.UtcNow,
            DurationInMinutes = 45,
            Type = AppointmentType.Diagnosis,
            Status = AppointmentStatus.Scheduled,
            TherapistId = _therapist!.Id,
            Therapist = _therapist,
            PatientId = _patient!.Id,
            Patient = _patient
        };

        return this;
    }

    public async Task<( LogopedicContext Context, User? User, Therapist? Therapist, Patient? Patient, Appointment?
        Appointment )> BuildAsync()
    {
        var ctx = new LogopedicContext(_options);

        if (_user != null)
        {
            ctx.Users.Add(_user);
        }

        if (_therapist != null)
        {
            ctx.Therapists.Add(_therapist);
        }

        if (_patient != null)
        {
            ctx.Patients.Add(_patient);
        }

        if (_appointment != null)
        {
            ctx.Appointments.Add(_appointment);
        }

        await ctx.SaveChangesAsync();
        return (ctx, _user, _therapist, _patient, _appointment);
    }
}