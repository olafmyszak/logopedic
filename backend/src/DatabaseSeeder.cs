using Bogus;
using LogopedicBackend.Enums;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend;

public static class DatabaseSeeder
{
    private static readonly TimeZoneInfo s_warsawTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

    public static void Seed(DbContext context, bool _)
    {
        if (context.Set<Appointment>()
            .Any())
        {
            return;
        }

        bool seedTherapists = !context.Set<Therapist>()
            .Any(t => t.FullName != "admin");

        Console.WriteLine("Seeding...");

        const string locale = "pl";
        Faker faker = new(locale);

        if (seedTherapists)
        {
            const int therapistCount = 100;

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
        }

        Faker<Patient> patientFaker = new Faker<Patient>(locale).RuleFor(p => p.FullName, f => f.Name.FullName())
            .RuleFor(p => p.DateOfBirth,
                f => DateOnly.FromDateTime(f.Date.Between(DateTime.Today.AddYears(-25), DateTime.Today.AddYears(-5))))
            .RuleFor(p => p.ContactInfo, f => f.Random.Bool() ? f.Person.Email : f.Phone.PhoneNumber())
            .RuleFor(p => p.Notes, f => f.Random.Bool(0.75f) ? f.Lorem.Sentence() : null);

        int[] durations = [15, 30, 45, 60, 90];

        List<Therapist> therapists = context.Set<Therapist>()
            .Where(t => t.FullName != "admin")
            .ToList();

        const int patientsPerTherapist = 10;
        const int paddingMinutes = 5;

        foreach (Therapist therapist in therapists)
        {
            DateTimeOffset nextAvailable = AdjustToBusinessHours(DateTimeOffset.UtcNow);
            List<Appointment> toInsert = [];

            for (int i = 0; i < patientsPerTherapist; ++i)
            {
                Patient patient = patientFaker.Generate();
                patient.TherapistId = therapist.Id;
                patient.Therapist = therapist;
                context.Set<Patient>()
                    .Add(patient);

                const int appointmentsPerPatient = 10;
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

                    // Check if the next appointment would go past 5pm or need to move to next business day
                    nextAvailable = EnsureWithinBusinessHours(nextAvailable, duration);
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
        if (await context.Set<Appointment>()
                .AnyAsync(ct))
        {
            return;
        }

        bool seedTherapists = !await context.Set<Therapist>()
            .AnyAsync(t => t.FullName != "admin", ct);

        const string locale = "pl";

        const int therapistCount = 100;
        Faker faker = new(locale);

        if (seedTherapists)
        {
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
        }

        Faker<Patient> patientFaker = new Faker<Patient>(locale).RuleFor(p => p.FullName, f => f.Name.FullName())
            .RuleFor(p => p.DateOfBirth,
                f => DateOnly.FromDateTime(f.Date.Between(DateTime.Today.AddYears(-25), DateTime.Today.AddYears(-5))))
            .RuleFor(p => p.ContactInfo, f => f.Random.Bool() ? f.Person.Email : f.Phone.PhoneNumber())
            .RuleFor(p => p.Notes, f => f.Random.Bool(0.75f) ? f.Lorem.Sentence() : null);

        int[] durations = [15, 30, 45, 60, 90];

        List<Therapist> therapists = await context.Set<Therapist>()
            .Where(t => t.FullName != "admin")
            .ToListAsync(ct);

        const int patientsPerTherapist = 10;
        const int paddingMinutes = 5;

        foreach (Therapist therapist in therapists)
        {
            DateTimeOffset nextAvailable = AdjustToBusinessHours(DateTimeOffset.UtcNow);
            List<Appointment> toInsert = [];

            for (int i = 0; i < patientsPerTherapist; ++i)
            {
                Patient patient = patientFaker.Generate();
                patient.TherapistId = therapist.Id;
                patient.Therapist = therapist;
                context.Set<Patient>()
                    .Add(patient);

                const int appointmentsPerPatient = 10;
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

                    // Check if the next appointment would go past 5pm or need to move to next business day
                    nextAvailable = EnsureWithinBusinessHours(nextAvailable, duration);
                }
            }

            context.Set<Appointment>()
                .AddRange(toInsert);
            await context.SaveChangesAsync(ct);
        }

        await context.SaveChangesAsync(ct);
    }

    private static DateTimeOffset AdjustToBusinessHours(DateTimeOffset dateTimeOffset)
    {
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(dateTimeOffset.UtcDateTime, s_warsawTimeZone);
        DateTime today9Am = localTime.Date.AddHours(9);

        if (localTime.TimeOfDay < TimeSpan.FromHours(9))
        {
            // Before 9am today, start at 9am today
            return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(today9Am, s_warsawTimeZone));
        }

        if (localTime.TimeOfDay >= TimeSpan.FromHours(17))
        {
            // After 5pm today, start at 9am tomorrow
            return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(today9Am.AddDays(1), s_warsawTimeZone));
        }

        return dateTimeOffset;
    }

    private static DateTimeOffset EnsureWithinBusinessHours(DateTimeOffset nextTime, int durationInMinutes)
    {
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(nextTime.UtcDateTime, s_warsawTimeZone);
        DateTime endTime = localTime.AddMinutes(durationInMinutes);

        // If appointment would end after 5pm, move to 9am next day
        if (endTime.TimeOfDay > TimeSpan.FromHours(17))
        {
            // Use Date.AddDays() to set hour to 0:00
            DateTime nextDay = localTime.Date.AddDays(1);

            while (nextDay.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                nextDay = nextDay.AddDays(1);
            }

            DateTime next9Am = TimeZoneInfo.ConvertTimeToUtc(nextDay.AddHours(9), s_warsawTimeZone);
            return new DateTimeOffset(next9Am);
        }

        return nextTime;
    }
}
