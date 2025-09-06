using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using LogopedicBackend.Enums;
using LogopedicBackend.Models;
using LogopedicBackend.Services;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Logopedic.UnitTests;

public class AppointmentServiceTests
{
    private static LogopedicContext CreateContextWith(params Appointment[] appointments)
    {
        var options = new DbContextOptionsBuilder<LogopedicContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var ctx = new LogopedicContext(options);

        // Seed minimal graph for each appointment
        foreach (var a in appointments)
        {
            ctx.Therapists.Add(a.Therapist);
            ctx.Patients.Add(a.Patient);
            ctx.Appointments.Add(a);
        }

        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenAppointmentExistsAndTherapistMatches()
    {
        // Arrange
        var builder = new TestDataBuilder()
            .WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment();

        var (ctx, _, therapist, _, appointment) = await builder.BuildAsync();

        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(therapist!.Id));

        var patientService = Substitute.For<IPatientService>();

        var appointmentService = new AppointmentService(ctx, therapistService, patientService);

        // Act
        var result = await appointmentService.GetByIdAsync(appointment!.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(appointment.Id, result.Id);
        Assert.Equal(appointment.PatientId, result.PatientId);
        Assert.Equal(appointment.StartTime, result.StartTime);
        Assert.Equal(appointment.DurationInMinutes, result.DurationInMinutes);
        Assert.Equal(appointment.Type, result.Type);
        Assert.Equal(appointment.Status, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenAppointmentExistsAndTherapistDoesNotMatch()
    {
        // Arrange
        var builder = new TestDataBuilder()
            .WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment();

        var (ctx, _, therapist, _, appointment) = await builder.BuildAsync();

        var wrongTherapistId = therapist!.Id - 1;
        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(wrongTherapistId));

        var patientService = Substitute.For<IPatientService>();

        var appointmentService = new AppointmentService(ctx, therapistService, patientService);

        // Act
        var result = await appointmentService.GetByIdAsync(appointment!.Id);

        //Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenAppointmentDoesNotExistAndTherapistMatches()
    {
        // Arrange
        var builder = new TestDataBuilder()
            .WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment();

        var (ctx, _, therapist, _, appointment) = await builder.BuildAsync();

        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(therapist!.Id));

        var patientService = Substitute.For<IPatientService>();

        var appointmentService = new AppointmentService(ctx, therapistService, patientService);

        // Act
        var wrongAppointmentId = appointment!.Id - 1;
        var result = await appointmentService.GetByIdAsync(wrongAppointmentId);

        //Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenAppointmentDoesNotExistAndTherapistDoesNotMatch()
    {
        // Arrange
        var builder = new TestDataBuilder()
            .WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment();

        var (ctx, _, therapist, _, appointment) = await builder.BuildAsync();

        var wrongTherapistId = therapist!.Id - 1;
        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(wrongTherapistId));

        var patientService = Substitute.For<IPatientService>();

        var appointmentService = new AppointmentService(ctx, therapistService, patientService);

        // Act
        var wrongAppointmentId = appointment!.Id - 1;
        var result = await appointmentService.GetByIdAsync(wrongAppointmentId);

        //Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task QueryAsync_ReturnsPagedResultDto_WhenQueryIsValid()
    {
        var therapist = new Therapist
        {
            Id = 1, FullName = "Dr", UserId = Guid.NewGuid().ToString(),
            User = new User { Id = Guid.NewGuid().ToString() }
        };
        var patient1 = new Patient
        {
            Id = 1, FullName = "P1", DateOfBirth = new DateOnly(2010, 1, 1), ContactInfo = "c",
            TherapistId = therapist.Id, Therapist = therapist
        };
        var patient2 = new Patient
        {
            Id = 2, FullName = "P2", DateOfBirth = new DateOnly(2011, 1, 1), ContactInfo = "c2",
            TherapistId = therapist.Id, Therapist = therapist
        };

        var a1 = new Appointment
        {
            Id = 10, StartTime = DateTimeOffset.UtcNow.AddDays(-1), DurationInMinutes = 30,
            Type = AppointmentType.Diagnosis, Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id,
            Therapist = therapist, PatientId = patient1.Id, Patient = patient1
        };
        var a2 = new Appointment
        {
            Id = 11, StartTime = DateTimeOffset.UtcNow, DurationInMinutes = 45, Type = AppointmentType.Consultation,
            Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id, Therapist = therapist,
            PatientId = patient2.Id, Patient = patient2
        };

        await using var ctx = CreateContextWith(a1, a2);

        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        var patientService = Substitute.For<IPatientService>();

        var appointmentService = new AppointmentService(ctx, therapistService, patientService);

        var query = new AppointmentQueryParameters
        {
            PageNumber = 1,
            PageSize = 1, // intentionally small to exercise paging
            From = DateTimeOffset.Now.AddDays(-7),
            To = DateTimeOffset.Now.AddDays(7)
        };

        // Act
        var result = await appointmentService.QueryAsync(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsT0, "expected PagedResultDto variant");
        var paged = result.AsT0; // OneOf exposes AsT0/AsT1...
        Assert.Equal(1, paged.PageNumber);
        Assert.Equal(1, paged.PageSize);
        Assert.Equal(2, paged.TotalCount); // total matching records
        Assert.Single(paged.Items);
        // ensure returned item is one of the appointments and mapping looks correct
        Assert.Contains(paged.Items.Select(i => i.Id), ids => ids == a1.Id || ids == a2.Id);
    }

    [Fact]
    public async Task QueryAsync_ReturnsInvalidDateRangeError_WhenStartAfterEnd()
    {
        // arrange - (no DB records required for validation failure)
        await using var ctx = CreateContextWith();

        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        var patientService = Substitute.For<IPatientService>();

        var appointmentService = new AppointmentService(ctx, therapistService, patientService);

        var query = new AppointmentQueryParameters
        {
            PageNumber = 1,
            PageSize = 20,
            From = DateTimeOffset.Now.AddDays(7),
            To = DateTimeOffset.Now.AddDays(-7) // End before start
        };

        // act
        var result = await appointmentService.QueryAsync(query, CancellationToken.None);

        // assert: expect the InvalidDateRangeError variant
        Assert.True(result.IsT1, "expected InvalidDateRangeError variant");
    }

    [Fact]
    public async Task QueryAsync_ReturnsInvalidPageSizeError_WhenPageSizeTooLargeOrTooSmall()
    {
        // arrange
        await using var ctx = CreateContextWith();

        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        var patientService = Substitute.For<IPatientService>();

        var appointmentService = new AppointmentService(ctx, therapistService, patientService);

        var query1 = new AppointmentQueryParameters
        {
            PageNumber = 1,
            PageSize = AppointmentQueryParameters.MaxPageSize + 1 // exceed allowed limit
        };

        var query2 = new AppointmentQueryParameters
        {
            PageNumber = 1,
            PageSize = AppointmentQueryParameters.MinPageSize - 1 // exceed allowed limit
        };

        // act
        var result1 = await appointmentService.QueryAsync(query1, CancellationToken.None);
        var result2 = await appointmentService.QueryAsync(query2, CancellationToken.None);

        // assert: expect the InvalidPageSizeError variant
        Assert.True(result1.IsT2, "expected InvalidPageSizeError variant");
        Assert.True(result2.IsT2, "expected InvalidPageSizeError variant");
    }

    [Fact]
    public async Task QueryAsync_FiltersByDateRange_ReturnsOnlyAppointmentsWithinRange()
    {
        var now = DateTimeOffset.UtcNow;
        var therapist = new Therapist
        {
            Id = 1, FullName = "Dr. Smith", UserId = Guid.NewGuid().ToString(),
            User = new User { Id = Guid.NewGuid().ToString() }
        };
        var patient1 = new Patient
        {
            Id = 1, FullName = "Alice", DateOfBirth = new DateOnly(2010, 1, 1), ContactInfo = "alice@test.local",
            TherapistId = therapist.Id, Therapist = therapist
        };
        var patient2 = new Patient
        {
            Id = 2, FullName = "Bob", DateOfBirth = new DateOnly(2011, 2, 2), ContactInfo = "bob@test.local",
            TherapistId = therapist.Id, Therapist = therapist
        };
        var patient3 = new Patient
        {
            Id = 3, FullName = "Charlie", DateOfBirth = new DateOnly(2012, 3, 3), ContactInfo = "charlie@test.local",
            TherapistId = therapist.Id, Therapist = therapist
        };

        var appointments = new List<Appointment>
        {
            new()
            {
                Id = 10, StartTime = now.AddDays(-3), DurationInMinutes = 30,
                Type = AppointmentType.Diagnosis, Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id, Therapist = therapist, PatientId = patient1.Id, Patient = patient1
            },
            new()
            {
                Id = 11, StartTime = now.AddDays(-2), DurationInMinutes = 45,
                Type = AppointmentType.Consultation, Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id, Therapist = therapist, PatientId = patient2.Id, Patient = patient2
            },
            new()
            {
                Id = 12, StartTime = now.AddDays(-1), DurationInMinutes = 60,
                Type = AppointmentType.Therapy, Status = AppointmentStatus.Completed,
                TherapistId = therapist.Id, Therapist = therapist, PatientId = patient1.Id, Patient = patient1
            },
            new()
            {
                Id = 13, StartTime = now, DurationInMinutes = 30,
                Type = AppointmentType.Consultation, Status = AppointmentStatus.Cancelled,
                TherapistId = therapist.Id, Therapist = therapist, PatientId = patient3.Id, Patient = patient3
            },
            new()
            {
                Id = 14, StartTime = now.AddDays(1), DurationInMinutes = 45,
                Type = AppointmentType.Diagnosis, Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id, Therapist = therapist, PatientId = patient2.Id, Patient = patient2
            }
        };

        await using var ctx = CreateContextWith(appointments.ToArray());

        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        var patientService = Substitute.For<IPatientService>();

        var appointmentService = new AppointmentService(ctx, therapistService, patientService);


        // Expected ids: empty list
        var query1 = new AppointmentQueryParameters
        {
            PageNumber = 1,
            PageSize = appointments.Count,
            From = now.AddDays(2),
            To = now.AddDays(3)
        };

        // Expected ids: 11, 12
        var query2 = new AppointmentQueryParameters
        {
            PageNumber = 1,
            PageSize = appointments.Count,
            From = now.AddDays(-2),
            To = now,
        };

        // Expected ids: 12, 13
        var query3 = new AppointmentQueryParameters
        {
            PageNumber = 1,
            PageSize = appointments.Count,
            From = now.AddDays(-1),
            To = now.AddDays(1)
        };

        // Act
        var result1 = await appointmentService.QueryAsync(query1, CancellationToken.None);
        var result2 = await appointmentService.QueryAsync(query2, CancellationToken.None);
        var result3 = await appointmentService.QueryAsync(query3, CancellationToken.None);

        // Assert
        Assert.True(result1.IsT0);
        Assert.True(result2.IsT0);
        Assert.True(result3.IsT0);

        var actualIds1 = result1.AsT0.Items.Select(a => a.Id).ToArray();
        var expectedIds1 = Array.Empty<int>();
        Assert.Equal(expectedIds1, actualIds1);

        var actualIds2 = result2.AsT0.Items.Select(a => a.Id).ToArray();
        var expectedIds2 = new[] { 11, 12 };
        Assert.Equal(expectedIds2, actualIds2);

        var actualIds3 = result3.AsT0.Items.Select(a => a.Id).ToArray();
        var expectedIds3 = new[] { 12, 13 };
        Assert.Equal(expectedIds3, actualIds3);
    }

    [Fact]
    public async Task QueryAsync_FirstPage_ReturnsFirstNItems()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var therapist = new Therapist
        {
            Id = 1, FullName = "Dr. Smith", UserId = Guid.NewGuid().ToString(),
            User = new User { Id = Guid.NewGuid().ToString() }
        };
        var patient = new Patient
        {
            Id = 1, FullName = "Alice", DateOfBirth = new DateOnly(2010, 1, 1), ContactInfo = "alice@test.local",
            TherapistId = therapist.Id, Therapist = therapist
        };

        var appointments = new List<Appointment>();
        for (var i = 1; i <= 5; i++)
        {
            appointments.Add(new Appointment
            {
                Id = i,
                StartTime = now.AddDays(i),
                DurationInMinutes = 30,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient.Id,
                Patient = patient
            });
        }

        await using var ctx = new LogopedicContext(new DbContextOptionsBuilder<LogopedicContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        ctx.Therapists.Add(therapist);
        ctx.Patients.Add(patient);
        ctx.Appointments.AddRange(appointments);
        await ctx.SaveChangesAsync();

        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        var patientService = Substitute.For<IPatientService>();

        var svc = new AppointmentService(ctx, therapistService, patientService);

        var query = new AppointmentQueryParameters
        {
            PageNumber = 1,
            PageSize = 2
        };

        // Act
        var result = await svc.QueryAsync(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsT0);
        var paged = result.AsT0;
        Assert.Equal(2, paged.Items.Count);
        Assert.Equal(5, paged.TotalCount); // total appointments for therapist
        Assert.Equal([1, 2], paged.Items.Select(a => a.Id)); // first page IDs
    }

    [Fact]
    public async Task QueryAsync_PageBeyondLast_ReturnsEmptyItemsButCorrectTotalCount()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var therapist = new Therapist
        {
            Id = 1, FullName = "Dr. Smith", UserId = Guid.NewGuid().ToString(),
            User = new User { Id = Guid.NewGuid().ToString() }
        };
        var patient = new Patient
        {
            Id = 1, FullName = "Alice", DateOfBirth = new DateOnly(2010, 1, 1), ContactInfo = "alice@test.local",
            TherapistId = therapist.Id, Therapist = therapist
        };

        var appointments = Enumerable.Range(1, 5).Select(i => new Appointment
        {
            Id = i,
            StartTime = now.AddDays(i),
            DurationInMinutes = 30,
            Type = AppointmentType.Consultation,
            Status = AppointmentStatus.Scheduled,
            TherapistId = therapist.Id,
            Therapist = therapist,
            PatientId = patient.Id,
            Patient = patient
        }).ToList();

        await using var ctx = new LogopedicContext(new DbContextOptionsBuilder<LogopedicContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        ctx.Therapists.Add(therapist);
        ctx.Patients.Add(patient);
        ctx.Appointments.AddRange(appointments);
        await ctx.SaveChangesAsync();

        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        var patientService = Substitute.For<IPatientService>();

        var svc = new AppointmentService(ctx, therapistService, patientService);

        var query = new AppointmentQueryParameters
        {
            PageNumber = 4, // each page has 2 items; pages 1-3 cover all 5
            PageSize = 2
        };

        // Act
        var result = await svc.QueryAsync(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsT0);
        var paged = result.AsT0;
        Assert.Empty(paged.Items); // no items on page beyond last
        Assert.Equal(5, paged.TotalCount); // total count remains correct
    }

    [Fact]
    public async Task QueryAsync_SortByStartTimeAsc_OrdersCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var therapist = new Therapist
        {
            Id = 1, FullName = "Dr. Smith", UserId = Guid.NewGuid().ToString(),
            User = new User { Id = Guid.NewGuid().ToString() }
        };
        var patient = new Patient
        {
            Id = 1, FullName = "Alice", DateOfBirth = new DateOnly(2010, 1, 1), ContactInfo = "alice@test.local",
            TherapistId = therapist.Id, Therapist = therapist
        };

        // Appointments with overlapping StartTime and Duration to test tie-breaking
        var appointments = new List<Appointment>
        {
            new Appointment
            {
                Id = 4, StartTime = now.AddHours(2), DurationInMinutes = 30, Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id, Therapist = therapist,
                PatientId = patient.Id, Patient = patient
            },
            new Appointment
            {
                Id = 2, StartTime = now.AddHours(2), DurationInMinutes = 45, Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id, Therapist = therapist,
                PatientId = patient.Id, Patient = patient
            },
            new Appointment
            {
                Id = 3, StartTime = now.AddHours(1), DurationInMinutes = 60, Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id, Therapist = therapist,
                PatientId = patient.Id, Patient = patient
            },
            new Appointment
            {
                Id = 1, StartTime = now.AddHours(1), DurationInMinutes = 30, Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id, Therapist = therapist,
                PatientId = patient.Id, Patient = patient
            },
            new Appointment
            {
                Id = 5, StartTime = now.AddHours(1), DurationInMinutes = 30, Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id, Therapist = therapist,
                PatientId = patient.Id, Patient = patient
            }
        };

        await using var ctx = new LogopedicContext(new DbContextOptionsBuilder<LogopedicContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        ctx.Therapists.Add(therapist);
        ctx.Patients.Add(patient);
        ctx.Appointments.AddRange(appointments);
        await ctx.SaveChangesAsync();

        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        var patientService = Substitute.For<IPatientService>();

        var svc = new AppointmentService(ctx, therapistService, patientService);

        var query = new AppointmentQueryParameters
        {
            Sort = "startTime:asc",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await svc.QueryAsync(query, CancellationToken.None);
        var paged = result.AsT0;

        // Expect ascending StartTime, tie-break by Id ascending
        var expectedOrder = new[] { 1, 3, 5, 2, 4 };
        Assert.Equal(expectedOrder, paged.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task QueryAsync_SortByDurationDesc_OrdersCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var therapist = new Therapist
        {
            Id = 1, FullName = "Dr. Smith", UserId = Guid.NewGuid().ToString(),
            User = new User { Id = Guid.NewGuid().ToString() }
        };
        var patient = new Patient
        {
            Id = 1, FullName = "Alice", DateOfBirth = new DateOnly(2010, 1, 1), ContactInfo = "alice@test.local",
            TherapistId = therapist.Id, Therapist = therapist
        };

        // Appointments with overlapping StartTime and Duration to test tie-breaking
        var appointments = new List<Appointment>
        {
            new Appointment
            {
                Id = 4, StartTime = now.AddHours(2), DurationInMinutes = 30, Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id, Therapist = therapist,
                PatientId = patient.Id, Patient = patient
            },
            new Appointment
            {
                Id = 2, StartTime = now.AddHours(2), DurationInMinutes = 45, Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id, Therapist = therapist,
                PatientId = patient.Id, Patient = patient
            },
            new Appointment
            {
                Id = 3, StartTime = now.AddHours(1), DurationInMinutes = 60, Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id, Therapist = therapist,
                PatientId = patient.Id, Patient = patient
            },
            new Appointment
            {
                Id = 5, StartTime = now.AddHours(1), DurationInMinutes = 30, Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id, Therapist = therapist,
                PatientId = patient.Id, Patient = patient
            },
            new Appointment
            {
                Id = 1, StartTime = now.AddHours(1), DurationInMinutes = 30, Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled, TherapistId = therapist.Id, Therapist = therapist,
                PatientId = patient.Id, Patient = patient
            }
        };

        await using var ctx = new LogopedicContext(new DbContextOptionsBuilder<LogopedicContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        ctx.Therapists.Add(therapist);
        ctx.Patients.Add(patient);
        ctx.Appointments.AddRange(appointments);
        await ctx.SaveChangesAsync();

        var therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        var patientService = Substitute.For<IPatientService>();

        var svc = new AppointmentService(ctx, therapistService, patientService);

        var query = new AppointmentQueryParameters
        {
            Sort = "duration:desc",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await svc.QueryAsync(query, CancellationToken.None);
        var paged = result.AsT0;

        // Expect descending Duration, tie-break by Id ascending
        var expectedOrder = new[] { 3, 2, 1, 4, 5 };
        Assert.Equal(expectedOrder, paged.Items.Select(a => a.Id));
    }
}