using System.Linq.Expressions;
using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using LogopedicBackend.Enums;
using LogopedicBackend.Models;
using LogopedicBackend.Services;
using LogopedicBackend.Services.Results.Appointments;
using LogopedicBackend.Services.Results.Common.NotFound;
using LogopedicBackend.Services.Results.Common.Paging;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OneOf;

namespace Logopedic.UnitTests;

public class AppointmentServiceTests
{
    private static LogopedicContext CreateContextWith(params Appointment[] appointments)
    {
        DbContextOptions<LogopedicContext> options = new DbContextOptionsBuilder<LogopedicContext>()
            .UseInMemoryDatabase(Guid.NewGuid()
                .ToString())
            .Options;

        LogopedicContext ctx = new(options);

        // Seed minimal graph for each appointment
        foreach (Appointment a in appointments)
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
        TestDataBuilder builder = new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment();

        (LogopedicContext ctx, _, Therapist? therapist, _, Appointment? appointment) = await builder.BuildAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(therapist!.Id));

        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService appointmentService = new(ctx, therapistService, patientService);

        // Act
        AppointmentDto? result = await appointmentService.GetByIdAsync(appointment!.Id);

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
        TestDataBuilder builder = new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment();

        (LogopedicContext ctx, _, Therapist? therapist, _, Appointment? appointment) = await builder.BuildAsync();

