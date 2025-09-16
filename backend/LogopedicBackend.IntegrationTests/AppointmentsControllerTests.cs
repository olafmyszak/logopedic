using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using LogopedicBackend.Dtos;
using LogopedicBackend.Enums;
using LogopedicBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace LogopedicBackend.IntegrationTests;

[Collection("Database collection")]
public class AppointmentsControllerTests(IntegrationTestWebAppFactory factory, ITestOutputHelper testOutputHelper)
    : BaseIntegrationTest(factory, testOutputHelper)
{
    private const string AppointmentsPath = "/api/appointments";

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task GetById_Returns200AndAppointmentDto_WhenAppointmentExists()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists
            .Include(therapist => therapist.Appointments)
            .SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);
        int validAppointmentId = therapist.Appointments
            .Select(a => a.Id)
            .First();

        // Act
        HttpResponseMessage response = await Client.GetAsync($"{AppointmentsPath}/{validAppointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AppointmentDto? dto = await response.Content.ReadFromJsonAsync<AppointmentDto>(_jsonSerializerOptions);
        Assert.NotNull(dto);
        Assert.Equal(validAppointmentId, dto.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenAppointmentDoesNotExist()
    {
        // Arrange
        const int nonExistingAppointmentId = -1;

        // Act
        HttpResponseMessage response = await Client.GetAsync($"{AppointmentsPath}/{nonExistingAppointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns404_WhenAppointmentDoesNotBelongToCurrentTherapist()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists.SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);
        int appointmentId = await DbContext.Appointments
            .Where(a => a.TherapistId != therapist.Id)
            .Select(a => a.Id)
            .FirstAsync();

        // Act
        HttpResponseMessage response = await Client.GetAsync($"{AppointmentsPath}/{appointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns404_WhenIdIsInvalid()
    {
        // Arrange
        const string invalidId = "sdfsfdsdf";

        // Act
        HttpResponseMessage response = await Client.GetAsync($"{AppointmentsPath}/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Cancels_WhenCancellationRequested()
    {
        // Arrange
        CancellationTokenSource cts = new();
        int id = await DbContext.Appointments
            .Select(a => a.Id)
            .FirstAsync(cts.Token);

        // Act
        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await Client.GetAsync($"{AppointmentsPath}/{id}", cts.Token));
    }


    [Fact]
    public async Task Query_ReturnsPagedResult_WhenQueryIsValid()
    {
        // Arrange
        const int pageNumber = 1;
        const int pageSize = 5;
        const string sort = "startTime:asc";
        string query = $"?pageNumber={pageNumber}&pageSize={pageSize}&sort={Uri.EscapeDataString(sort)}";

        // Act
        HttpResponseMessage res = await Client.GetAsync($"{AppointmentsPath}{query}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        PagedResultDto<AppointmentDto>? paged =
            await res.Content.ReadFromJsonAsync<PagedResultDto<AppointmentDto>>(_jsonSerializerOptions);

        Assert.NotNull(paged);
        Assert.Equal(pageNumber, paged.PageNumber);
        Assert.Equal(pageSize, paged.PageSize);
        Assert.NotNull(paged.Items);
        Assert.True(paged.Items.Count <= pageSize);

        if (paged.Items.Count >= 2)
        {
            List<DateTimeOffset> ordered = paged.Items
                .Select(i => i.StartTime)
                .OrderBy(d => d)
                .ToList();

            List<DateTimeOffset> actual = paged.Items
                .Select(i => i.StartTime)
                .ToList();

            Assert.Equal(ordered, actual);
        }

        Assert.True(paged.TotalCount >= paged.Items.Count);
    }

    [Fact]
    public async Task Query_ReturnsValidationProblem_WhenDateRangeIsInvalid()
    {
        // Arrange
        DateTimeOffset from = DateTimeOffset.UtcNow.AddDays(7);
        DateTimeOffset to = DateTimeOffset.UtcNow;
        string query = $"?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}";

        // Act
        HttpResponseMessage res = await Client.GetAsync($"{AppointmentsPath}{query}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        ValidationProblemDetails? details =
            await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(_jsonSerializerOptions);

        Assert.NotNull(details);
        Assert.Equal("Invalid date range", details.Title);
        Assert.Equal(400, details.Status);
        Assert.Contains("dateRange", details.Errors.Keys);

        string expectedFrom = from.ToString("u");
        string expectedTo = to.ToString("u");
        string? message = details.Errors["dateRange"]
            .FirstOrDefault();
        Assert.NotNull(message);
        Assert.Contains(expectedFrom, message);
        Assert.Contains(expectedTo, message);
    }

    [Theory]
    [InlineData(AppointmentQueryParameters.MinPageSize - 1)]
    [InlineData(AppointmentQueryParameters.MaxPageSize + 1)]
    public async Task Query_ReturnsValidationProblem_WhenPageSizeIsInvalid(int invalidPageSize)
    {
        // Arrange
        string query = $"?pageSize={invalidPageSize}";

        // Act
        HttpResponseMessage res = await Client.GetAsync($"{AppointmentsPath}{query}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        ValidationProblemDetails? details =
            await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(_jsonSerializerOptions);

        Assert.NotNull(details);
        Assert.Equal("Invalid page size", details.Title);
        Assert.Equal(400, details.Status);
        Assert.Contains("pageSize", details.Errors.Keys);

        string? message = details.Errors["pageSize"]
            .FirstOrDefault();
        Assert.NotNull(message);
        Assert.Contains(invalidPageSize.ToString(), message);
        Assert.Contains(AppointmentQueryParameters.MinPageSize.ToString(), message);
        Assert.Contains(AppointmentQueryParameters.MaxPageSize.ToString(), message);
    }

    [Fact]
    public async Task Create_Returns201AndCreatesAppointment_WhenDtoIsValid()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists
            .Include(therapist => therapist.Patients)
            .Include(therapist => therapist.Appointments)
            .SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);

        int patientId = therapist.Patients
            .Select(p => p.Id)
            .First();
        DateTimeOffset maxDate = therapist.Appointments.Max(a => a.StartTime);

        CreateAppointmentDto createDto = new()
        {
            StartTime = maxDate.AddDays(5),
            DurationInMinutes = 60,
            Type = AppointmentType.Diagnosis,
            PatientId = patientId
        };

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(AppointmentsPath, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Check if appointment was created
        AppointmentDto? appointmentDto =
            await response.Content.ReadFromJsonAsync<AppointmentDto>(_jsonSerializerOptions);
        Assert.NotNull(appointmentDto);

        bool exists = await DbContext.Appointments.AnyAsync(a => a.Id == appointmentDto.Id);
        Assert.True(exists);
    }

    [Fact]
    public async Task Create_Returns404AndProblemDetails_WhenDtoIsValidAndPatientIdIsInvalid()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists
            .Include(therapist => therapist.Appointments)
            .SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);
        const int invalidPatientId = -1;
        DateTimeOffset maxDate = therapist.Appointments.Max(a => a.StartTime);

        CreateAppointmentDto createDto = new()
        {
            StartTime = maxDate.AddDays(5),
            DurationInMinutes = 60,
            Type = AppointmentType.Diagnosis,
            PatientId = invalidPatientId
        };

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(AppointmentsPath, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Create_Returns404AndProblemDetails_WhenDtoIsValidAndPatientIdBelongsToAnotherTherapist()
    {
        // Arrange
        Therapist notCurrentTherapist = await DbContext.Therapists
            .Include(therapist => therapist.Patients)
            .Include(therapist => therapist.Appointments)
            .SingleAsync(t => t.UserId != DataSeeder.TestUser.Id);
        int invalidPatientId = notCurrentTherapist.Patients
            .Select(p => p.Id)
            .First();
        DateTimeOffset maxDate = notCurrentTherapist.Appointments.Max(a => a.StartTime);

        CreateAppointmentDto createDto = new()
        {
            StartTime = maxDate.AddDays(5),
            DurationInMinutes = 60,
            Type = AppointmentType.Diagnosis,
            PatientId = invalidPatientId
        };

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(AppointmentsPath, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Create_Returns409AndProblemDetails_WhenAppointmentTimeCollidesAndPatientIdIsValid()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists
            .Include(therapist => therapist.Patients)
            .Include(therapist => therapist.Appointments)
            .SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);
        int patientId = therapist.Patients
            .Select(p => p.Id)
            .First();
        DateTimeOffset minDate = therapist.Appointments.Min(a => a.StartTime);

        CreateAppointmentDto createDto = new()
        {
            StartTime = minDate.AddHours(-1), // Conflicting time
            DurationInMinutes = 300,
            Type = AppointmentType.Diagnosis,
            PatientId = patientId
        };

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(AppointmentsPath, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        ProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Create_Returns400AndValidationProblemDetails_WhenAppointmentDtoIsInvalidAndPatientIdIsValid()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists
            .Include(therapist => therapist.Patients)
            .Include(therapist => therapist.Appointments)
            .SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);
        int patientId = therapist.Patients
            .Select(p => p.Id)
            .First();
        DateTimeOffset maxDate = therapist.Appointments.Max(a => a.StartTime);

        CreateAppointmentDto createDto = new()
        {
            StartTime = maxDate.AddHours(1),
            DurationInMinutes = -5, // Invalid duration
            Type = AppointmentType.Diagnosis,
            PatientId = patientId
        };

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(AppointmentsPath, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ValidationProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Patch_Returns204AndUpdatesAppointment_WhenIdAndDtoValid()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists
            .Include(therapist => therapist.Appointments)
            .SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);
        Appointment oldAppointment = therapist.Appointments.First();

        PatchAppointmentDto dto = new() { Status = AppointmentStatus.NoShow, Type = AppointmentType.Therapy };

        // Act
        HttpResponseMessage response = await Client.PatchAsJsonAsync($"{AppointmentsPath}/{oldAppointment.Id}", dto);

        // Response
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Use AsNoTracking() so EF doesn't return the already-tracked (stale) entity.
        // This forces a fresh read from the database after the PATCH.
        Appointment updatedAppointment = await DbContext.Appointments
            .AsNoTracking()
            .SingleAsync(a => a.Id == oldAppointment.Id);

        Assert.NotNull(updatedAppointment);
        Assert.Equal(dto.Status, updatedAppointment.Status);
        Assert.Equal(dto.Type, updatedAppointment.Type);
    }

    [Fact]
    public async Task Patch_Returns404AndProblemDetails_WhenIdDoesNotExist()
    {
        // Arrange
        const int nonExistingId = -1;

        PatchAppointmentDto dto = new();

        // Act
        HttpResponseMessage response = await Client.PatchAsJsonAsync($"{AppointmentsPath}/{nonExistingId}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Patch_Returns404AndProblemDetails_WhenIdValidAndPatientIdInvalid()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists
            .Include(therapist => therapist.Appointments)
            .SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);
        Appointment appointment = therapist.Appointments.First();

        PatchAppointmentDto dto = new() { PatientId = -1 };

        // Act
        HttpResponseMessage response = await Client.PatchAsJsonAsync($"{AppointmentsPath}/{appointment.Id}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);

        // Assert appointment was not changed
        Appointment? updatedAppointment = await DbContext.Appointments
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == appointment.Id);
        Assert.NotNull(updatedAppointment);
        Assert.Equal(appointment.DurationInMinutes, updatedAppointment.DurationInMinutes);
        Assert.Equal(appointment.Status, updatedAppointment.Status);
        Assert.Equal(appointment.Type, updatedAppointment.Type);
    }

    [Fact]
    public async Task Patch_Returns400AndValidationProblemDetails_WhenIdValidAndDtoInvalid()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists
            .Include(therapist => therapist.Appointments)
            .SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);
        Appointment appointment = therapist.Appointments.First();

        PatchAppointmentDto dto = new() { DurationInMinutes = -5 };

        // Act
        HttpResponseMessage response = await Client.PatchAsJsonAsync($"{AppointmentsPath}/{appointment.Id}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ValidationProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);

        // Assert appointment was not changed
        Appointment? updatedAppointment = await DbContext.Appointments
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == appointment.Id);
        Assert.NotNull(updatedAppointment);
        Assert.Equal(appointment.DurationInMinutes, updatedAppointment.DurationInMinutes);
        Assert.Equal(appointment.Status, updatedAppointment.Status);
    }

    [Fact]
    public async Task Patch_Returns409AndProblemDetails_WhenStartTimeConflicting()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists
            .Include(therapist => therapist.Appointments)
            .SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);
        Appointment appointment = therapist.Appointments.First();
        DateTimeOffset minDate = therapist.Appointments.Min(a => a.StartTime);

        PatchAppointmentDto dto = new()
        {
            StartTime = minDate.AddHours(-1), // Conflicting date
            DurationInMinutes = 300
        };

        // Act
        HttpResponseMessage response = await Client.PatchAsJsonAsync($"{AppointmentsPath}/{appointment.Id}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        ProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);

        // Assert appointment was not changed
        Appointment? updatedAppointment = await DbContext.Appointments
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == appointment.Id);
        Assert.NotNull(updatedAppointment);
        Assert.Equal(appointment.DurationInMinutes, updatedAppointment.DurationInMinutes);
        Assert.Equal(appointment.Status, updatedAppointment.Status);
    }

    [Fact]
    public async Task Delete_Returns204AndDeletesAppointment_WhenIdIsValid()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists
            .Include(therapist => therapist.Appointments)
            .SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);
        int appointmentId = therapist.Appointments
            .Select(a => a.Id)
            .First();

        // Act
        HttpResponseMessage response = await Client.DeleteAsync($"{AppointmentsPath}/{appointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        bool appointmentExists = await DbContext.Appointments
            .AsNoTracking()
            .AnyAsync(a => a.Id == appointmentId);
        Assert.False(appointmentExists);
    }

    [Fact]
    public async Task Delete_Returns404_WhenIdDoesNotExist()
    {
        // Arrange
        const int appointmentId = -1;

        // Act
        HttpResponseMessage response = await Client.DeleteAsync($"{AppointmentsPath}/{appointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Delete_Returns404_WhenIdDoesNotBelongToTherapist()
    {
        // Arrange
        Therapist therapist = await DbContext.Therapists.SingleAsync(t => t.UserId == DataSeeder.TestUser.Id);
        int appointmentId = await DbContext.Appointments
            .Where(a => a.TherapistId != therapist.Id)
            .Select(a => a.Id)
            .FirstAsync();

        // Act
        HttpResponseMessage response = await Client.DeleteAsync($"{AppointmentsPath}/{appointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }
}
