using Bogus;
using LogopedicBackend.Enums;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend;

public static class DatabaseSeeder
{
    public static void Seed(DbContext context, bool _)
    {
        if (context.Set<Therapist>()
            .Any(t => t.FullName != "admin"))
        {
            return;
        }

        Console.WriteLine("Seeding...");

        const string locale = "pl";

        const int therapistCount = 100;
        Faker faker = new(locale);

        Faker<User> userFaker = new Faker<User>().RuleFor(u => u.Email, f => f.Internet.Email());

        Faker<Therapist> therapistFaker = new Faker<Therapist>().RuleFor(t => t.FullName, f => f.Person.FullName);

        PasswordHasher<User> passwordHasher = new();

        IdentityRole role = context.Set<IdentityRole>()
            .Single(r => r.NormalizedName == "THERAPIST");

        for (int i = 0; i < therapistCount; ++i)
        {
            User user = userFaker.Generate();

            user.Id = Guid.NewGuid()
                .ToString(); // ensure unique string id if your User.Id is string
            user.UserName = user.Email;
            user.NormalizedUserName = user.Email!.ToUpperInvariant();
            user.NormalizedEmail = user.Email!.ToUpperInvariant();
            user.EmailConfirmed = true; // dev convenience
            user.SecurityStamp = Guid.NewGuid()
                .ToString();
            user.ConcurrencyStamp = Guid.NewGuid()
                .ToString();
            user.PasswordHash = passwordHasher.HashPassword(user, "P@ssword2");

            bool userExists = context.Set<User>()
                .Any(u => u.NormalizedEmail == user.NormalizedEmail);

            if (!userExists)
            {
                context.Set<User>()
                    .Add(user);
            }

            IdentityUserRole<string> userRole = new() { RoleId = role.Id, UserId = user.Id };
            context.Set<IdentityUserRole<string>>()
                .Add(userRole);


            Therapist therapist = therapistFaker.Generate();
            therapist.UserId = user.Id;
            therapist.User = user;

            context.Set<Therapist>()
                .Add(therapist);
        }

        context.SaveChanges();

        Faker<Patient> patientFaker = new Faker<Patient>(locale).RuleFor(p => p.FullName, f => f.Name.FullName())
            .RuleFor(p => p.DateOfBirth,
                f => DateOnly.FromDateTime(f.Date.Between(DateTime.Today.AddYears(-25), DateTime.Today.AddYears(-5))))
            .RuleFor(p => p.ContactInfo, f => f.Random.Bool() ? f.Person.Email : f.Phone.PhoneNumber())
            .RuleFor(p => p.Notes, f => f.Random.Bool(0.75f) ? f.Lorem.Sentence() : null);

        int[] durations = [15, 30, 45, 60, 90];

        List<Therapist> therapists = context.Set<Therapist>()
            .Where(t => t.FullName != "admin")
            .ToList();

        const int patientsPerTherapist = 100;
        const int paddingMinutes = 5;

        foreach (Therapist therapist in therapists)
        {
            DateTimeOffset nextAvailable = DateTimeOffset.UtcNow;
            List<Appointment> toInsert = [];

            for (int i = 0; i < patientsPerTherapist; ++i)
            {
                Patient patient = patientFaker.Generate();
                patient.TherapistId = therapist.Id;
                patient.Therapist = therapist;
                context.Set<Patient>()
                    .Add(patient);

                const int appointmentsPerPatient = 100;
                for (int j = 0; j < appointmentsPerPatient; ++j)
                {
                    int duration = faker.Random.ListItem(durations);

                    Appointment appointment = new()
                    {
                        StartTime = nextAvailable,
                        DurationInMinutes = duration,
                        Type = faker.PickRandom<AppointmentType>(),
                        Status = faker.PickRandom<AppointmentStatus>(),
                        TherapistId = patient.TherapistId,
                        Therapist = patient.Therapist,
                        PatientId = patient.Id,
                        Patient = patient
                    };

                    toInsert.Add(appointment);

                    nextAvailable = nextAvailable.AddMinutes(duration + paddingMinutes);
                }
            }

            context.Set<Appointment>()
                .AddRange(toInsert);
            context.SaveChanges();
        }

        context.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, bool _, CancellationToken ct)
    {
        if (await context.Set<Therapist>()
                .AnyAsync(t => t.FullName != "admin", ct))
        {
            return;
        }

        const string locale = "pl";

        const int therapistCount = 100;
        Faker faker = new(locale);

        Faker<User> userFaker = new Faker<User>().RuleFor(u => u.Email, f => f.Internet.Email());

        Faker<Therapist> therapistFaker = new Faker<Therapist>().RuleFor(t => t.FullName, f => f.Person.FullName);

        PasswordHasher<User> passwordHasher = new();

        IdentityRole role = await context.Set<IdentityRole>()
            .SingleAsync(r => r.NormalizedName == "THERAPIST", ct);

        for (int i = 0; i < therapistCount; ++i)
        {
            User user = userFaker.Generate();

            user.Id = Guid.NewGuid()
                .ToString(); // ensure unique string id if your User.Id is string
            user.UserName = user.Email;
            user.NormalizedUserName = user.Email!.ToUpperInvariant();
            user.NormalizedEmail = user.Email!.ToUpperInvariant();
            user.EmailConfirmed = true; // dev convenience
            user.SecurityStamp = Guid.NewGuid()
                .ToString();
            user.ConcurrencyStamp = Guid.NewGuid()
                .ToString();
            user.PasswordHash = passwordHasher.HashPassword(user, "P@ssword2");

            bool userExists = await context.Set<User>()
                .AnyAsync(u => u.NormalizedEmail == user.NormalizedEmail, ct);

            if (!userExists)
            {
                context.Set<User>()
                    .Add(user);
            }

            IdentityUserRole<string> userRole = new() { RoleId = role.Id, UserId = user.Id };
            context.Set<IdentityUserRole<string>>()
                .Add(userRole);


            Therapist therapist = therapistFaker.Generate();
            therapist.UserId = user.Id;
            therapist.User = user;

            context.Set<Therapist>()
                .Add(therapist);
        }

        await context.SaveChangesAsync(ct);

        Faker<Patient> patientFaker = new Faker<Patient>(locale).RuleFor(p => p.FullName, f => f.Name.FullName())
            .RuleFor(p => p.DateOfBirth,
                f => DateOnly.FromDateTime(f.Date.Between(DateTime.Today.AddYears(-25), DateTime.Today.AddYears(-5))))
            .RuleFor(p => p.ContactInfo, f => f.Random.Bool() ? f.Person.Email : f.Phone.PhoneNumber())
            .RuleFor(p => p.Notes, f => f.Random.Bool(0.75f) ? f.Lorem.Sentence() : null);

        int[] durations = [15, 30, 45, 60, 90];

        List<Therapist> therapists = await context.Set<Therapist>()
            .Where(t => t.FullName != "admin")
            .ToListAsync(ct);

        const int patientsPerTherapist = 100;
        const int paddingMinutes = 5;

        foreach (Therapist therapist in therapists)
        {
            DateTimeOffset nextAvailable = DateTimeOffset.UtcNow;
            List<Appointment> toInsert = new();

            for (int i = 0; i < patientsPerTherapist; ++i)
            {
                Patient patient = patientFaker.Generate();
                patient.TherapistId = therapist.Id;
                patient.Therapist = therapist;
                context.Set<Patient>()
                    .Add(patient);

                const int appointmentsPerPatient = 100;
                for (int j = 0; j < appointmentsPerPatient; ++j)
                {
                    int duration = faker.Random.ListItem(durations);

                    Appointment appointment = new()
                    {
                        StartTime = nextAvailable,
                        DurationInMinutes = duration,
                        Type = faker.PickRandom<AppointmentType>(),
                        Status = faker.PickRandom<AppointmentStatus>(),
                        TherapistId = patient.TherapistId,
                        Therapist = patient.Therapist,
                        PatientId = patient.Id,
                        Patient = patient
                    };

                    toInsert.Add(appointment);

                    nextAvailable = nextAvailable.AddMinutes(duration + paddingMinutes);
                }
            }

            context.Set<Appointment>()
                .AddRange(toInsert);
            await context.SaveChangesAsync(ct);
        }

        await context.SaveChangesAsync(ct);
    }
}