        int wrongTherapistId = therapist!.Id - 1;
        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(wrongTherapistId));

        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService appointmentService = new(ctx, therapistService, patientService);

        // Act
        AppointmentDto? result = await appointmentService.GetByIdAsync(appointment!.Id);

        //Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenAppointmentDoesNotExistAndTherapistMatches()
    {
        // Arrange
        TestDataBuilder builder = new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment();

        (LogopedicContext ctx, _, Therapist? therapist, _, Appointment? appointment) = await builder.BuildAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(therapist!.Id));

        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService appointmentService = new(ctx, therapistService, patientService);

        // Act
        int wrongAppointmentId = appointment!.Id - 1;
        AppointmentDto? result = await appointmentService.GetByIdAsync(wrongAppointmentId);

        //Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenAppointmentDoesNotExistAndTherapistDoesNotMatch()
    {
        // Arrange
        TestDataBuilder builder = new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment();

        (LogopedicContext ctx, _, Therapist? therapist, _, Appointment? appointment) = await builder.BuildAsync();

        int wrongTherapistId = therapist!.Id - 1;
        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(wrongTherapistId));

        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService appointmentService = new(ctx, therapistService, patientService);

        // Act
        int wrongAppointmentId = appointment!.Id - 1;
        AppointmentDto? result = await appointmentService.GetByIdAsync(wrongAppointmentId);

        //Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task QueryAsync_ReturnsPagedResultDto_WhenQueryIsValid()
    {
        Therapist therapist = new()
        {
            Id = 1,
            FullName = "Dr",
            UserId = Guid.NewGuid()
                .ToString(),
            User = new User
            {
                Id = Guid.NewGuid()
                    .ToString()
            }
        };
        Patient patient1 = new()
        {
            Id = 1,
            FullName = "P1",
            DateOfBirth = new DateOnly(2010, 1, 1),
            ContactInfo = "c",
            TherapistId = therapist.Id,
            Therapist = therapist
        };
        Patient patient2 = new()
        {
            Id = 2,
            FullName = "P2",
            DateOfBirth = new DateOnly(2011, 1, 1),
            ContactInfo = "c2",
            TherapistId = therapist.Id,
            Therapist = therapist
        };

        Appointment a1 = new()
        {
            Id = 10,
            StartTime = DateTimeOffset.UtcNow.AddDays(-1),
            DurationInMinutes = 30,
            Type = AppointmentType.Diagnosis,
            Status = AppointmentStatus.Scheduled,
            TherapistId = therapist.Id,
            Therapist = therapist,
            PatientId = patient1.Id,
            Patient = patient1
        };
        Appointment a2 = new()
        {
            Id = 11,
            StartTime = DateTimeOffset.UtcNow,
            DurationInMinutes = 45,
            Type = AppointmentType.Consultation,
            Status = AppointmentStatus.Scheduled,
            TherapistId = therapist.Id,
            Therapist = therapist,
            PatientId = patient2.Id,
            Patient = patient2
        };

        await using LogopedicContext ctx = CreateContextWith(a1, a2);

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService appointmentService = new(ctx, therapistService, patientService);

        AppointmentQueryParameters query = new()
        {
            PageNumber = 1,
            PageSize = 1, // intentionally small to exercise paging
            From = DateTimeOffset.Now.AddDays(-7),
            To = DateTimeOffset.Now.AddDays(7)
        };

        // Act
        OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError> result =
            await appointmentService.QueryAsync(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsT0, "expected PagedResultDto variant");
        PagedResultDto<AppointmentDto>? paged = result.AsT0; // OneOf exposes AsT0/AsT1...
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
        await using LogopedicContext ctx = CreateContextWith();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));
        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService appointmentService = new(ctx, therapistService, patientService);

        AppointmentQueryParameters query = new()
        {
            PageNumber = 1,
            PageSize = 20,
            From = DateTimeOffset.Now.AddDays(7),
            To = DateTimeOffset.Now.AddDays(-7) // End before start
        };

        // act
        OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError> result =
            await appointmentService.QueryAsync(query, CancellationToken.None);

        // assert: expect the InvalidDateRangeError variant
        Assert.True(result.IsT1, "expected InvalidDateRangeError variant");
    }

    [Fact]
    public async Task QueryAsync_ReturnsInvalidPageSizeError_WhenPageSizeTooLargeOrTooSmall()
    {
        // arrange
        await using LogopedicContext ctx = CreateContextWith();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));
        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService appointmentService = new(ctx, therapistService, patientService);

        AppointmentQueryParameters query1 = new()
        {
            PageNumber = 1, PageSize = AppointmentQueryParameters.MaxPageSize + 1 // exceed allowed limit
        };

        AppointmentQueryParameters query2 = new()
        {
            PageNumber = 1, PageSize = AppointmentQueryParameters.MinPageSize - 1 // exceed allowed limit
        };

        // act
        OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError> result1 =
            await appointmentService.QueryAsync(query1, CancellationToken.None);
        OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError> result2 =
            await appointmentService.QueryAsync(query2, CancellationToken.None);

        // assert: expect the InvalidPageSizeError variant
        Assert.True(result1.IsT2, "expected InvalidPageSizeError variant");
        Assert.True(result2.IsT2, "expected InvalidPageSizeError variant");
    }

    [Fact]
    public async Task QueryAsync_FiltersByDateRange_ReturnsOnlyAppointmentsWithinRange()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Therapist therapist = new()
        {
            Id = 1,
            FullName = "Dr. Smith",
            UserId = Guid.NewGuid()
                .ToString(),
            User = new User
            {
                Id = Guid.NewGuid()
                    .ToString()
            }
        };
        Patient patient1 = new()
        {
            Id = 1,
            FullName = "Alice",
            DateOfBirth = new DateOnly(2010, 1, 1),
            ContactInfo = "alice@test.local",
            TherapistId = therapist.Id,
            Therapist = therapist
        };
        Patient patient2 = new()
        {
            Id = 2,
            FullName = "Bob",
            DateOfBirth = new DateOnly(2011, 2, 2),
            ContactInfo = "bob@test.local",
            TherapistId = therapist.Id,
            Therapist = therapist
        };
        Patient patient3 = new()
        {
            Id = 3,
            FullName = "Charlie",
            DateOfBirth = new DateOnly(2012, 3, 3),
            ContactInfo = "charlie@test.local",
            TherapistId = therapist.Id,
            Therapist = therapist
        };

        List<Appointment> appointments = new()
        {
            new Appointment
            {
                Id = 10,
                StartTime = now.AddDays(-3),
                DurationInMinutes = 30,
                Type = AppointmentType.Diagnosis,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient1.Id,
                Patient = patient1
            },
            new Appointment
            {
                Id = 11,
                StartTime = now.AddDays(-2),
                DurationInMinutes = 45,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient2.Id,
                Patient = patient2
            },
            new Appointment
            {
                Id = 12,
                StartTime = now.AddDays(-1),
                DurationInMinutes = 60,
                Type = AppointmentType.Therapy,
                Status = AppointmentStatus.Completed,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient1.Id,
                Patient = patient1
            },
            new Appointment
            {
                Id = 13,
                StartTime = now,
                DurationInMinutes = 30,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Cancelled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient3.Id,
                Patient = patient3
            },
            new Appointment
            {
                Id = 14,
                StartTime = now.AddDays(1),
                DurationInMinutes = 45,
                Type = AppointmentType.Diagnosis,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient2.Id,
                Patient = patient2
            }
        };

        await using LogopedicContext ctx = CreateContextWith(appointments.ToArray());

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService appointmentService = new(ctx, therapistService, patientService);


        // Expected ids: empty list
        AppointmentQueryParameters query1 = new()
        {
            PageNumber = 1, PageSize = appointments.Count, From = now.AddDays(2), To = now.AddDays(3)
        };

        // Expected ids: 11, 12
        AppointmentQueryParameters query2 = new()
        {
            PageNumber = 1, PageSize = appointments.Count, From = now.AddDays(-2), To = now
        };

        // Expected ids: 12, 13
        AppointmentQueryParameters query3 = new()
        {
            PageNumber = 1, PageSize = appointments.Count, From = now.AddDays(-1), To = now.AddDays(1)
        };

        // Act
        OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError> result1 =
            await appointmentService.QueryAsync(query1, CancellationToken.None);
        OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError> result2 =
            await appointmentService.QueryAsync(query2, CancellationToken.None);
        OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError> result3 =
            await appointmentService.QueryAsync(query3, CancellationToken.None);

        // Assert
        Assert.True(result1.IsT0);
        Assert.True(result2.IsT0);
        Assert.True(result3.IsT0);

        int[] actualIds1 = result1.AsT0
            .Items
            .Select(a => a.Id)
            .ToArray();
        int[] expectedIds1 = Array.Empty<int>();
        Assert.Equal(expectedIds1, actualIds1);

        int[] actualIds2 = result2.AsT0
            .Items
            .Select(a => a.Id)
            .ToArray();
        int[] expectedIds2 = new[] { 11, 12 };
        Assert.Equal(expectedIds2, actualIds2);

        int[] actualIds3 = result3.AsT0
            .Items
            .Select(a => a.Id)
            .ToArray();
        int[] expectedIds3 = new[] { 12, 13 };
        Assert.Equal(expectedIds3, actualIds3);
    }

    [Fact]
    public async Task QueryAsync_FirstPage_ReturnsFirstNItems()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Therapist therapist = new()
        {
            Id = 1,
            FullName = "Dr. Smith",
            UserId = Guid.NewGuid()
                .ToString(),
            User = new User
            {
                Id = Guid.NewGuid()
                    .ToString()
            }
        };
        Patient patient = new()
        {
            Id = 1,
            FullName = "Alice",
            DateOfBirth = new DateOnly(2010, 1, 1),
            ContactInfo = "alice@test.local",
            TherapistId = therapist.Id,
            Therapist = therapist
        };

        List<Appointment> appointments = new();
        for (int i = 1; i <= 5; i++)
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

        await using LogopedicContext ctx = new(new DbContextOptionsBuilder<LogopedicContext>().UseInMemoryDatabase(Guid
                .NewGuid()
                .ToString())
            .Options);

        ctx.Therapists.Add(therapist);
        ctx.Patients.Add(patient);
        ctx.Appointments.AddRange(appointments);
        await ctx.SaveChangesAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService svc = new(ctx, therapistService, patientService);

        AppointmentQueryParameters query = new() { PageNumber = 1, PageSize = 2 };

        // Act
        OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError> result =
            await svc.QueryAsync(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsT0);
        PagedResultDto<AppointmentDto>? paged = result.AsT0;
        Assert.Equal(2, paged.Items.Count);
        Assert.Equal(5, paged.TotalCount); // total appointments for therapist
        Assert.Equal([1, 2], paged.Items.Select(a => a.Id)); // first page IDs
    }

    [Fact]
    public async Task QueryAsync_PageBeyondLast_ReturnsEmptyItemsButCorrectTotalCount()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Therapist therapist = new()
        {
            Id = 1,
            FullName = "Dr. Smith",
            UserId = Guid.NewGuid()
                .ToString(),
            User = new User
            {
                Id = Guid.NewGuid()
                    .ToString()
            }
        };
        Patient patient = new()
        {
            Id = 1,
            FullName = "Alice",
            DateOfBirth = new DateOnly(2010, 1, 1),
            ContactInfo = "alice@test.local",
            TherapistId = therapist.Id,
            Therapist = therapist
        };

        List<Appointment> appointments = Enumerable.Range(1, 5)
            .Select(i => new Appointment
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
            })
            .ToList();

        await using LogopedicContext ctx = new(new DbContextOptionsBuilder<LogopedicContext>().UseInMemoryDatabase(Guid
                .NewGuid()
                .ToString())
            .Options);

        ctx.Therapists.Add(therapist);
        ctx.Patients.Add(patient);
        ctx.Appointments.AddRange(appointments);
        await ctx.SaveChangesAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService svc = new(ctx, therapistService, patientService);

        AppointmentQueryParameters query = new()
        {
            PageNumber = 4, // each page has 2 items; pages 1-3 cover all 5
            PageSize = 2
        };

        // Act
        OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError> result =
            await svc.QueryAsync(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsT0);
        PagedResultDto<AppointmentDto>? paged = result.AsT0;
        Assert.Empty(paged.Items); // no items on page beyond last
        Assert.Equal(5, paged.TotalCount); // total count remains correct
    }

    [Fact]
    public async Task QueryAsync_SortByStartTimeAsc_OrdersCorrectly()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Therapist therapist = new()
        {
            Id = 1,
            FullName = "Dr. Smith",
            UserId = Guid.NewGuid()
                .ToString(),
            User = new User
            {
                Id = Guid.NewGuid()
                    .ToString()
            }
        };
        Patient patient = new()
        {
            Id = 1,
            FullName = "Alice",
            DateOfBirth = new DateOnly(2010, 1, 1),
            ContactInfo = "alice@test.local",
            TherapistId = therapist.Id,
            Therapist = therapist
        };

        // Appointments with overlapping StartTime and Duration to test tie-breaking
        List<Appointment> appointments = new()
        {
            new Appointment
            {
                Id = 4,
                StartTime = now.AddHours(2),
                DurationInMinutes = 30,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient.Id,
                Patient = patient
            },
            new Appointment
            {
                Id = 2,
                StartTime = now.AddHours(2),
                DurationInMinutes = 45,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient.Id,
                Patient = patient
            },
            new Appointment
            {
                Id = 3,
                StartTime = now.AddHours(1),
                DurationInMinutes = 60,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient.Id,
                Patient = patient
            },
            new Appointment
            {
                Id = 1,
                StartTime = now.AddHours(1),
                DurationInMinutes = 30,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient.Id,
                Patient = patient
            },
            new Appointment
            {
                Id = 5,
                StartTime = now.AddHours(1),
                DurationInMinutes = 30,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient.Id,
                Patient = patient
            }
        };

        await using LogopedicContext ctx = new(new DbContextOptionsBuilder<LogopedicContext>().UseInMemoryDatabase(Guid
                .NewGuid()
                .ToString())
            .Options);

        ctx.Therapists.Add(therapist);
        ctx.Patients.Add(patient);
        ctx.Appointments.AddRange(appointments);
        await ctx.SaveChangesAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService svc = new(ctx, therapistService, patientService);

        AppointmentQueryParameters query = new() { Sort = "startTime:asc", PageNumber = 1, PageSize = 10 };

        OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError> result =
            await svc.QueryAsync(query, CancellationToken.None);
        PagedResultDto<AppointmentDto>? paged = result.AsT0;

        // Expect ascending StartTime, tie-break by Id ascending
        int[] expectedOrder = new[] { 1, 3, 5, 2, 4 };
        Assert.Equal(expectedOrder, paged.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task QueryAsync_SortByDurationDesc_OrdersCorrectly()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Therapist therapist = new()
        {
            Id = 1,
            FullName = "Dr. Smith",
            UserId = Guid.NewGuid()
                .ToString(),
            User = new User
            {
                Id = Guid.NewGuid()
                    .ToString()
            }
        };
        Patient patient = new()
        {
            Id = 1,
            FullName = "Alice",
            DateOfBirth = new DateOnly(2010, 1, 1),
            ContactInfo = "alice@test.local",
            TherapistId = therapist.Id,
            Therapist = therapist
        };

        // Appointments with overlapping StartTime and Duration to test tie-breaking
        List<Appointment> appointments = new()
        {
            new Appointment
            {
                Id = 4,
                StartTime = now.AddHours(2),
                DurationInMinutes = 30,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient.Id,
                Patient = patient
            },
            new Appointment
            {
                Id = 2,
                StartTime = now.AddHours(2),
                DurationInMinutes = 45,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient.Id,
                Patient = patient
            },
            new Appointment
            {
                Id = 3,
                StartTime = now.AddHours(1),
                DurationInMinutes = 60,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient.Id,
                Patient = patient
            },
            new Appointment
            {
                Id = 5,
                StartTime = now.AddHours(1),
                DurationInMinutes = 30,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient.Id,
                Patient = patient
            },
            new Appointment
            {
                Id = 1,
                StartTime = now.AddHours(1),
                DurationInMinutes = 30,
                Type = AppointmentType.Consultation,
                Status = AppointmentStatus.Scheduled,
                TherapistId = therapist.Id,
                Therapist = therapist,
                PatientId = patient.Id,
                Patient = patient
            }
        };

        await using LogopedicContext ctx = new(new DbContextOptionsBuilder<LogopedicContext>().UseInMemoryDatabase(Guid
                .NewGuid()
                .ToString())
            .Options);

        ctx.Therapists.Add(therapist);
        ctx.Patients.Add(patient);
        ctx.Appointments.AddRange(appointments);
        await ctx.SaveChangesAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist.Id));
        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService svc = new(ctx, therapistService, patientService);

        AppointmentQueryParameters query = new() { Sort = "duration:desc", PageNumber = 1, PageSize = 10 };

        OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError> result =
            await svc.QueryAsync(query, CancellationToken.None);
        PagedResultDto<AppointmentDto>? paged = result.AsT0;

        // Expect descending Duration, tie-break by Id ascending
        int[] expectedOrder = new[] { 3, 2, 1, 4, 5 };
        Assert.Equal(expectedOrder, paged.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task CreateAsync_ReturnsAppointmentCreated_WhenDtoIsValid()
    {
        (LogopedicContext ctx, _, Therapist? therapist, Patient? patient, _) = await new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment()
            .BuildAsync();

        // Arrange
        CreateAppointmentDto dto = new()
        {
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            DurationInMinutes = 45,
            Type = AppointmentType.Diagnosis,
            PatientId = patient!.Id
        };

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist)!);

        IPatientService? patientService = Substitute.For<IPatientService>();
        patientService.GetByIdAsync(patient.Id)!.Returns(Task.FromResult(patient));

        AppointmentService appointmentService = new(ctx, therapistService, patientService);

        // Act
        OneOf<AppointmentCreated, PatientNotFound, TimeConflict, DurationZeroOrLess> result =
            await appointmentService.CreateAsync(dto);

        // Assert
        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task CreateAsync_ReturnsPatientNotFound_WhenPatientIdDoesNotExist()
    {
        (LogopedicContext ctx, _, Therapist? therapist, Patient? patient, _) = await new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment()
            .BuildAsync();

        // Arrange
        CreateAppointmentDto dto = new()
        {
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            DurationInMinutes = 45,
            Type = AppointmentType.Diagnosis,
            PatientId = patient!.Id - 1 // Wrong patientId
        };

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist)!);

        IPatientService? patientService = Substitute.For<IPatientService>();
        patientService.GetByIdAsync(patient.Id)!.Returns(Task.FromResult(patient));

        AppointmentService appointmentService = new(ctx, therapistService, patientService);

        // Act
        OneOf<AppointmentCreated, PatientNotFound, TimeConflict, DurationZeroOrLess> result =
            await appointmentService.CreateAsync(dto);

        // Assert
        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task CreateAsync_ReturnsTimeConflict_WhenRequestedTimeConflictsWithExistingAppointments()
    {
        (LogopedicContext ctx, _, Therapist? therapist, Patient? patient, Appointment? appointment) =
            await new TestDataBuilder().WithUser()
                .WithTherapist()
                .WithPatient()
                .WithAppointment()
                .BuildAsync();

        // Arrange
        CreateAppointmentDto dto = new()
        {
            StartTime = appointment!.StartTime.AddMinutes(-15), // Make sure it overlaps an existing appointment
            DurationInMinutes = 45,
            Type = AppointmentType.Diagnosis,
            PatientId = patient!.Id
        };

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist)!);

        IPatientService? patientService = Substitute.For<IPatientService>();
        patientService.GetByIdAsync(patient.Id)!.Returns(Task.FromResult(patient));

        AppointmentService appointmentService = new(ctx, therapistService, patientService);

        // Act
        OneOf<AppointmentCreated, PatientNotFound, TimeConflict, DurationZeroOrLess> result =
            await appointmentService.CreateAsync(dto);

        // Assert
        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task CreateAsync_ReturnsDurationZeroOrLess_WhenDurationIsZeroOrLess()
    {
        (LogopedicContext ctx, _, Therapist? therapist, Patient? patient, _) = await new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment()
            .BuildAsync();

        // Arrange
        CreateAppointmentDto dto1 = new()
        {
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            DurationInMinutes = 0,
            Type = AppointmentType.Diagnosis,
            PatientId = patient!.Id
        };

        CreateAppointmentDto dto2 = new()
        {
            StartTime = DateTimeOffset.UtcNow.AddDays(3),
            DurationInMinutes = -5,
            Type = AppointmentType.Diagnosis,
            PatientId = patient.Id
        };

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(therapist)!);

        IPatientService? patientService = Substitute.For<IPatientService>();
        patientService.GetByIdAsync(patient.Id)!.Returns(Task.FromResult(patient));

        AppointmentService appointmentService = new(ctx, therapistService, patientService);

        // Act
        OneOf<AppointmentCreated, PatientNotFound, TimeConflict, DurationZeroOrLess> result1 =
            await appointmentService.CreateAsync(dto1);
        OneOf<AppointmentCreated, PatientNotFound, TimeConflict, DurationZeroOrLess> result2 =
            await appointmentService.CreateAsync(dto2);

        // Assert
        Assert.True(result1.IsT3);
        Assert.True(result2.IsT3);
    }

    [Fact]
    public async Task PatchAsync_ReturnsAppointmentUpdatedAndUpdatesAppointment_WhenDtoAndIdValid()
    {
        // Arrange
        (LogopedicContext context, _, _, _, _) = await new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment()
            .BuildAsync();

        Appointment appointment = await context.Appointments.FirstAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(appointment.TherapistId));

        IPatientService? patientService = Substitute.For<IPatientService>();
        patientService.ExistsForTherapistAsync(appointment.PatientId)
            .Returns(Task.FromResult(true));

        AppointmentService appointmentService = new(context, therapistService, patientService);

        PatchAppointmentDto dto = new() { Type = AppointmentType.Diagnosis, Status = AppointmentStatus.Cancelled };

        // Act
        OneOf<AppointmentUpdated, AppointmentNotFound, PatientNotFound, DurationZeroOrLess, TimeConflict> result =
            await appointmentService.PatchAsync(appointment.Id, dto);

        // Asset
        Assert.Equal(result.AsT0, result.Value);

        Appointment updatedAppointment = await context.Appointments
            .AsNoTracking()
            .SingleAsync(a => a.Id == appointment.Id);

        Assert.Equal(dto.Type, updatedAppointment.Type);
        Assert.Equal(dto.Status, updatedAppointment.Status);
    }

    [Fact]
    public async Task PatchAsync_ReturnsAppointmentNotFound_WhenIdInvalid()
    {
        // Arrange
        (LogopedicContext context, _, _, _, _) = await new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment()
            .BuildAsync();

        Appointment appointment = await context.Appointments.FirstAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(appointment.TherapistId));

        IPatientService? patientService = Substitute.For<IPatientService>();
        patientService.ExistsForTherapistAsync(appointment.PatientId)
            .Returns(Task.FromResult(true));

        AppointmentService appointmentService = new(context, therapistService, patientService);

        PatchAppointmentDto dto = new() { Type = AppointmentType.Diagnosis, Status = AppointmentStatus.Cancelled };

        const int invalidId = -1;

        // Act
        OneOf<AppointmentUpdated, AppointmentNotFound, PatientNotFound, DurationZeroOrLess, TimeConflict> result =
            await appointmentService.PatchAsync(invalidId, dto);

        // Asset
        Assert.Equal(result.AsT1, result.Value);

        // Assert nothing changed
        Appointment updatedAppointment = await context.Appointments
            .AsNoTracking()
            .SingleAsync(a => a.Id == appointment.Id);

        Assert.Equal(appointment.Type, updatedAppointment.Type);
        Assert.Equal(appointment.Status, updatedAppointment.Status);
    }

    [Fact]
    public async Task PatchAsync_ReturnsPatientNotFound_WhenPatientIdInvalid()
    {
        // Arrange
        (LogopedicContext context, _, _, _, _) = await new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment()
            .BuildAsync();

        Appointment appointment = await context.Appointments.FirstAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(appointment.TherapistId));

        IPatientService? patientService = Substitute.For<IPatientService>();
        patientService.ExistsForTherapistAsync(appointment.PatientId)
            .Returns(Task.FromResult(false));

        AppointmentService appointmentService = new(context, therapistService, patientService);

        PatchAppointmentDto dto = new() { Type = AppointmentType.Diagnosis, Status = AppointmentStatus.Cancelled };

        // Act
        OneOf<AppointmentUpdated, AppointmentNotFound, PatientNotFound, DurationZeroOrLess, TimeConflict> result =
            await appointmentService.PatchAsync(appointment.Id, dto);

        // Asset
        Assert.Equal(result.AsT2, result.Value);

        // Assert nothing changed
        Appointment updatedAppointment = await context.Appointments
            .AsNoTracking()
            .SingleAsync(a => a.Id == appointment.Id);

        Assert.Equal(appointment.Type, updatedAppointment.Type);
        Assert.Equal(appointment.Status, updatedAppointment.Status);
    }

    [Fact]
    public async Task PatchAsync_ReturnsDurationZeroOrLess_WhenDurationIsLessOrZero()
    {
        // Arrange
        (LogopedicContext context, _, _, _, _) = await new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment()
            .BuildAsync();

        Appointment appointment = await context.Appointments.FirstAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(appointment.TherapistId));

        IPatientService? patientService = Substitute.For<IPatientService>();
        patientService.ExistsForTherapistAsync(appointment.PatientId)
            .Returns(Task.FromResult(true));

        AppointmentService appointmentService = new(context, therapistService, patientService);

        PatchAppointmentDto dto = new()
        {
            DurationInMinutes = -5, Type = AppointmentType.Diagnosis, Status = AppointmentStatus.Cancelled
        };

        // Act
        OneOf<AppointmentUpdated, AppointmentNotFound, PatientNotFound, DurationZeroOrLess, TimeConflict> result =
            await appointmentService.PatchAsync(appointment.Id, dto);

        // Asset
        Assert.Equal(result.AsT3, result.Value);

        // Assert nothing changed
        Appointment updatedAppointment = await context.Appointments
            .AsNoTracking()
            .SingleAsync(a => a.Id == appointment.Id);

        Assert.Equal(appointment.Type, updatedAppointment.Type);
        Assert.Equal(appointment.Status, updatedAppointment.Status);
    }

    [Fact]
    public async Task PatchAsync_ReturnsTimeConflict_WhenNewStartTimeConflicts()
    {
        // Arrange
        (LogopedicContext context, _, _, _, _) = await new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment()
            .BuildAsync();

        Appointment appointment = await context.Appointments
            .Include(appointment => appointment.Therapist)
            .Include(appointment => appointment.Patient)
            .FirstAsync();

        Appointment conflictingAppointment = new()
        {
            StartTime = appointment.StartTime.AddMinutes(appointment.DurationInMinutes + 5),
            DurationInMinutes = appointment.DurationInMinutes,
            Type = appointment.Type,
            Status = appointment.Status,
            TherapistId = appointment.TherapistId,
            Therapist = appointment.Therapist,
            PatientId = appointment.PatientId,
            Patient = appointment.Patient
        };

        context.Appointments.Add(conflictingAppointment);
        await context.SaveChangesAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(appointment.TherapistId));

        IPatientService? patientService = Substitute.For<IPatientService>();
        patientService.ExistsForTherapistAsync(appointment.PatientId)
            .Returns(Task.FromResult(true));

        AppointmentService appointmentService = new(context, therapistService, patientService);

        PatchAppointmentDto dto = new()
        {
            StartTime = conflictingAppointment.StartTime.AddMinutes(-5),
            DurationInMinutes = 60,
            Type = AppointmentType.Diagnosis,
            Status = AppointmentStatus.Cancelled
        };

        // Act
        OneOf<AppointmentUpdated, AppointmentNotFound, PatientNotFound, DurationZeroOrLess, TimeConflict> result =
            await appointmentService.PatchAsync(appointment.Id, dto);

        // Asset
        Assert.Equal(result.AsT4, result.Value);

        // Assert nothing changed
        Appointment updatedAppointment = await context.Appointments
            .AsNoTracking()
            .SingleAsync(a => a.Id == appointment.Id);

        Assert.Equal(appointment.Type, updatedAppointment.Type);
        Assert.Equal(appointment.Status, updatedAppointment.Status);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrueAndDeletes_WhenIdIsValid()
    {
        // Arrange
        (LogopedicContext context, _, _, _, _) = await new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment()
            .BuildAsync();

        Appointment appointment = await context.Appointments.FirstAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(appointment.TherapistId));

        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService appointmentService = new(context, therapistService, patientService);

        // Act
        bool result = await appointmentService.DeleteAsync(appointment.Id);

        // Assert
        Assert.True(result);

        bool exists = await context.Appointments
            .AsNoTracking()
            .AnyAsync(a => a.Id == appointment.Id);

        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenIdInvalid()
    {
        // Arrange
        (LogopedicContext context, _, _, _, _) = await new TestDataBuilder().WithUser()
            .WithTherapist()
            .WithPatient()
            .WithAppointment()
            .BuildAsync();

        Appointment appointment = await context.Appointments.FirstAsync();

        ITherapistService? therapistService = Substitute.For<ITherapistService>();
        therapistService.GetCurrentTherapistIdOrThrowAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(appointment.TherapistId));

        IPatientService? patientService = Substitute.For<IPatientService>();

        AppointmentService appointmentService = new(context, therapistService, patientService);

        const int invalidId = -1;
        // Act
        bool result = await appointmentService.DeleteAsync(invalidId);

        // Assert
        Assert.False(result);

        bool exists = await context.Appointments
            .AsNoTracking()
            .AnyAsync(a => a.Id == appointment.Id);

        Assert.True(exists);
    }
}

