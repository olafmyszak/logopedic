using Bogus;
using LogopedicBackend.Constants;
using LogopedicBackend.Data;
using LogopedicBackend.Enums;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.IntegrationTests;

// Run tests sequentially to avoid problems with seeding
[CollectionDefinition("Database collection", DisableParallelization = true)]
public class TestDataSeeder(LogopedicContext context, UserManager<User> userManager)
{
    private const string Locale = "pl";
    public const string TestUsername = "test-user1";
    public List<Therapist> Therapists { get; } = [];
    public List<Patient> Patients { get; } = [];
    public List<Appointment> Appointments { get; } = [];
    public User TestUser { get; private set; } = null!;

    public async Task SeedAsync(string defaultPassword = "P@ssword2", int patientsPerTherapists = 3,
        int appointmentsPerPatient = 3)
    {
        await SeedUsersAndTherapists(defaultPassword);
        await SeedPatients(patientsPerTherapists);
        await SeedAppointments(appointmentsPerPatient);

        await context.SaveChangesAsync();
    }

    private async Task SeedUsersAndTherapists(string defaultPassword)
    {
        var user1 = await userManager.FindByNameAsync(TestUsername);
        if (user1 == null)
        {
            user1 = new User
            {
                UserName = TestUsername,
                Email = "test@test.net",
                EmailConfirmed = true
            };

            var create = await userManager.CreateAsync(user1, defaultPassword);
            if (!create.Succeeded)
            {
                var errors = string.Join(", ", create.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create test user: {errors}");
            }

            await userManager.SetLockoutEnabledAsync(user1, false);
            await userManager.AddToRoleAsync(user1, AppRoles.Therapist);

            var therapist1 = new Therapist
            {
                FullName = "test user",
                UserId = user1.Id,
                User = user1
            };

            context.Therapists.Add(therapist1);
        }

        const string username2 = "test-user2";
        var user2 = await userManager.FindByNameAsync(username2);
        if (user2 == null)
        {
            user2 = new User
            {
                UserName = username2,
                Email = "test2@test.net",
                EmailConfirmed = true
            };

            var create = await userManager.CreateAsync(user2, defaultPassword);
            if (!create.Succeeded)
            {
                var errors = string.Join(", ", create.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create test user: {errors}");
            }

            await userManager.SetLockoutEnabledAsync(user2, false);
            await userManager.AddToRoleAsync(user2, AppRoles.Therapist);

            var therapist2 = new Therapist
            {
                FullName = "test user2",
                UserId = user2.Id,
                User = user2
            };

            context.Therapists.Add(therapist2);
        }

        await context.SaveChangesAsync();

        Therapists.AddRange(context.Therapists);
        TestUser = user1;
    }

    private async Task SeedPatients(int patientsPerTherapists)
    {
        var patientFaker = new Faker<Patient>(Locale).RuleFor(p => p.FullName, f => f.Name.FullName())
            .RuleFor(p => p.DateOfBirth,
                f => DateOnly.FromDateTime(f.Date.Between(DateTime.Today.AddYears(-25), DateTime.Today.AddYears(-5))))
            .RuleFor(p => p.ContactInfo, f => f.Random.Bool() ? f.Person.Email : f.Phone.PhoneNumber())
            .RuleFor(p => p.Notes, f => f.Random.Bool(0.25f) ? f.Lorem.Sentence() : null);

        foreach (var therapist in Therapists)
        {
            for (var i = 0; i < patientsPerTherapists; ++i)
            {
                var patient = patientFaker.Generate();
                patient.TherapistId = therapist.Id;
                patient.Therapist = therapist;

                Patients.Add(patient);
                // therapist.Patients.Add(patient);
            }
        }

        await context.Patients.AddRangeAsync(Patients);
        await context.SaveChangesAsync();
    }

    private async Task SeedAppointments(int appointmentsPerPatient)
    {
        var faker = new Faker(Locale);

        const int maxAttemptsPerAppointment = 200;

        var durations = new[] { 15, 30, 45, 60, 90 };

        foreach (var patient in Patients)
        {
            for (var i = 0; i < appointmentsPerPatient; ++i)
            {
                var placed = false;
                for (var attempt = 0; attempt < maxAttemptsPerAppointment && !placed; ++attempt)
                {
                    // generate candidate start within -30…+60 days and between 8:00 and 17:00,
                    // with minutes snapped to 0,15,30,45
                    var dayOffset = faker.Random.Int(-30, 60);
                    var hour = faker.Random.Int(8, 17);
                    var minute = faker.Random.ListItem([0, 15, 30, 45]);

                    var candidateStart = DateTimeOffset.UtcNow
                        .AddDays(dayOffset)
                        .AddHours(hour)
                        .AddMinutes(minute);

                    var duration = faker.Random.ListItem(durations);
                    var candidateEnd = candidateStart.AddMinutes(duration);

                    var conflict = await context.Appointments.AnyAsync(a =>
                        a.TherapistId == patient.Therapist.Id && a.StartTime < candidateEnd &&
                        a.StartTime.AddMinutes(a.DurationInMinutes) > candidateStart);

                    if (conflict)
                    {
                        continue;
                    }

                    // create and attach the appointment
                    var appointment = new Appointment
                    {
                        StartTime = candidateStart,
                        DurationInMinutes = duration,
                        Type = faker.PickRandom<AppointmentType>(),
                        Status = faker.PickRandom<AppointmentStatus>(),
                        TherapistId = patient.TherapistId,
                        Therapist = patient.Therapist,
                        PatientId = patient.Id,
                        Patient = patient
                    };

                    // context.Appointments.Add(appointment);
                    // await context.SaveChangesAsync();
                    Appointments.Add(appointment);

                    placed = true;
                }
            }
        }

        context.AddRange(Appointments);
        await context.SaveChangesAsync();
        Appointments.AddRange(context.Appointments);
    }
}