public class AppointmentFilterTests
{
    private readonly List<Appointment> _appointments =
    [
        new() { Id = 1, PatientId = 10, Status = AppointmentStatus.Scheduled, Type = AppointmentType.Consultation },

        new() { Id = 2, PatientId = 11, Status = AppointmentStatus.Completed, Type = AppointmentType.Diagnosis },

        new() { Id = 3, PatientId = 10, Status = AppointmentStatus.Cancelled, Type = AppointmentType.Therapy },

        new() { Id = 4, PatientId = 12, Status = AppointmentStatus.Scheduled, Type = AppointmentType.Diagnosis }
    ];

    [Fact]
    public void Filters_ByPatientId()
    {
        // Arrange
        AppointmentQueryParameters query = new() { PatientId = 10 };

        // Act
        List<Appointment> result = ApplyFiltering(_appointments.AsQueryable(), query)
            .ToList();

        // Assert
        Assert.All(result, a => Assert.Equal(10, a.PatientId));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Filters_ByStatus()
    {
        AppointmentQueryParameters query = new() { Status = [AppointmentStatus.Completed] };

        List<Appointment> result = ApplyFiltering(_appointments.AsQueryable(), query)
            .ToList();

        Assert.Single(result);
        Assert.Equal(AppointmentStatus.Completed, result[0].Status);
    }

    [Fact]
    public void Filters_ByType()
    {
        AppointmentQueryParameters query = new() { Type = [AppointmentType.Diagnosis] };

        List<Appointment> result = ApplyFiltering(_appointments.AsQueryable(), query)
            .ToList();

        Assert.All(result, a => Assert.Equal(AppointmentType.Diagnosis, a.Type));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Filters_ByPatientId_AndStatus()
    {
        AppointmentQueryParameters query = new() { PatientId = 10, Status = [AppointmentStatus.Cancelled] };

        List<Appointment> result = ApplyFiltering(_appointments.AsQueryable(), query)
            .ToList();

        Assert.Single(result);
        Assert.Equal(3, result[0].Id);
    }

    // Bring in the tested method here for convenience
    private static IQueryable<Appointment> ApplyFiltering(IQueryable<Appointment> baseQuery,
        AppointmentQueryParameters query)
    {
        if (query.PatientId is not null)
        {
            baseQuery = baseQuery.Where(a => a.PatientId == query.PatientId.Value);
        }

        if (query.Status.Length > 0)
        {
            baseQuery = baseQuery.Where(a => query.Status.Contains(a.Status));
        }

        if (query.Type.Length > 0)
        {
            baseQuery = baseQuery.Where(a => query.Type.Contains(a.Type));
        }

        return baseQuery;
    }

    // Dummy appointment entity for tests
    private class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public AppointmentType Type { get; set; }
        public AppointmentStatus Status { get; set; }
    }
}

public class AppointmentSortingTests
{
    private readonly List<Appointment> _appointments =
    [
        new()
        {
            Id = 3,
            StartTime = new DateTimeOffset(2025, 9, 6, 9, 0, 0, TimeSpan.Zero),
            DurationInMinutes = 30,
            Type = AppointmentType.Diagnosis,
            Status = AppointmentStatus.Scheduled
        },
        new()
        {
            Id = 1,
            StartTime = new DateTimeOffset(2025, 9, 5, 14, 0, 0, TimeSpan.Zero),
            DurationInMinutes = 60,
            Type = AppointmentType.Consultation,
            Status = AppointmentStatus.Completed
        },
        new()
        {
            Id = 5,
            StartTime = new DateTimeOffset(2025, 9, 7, 10, 0, 0, TimeSpan.Zero),
            DurationInMinutes = 45,
            Type = AppointmentType.Diagnosis,
            Status = AppointmentStatus.Cancelled
        },
        new()
        {
            Id = 2,
            StartTime = new DateTimeOffset(2025, 9, 5, 9, 0, 0, TimeSpan.Zero),
            DurationInMinutes = 90,
            Type = AppointmentType.Consultation,
            Status = AppointmentStatus.Scheduled
        },
        new()
        {
            Id = 4,
            StartTime = new DateTimeOffset(2025, 9, 8, 11, 0, 0, TimeSpan.Zero),
            DurationInMinutes = 15,
            Type = AppointmentType.Therapy,
            Status = AppointmentStatus.Completed
        }
    ];

    public static TheoryData<AppointmentQueryParameters, int[]> SortingCases
    {
        get
        {
            TheoryData<AppointmentQueryParameters, int[]> data = new()
            {
                { new AppointmentQueryParameters { Sort = "id:desc" }, [5, 4, 3, 2, 1] },
                { new AppointmentQueryParameters { Sort = "id:asc" }, [1, 2, 3, 4, 5] },
                { new AppointmentQueryParameters { Sort = "startTime:desc" }, [4, 5, 3, 1, 2] },
                { new AppointmentQueryParameters { Sort = "startTime:asc" }, [2, 1, 3, 5, 4] },
                { new AppointmentQueryParameters { Sort = "duration:desc" }, [2, 1, 5, 3, 4] },
                { new AppointmentQueryParameters { Sort = "duration:asc" }, [4, 3, 5, 1, 2] },
                { new AppointmentQueryParameters { Sort = "type:asc" }, [1, 2, 3, 5, 4] },
                { new AppointmentQueryParameters { Sort = "type:desc" }, [4, 3, 5, 1, 2] },
                { new AppointmentQueryParameters { Sort = "status:asc" }, [5, 1, 4, 2, 3] },
                { new AppointmentQueryParameters { Sort = "status:desc" }, [2, 3, 1, 4, 5] }
            };

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(SortingCases))]
    public void Sorts_Correctly(AppointmentQueryParameters sortExpression, int[] expectedIds)
    {
        IQueryable<Appointment> data = _appointments.AsQueryable();

        Appointment[] result = ApplySorting(data, sortExpression)
            .ToArray();

        Assert.Equal(expectedIds, result.Select(a => a.Id));
    }

    private static IQueryable<Appointment> ApplySorting(IQueryable<Appointment> baseQuery,
        AppointmentQueryParameters query)
    {
        string sort = query.Sort;

        if (string.IsNullOrWhiteSpace(sort))
        {
            sort = "startTime:asc";
        }

        string[] clauses = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Dictionary<string, Expression<Func<Appointment, object?>>> map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["starttime"] = a => a.StartTime,
            ["id"] = a => a.Id,
            ["duration"] = a => a.DurationInMinutes,
            ["type"] = a => a.Type,
            ["status"] = a => a.Status
        };

        IOrderedQueryable<Appointment>? ordered = null;

        bool hasIdSort = false;

        foreach (string clause in clauses)
        {
            string[] parts = clause.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string field = parts[0];
            string direction = parts.Length > 1 ? parts[1] : "asc";

            // Skip fields which don't correspond to allowed sorting fields
            if (!map.TryGetValue(field, out Expression<Func<Appointment, object?>>? selector))
            {
                continue;
            }

            if (!hasIdSort && string.Equals(field, "id", StringComparison.OrdinalIgnoreCase))
            {
                hasIdSort = true;
            }

            if (ordered is null)
            {
                ordered = direction.Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? baseQuery.OrderByDescending(selector)
                    : baseQuery.OrderBy(selector);
            }
            else
            {
                ordered = direction.Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? ordered.ThenByDescending(selector)
                    : ordered.ThenBy(selector);
            }
        }

        if (ordered is null)
        {
            return baseQuery.OrderBy(a => a.StartTime)
                .ThenBy(a => a.Id);
        }

        if (!hasIdSort)
        {
            ordered = ordered.ThenBy(a => a.Id);
        }

        return ordered;
    }

    // Dummy appointment entity for tests
    private class Appointment
    {
        public int Id { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public int DurationInMinutes { get; set; }
        public AppointmentType Type { get; set; }
        public AppointmentStatus Status { get; set; }
    }
}
