using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using LogopedicBackend.Dtos;
using LogopedicBackend.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace LogopedicBackend.IntegrationTests;

public class AppointmentsControllerTests(
    IntegrationTestWebAppFactory factory,
    ITestOutputHelper testOutputHelper) : BaseIntegrationTest(factory, testOutputHelper)
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string AppointmentsPath = "/api/appointments";

    [Fact]
    public async Task GetById_Returns200AndAppointmentDto_WhenAppointmentExists()
    {
        // Arrange
        var therapist = DataSeeder.Therapists.Single(t => t.UserId == DataSeeder.TestUser.Id);
        var validAppointmentId = therapist.Appointments.Select(a => a.Id).First();

        // Act
        var response = await Client.GetAsync($"{AppointmentsPath}/{validAppointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<AppointmentDto>(_jsonSerializerOptions);
        Assert.NotNull(dto);
        Assert.Equal(validAppointmentId, dto.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenAppointmentDoesNotExist()
    {
        // Arrange
        var nonExistingAppointmentId = DataSeeder.Appointments.Max(a => a.Id) + 1;

        // Act
        var response = await Client.GetAsync($"{AppointmentsPath}/{nonExistingAppointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns404_WhenAppointmentDoesNotBelongToCurrentTherapist()
    {
        // Arrange
        var notCurrentTherapist = DataSeeder.Therapists.Single(t => t.UserId != DataSeeder.TestUser.Id);
        var appointmentId = notCurrentTherapist.Appointments.Select(a => a.Id).First();

        // Act
        var response = await Client.GetAsync($"{AppointmentsPath}/{appointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns404_WhenIdIsInvalid()
    {
        // Arrange
        const string invalidId = "sdfsfdsdf";

        // Act
        var response = await Client.GetAsync($"{AppointmentsPath}/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Cancels_WhenCancellationRequested()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var id = DataSeeder.Appointments[0].Id;

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
        var query = $"?pageNumber={pageNumber}&pageSize={pageSize}&sort={Uri.EscapeDataString(sort)}";

        // Act
        var res = await Client.GetAsync($"{AppointmentsPath}{query}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var paged = await res.Content.ReadFromJsonAsync<PagedResultDto<AppointmentDto>>(_jsonSerializerOptions);

        Assert.NotNull(paged);
        Assert.Equal(pageNumber, paged.PageNumber);
        Assert.Equal(pageSize, paged.PageSize);
        Assert.NotNull(paged.Items);
        Assert.True(paged.Items.Count <= pageSize);

        if (paged.Items.Count >= 2)
        {
            var ordered = paged.Items
                .Select(i => i.StartTime)
                .OrderBy(d => d)
                .ToList();

            var actual = paged.Items
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
        var from = DateTimeOffset.UtcNow.AddDays(7);
        var to = DateTimeOffset.UtcNow;
        var query = $"?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}";

        // Act
        var res = await Client.GetAsync($"{AppointmentsPath}{query}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var details = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(_jsonSerializerOptions);

        Assert.NotNull(details);
        Assert.Equal("Invalid date range", details.Title);
        Assert.Equal(400, details.Status);
        Assert.Contains("dateRange", details.Errors.Keys);

        var expectedFrom = from.ToString("u");
        var expectedTo = to.ToString("u");
        var message = details.Errors["dateRange"].FirstOrDefault();
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
        var query = $"?pageSize={invalidPageSize}";

        // Act
        var res = await Client.GetAsync($"{AppointmentsPath}{query}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var details = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(_jsonSerializerOptions);

        Assert.NotNull(details);
        Assert.Equal("Invalid page size", details.Title);
        Assert.Equal(400, details.Status);
        Assert.Contains("pageSize", details.Errors.Keys);

        var message = details.Errors["pageSize"].FirstOrDefault();
        Assert.NotNull(message);
        Assert.Contains(invalidPageSize.ToString(), message);
        Assert.Contains(AppointmentQueryParameters.MinPageSize.ToString(), message);
        Assert.Contains(AppointmentQueryParameters.MaxPageSize.ToString(), message);
    }

    [Fact]
    public async Task Create_Returns201AndCreatesAppointment_WhenDtoIsValid()
    {
        // Arrange
        var therapist = DataSeeder.Therapists.Single(t => t.UserId == DataSeeder.TestUser.Id);

        var patientId = therapist.Patients.Select(p => p.Id).First();
        var maxDate = therapist.Appointments.Max(a => a.StartTime);

        var createDto = new CreateAppointmentDto
        {
            StartTime = maxDate.AddDays(5),
            DurationInMinutes = 60,
            Type = AppointmentType.Diagnosis,
            PatientId = patientId
        };

        // Act
        var response = await Client.PostAsJsonAsync(AppointmentsPath, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Check if appointment was created
        var appointmentDto = await response.Content.ReadFromJsonAsync<AppointmentDto>(_jsonSerializerOptions);
        Assert.NotNull(appointmentDto);

        var exists = await DbContext.Appointments.AnyAsync(a => a.Id == appointmentDto.Id);
        Assert.True(exists);
    }

    [Fact]
    public async Task Create_Returns404AndProblemDetails_WhenDtoIsValidAndPatientIdIsInvalid()
    {
        // Arrange
        var therapist = DataSeeder.Therapists.Single(t => t.UserId == DataSeeder.TestUser.Id);
        const int invalidPatientId = -1;
        var maxDate = therapist.Appointments.Max(a => a.StartTime);

        var createDto = new CreateAppointmentDto
        {
            StartTime = maxDate.AddDays(5),
            DurationInMinutes = 60,
            Type = AppointmentType.Diagnosis,
            PatientId = invalidPatientId
        };

        // Act
        var response = await Client.PostAsJsonAsync(AppointmentsPath, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Create_Returns404AndProblemDetails_WhenDtoIsValidAndPatientIdBelongsToAnotherTherapist()
    {
        // Arrange
        var notCurrentTherapist = DataSeeder.Therapists.Single(t => t.UserId != DataSeeder.TestUser.Id);
        var invalidPatientId = notCurrentTherapist.Patients.Select(p => p.Id).First();
        var maxDate = notCurrentTherapist.Appointments.Max(a => a.StartTime);

        var createDto = new CreateAppointmentDto
        {
            StartTime = maxDate.AddDays(5),
            DurationInMinutes = 60,
            Type = AppointmentType.Diagnosis,
            PatientId = invalidPatientId
        };

        // Act
        var response = await Client.PostAsJsonAsync(AppointmentsPath, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Create_Returns409AndProblemDetails_WhenAppointmentTimeCollidesAndPatientIdIsValid()
    {
        // Arrange
        var therapist = DataSeeder.Therapists.Single(t => t.UserId == DataSeeder.TestUser.Id);
        var patientId = therapist.Patients.Select(p => p.Id).First();
        var minDate = therapist.Appointments.Min(a => a.StartTime);

        var createDto = new CreateAppointmentDto
        {
            StartTime = minDate.AddHours(-1), // Conflicting time
            DurationInMinutes = 300,
            Type = AppointmentType.Diagnosis,
            PatientId = patientId
        };

        // Act
        var response = await Client.PostAsJsonAsync(AppointmentsPath, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Create_Returns400AndValidationProblemDetails_WhenAppointmentDtoIsInvalidAndPatientIdIsValid()
    {
        // Arrange
        var therapist = DataSeeder.Therapists.Single(t => t.UserId == DataSeeder.TestUser.Id);
        var patientId = therapist.Patients.Select(p => p.Id).First();
        var maxDate = therapist.Appointments.Max(a => a.StartTime);

        var createDto = new CreateAppointmentDto
        {
            StartTime = maxDate.AddHours(1),
            DurationInMinutes = -5, // Invalid duration
            Type = AppointmentType.Diagnosis,
            PatientId = patientId
        };

        // Act
        var response = await Client.PostAsJsonAsync(AppointmentsPath, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Patch_ReturnsNoContentAndUpdatesAppointment_WhenIdAndDtoValid()
    {
        // Arrange
        var therapist = DataSeeder.Therapists.Single(t => t.UserId == DataSeeder.TestUser.Id);
        var oldAppointment = therapist.Appointments.First();

        var dto = new PatchAppointmentDto
        {
            DurationInMinutes = oldAppointment.DurationInMinutes - 5,
            Status = AppointmentStatus.NoShow,
            Type = AppointmentType.Therapy,
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"{AppointmentsPath}/{oldAppointment.Id}", dto);

        // Response
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Use AsNoTracking() so EF doesn't return the already-tracked (stale) entity.
        // This forces a fresh read from the database after the PATCH.
        var updatedAppointment = await DbContext.Appointments
            .AsNoTracking()
            .SingleAsync(a => a.Id == oldAppointment.Id);

        Assert.NotNull(updatedAppointment);
        Assert.Equal(dto.DurationInMinutes, updatedAppointment.DurationInMinutes);
        Assert.Equal(dto.Status, updatedAppointment.Status);
        Assert.Equal(dto.Type, updatedAppointment.Type);
    }

    [Fact]
    public async Task Patch_Returns404AndProblemDetails_WhenIdDoesNotExist()
    {
        // Arrange
        const int nonExistingId = -1;

        var dto = new PatchAppointmentDto();

        // Act
        var response = await Client.PatchAsJsonAsync($"{AppointmentsPath}/{nonExistingId}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Patch_Returns404AndProblemDetails_WhenIdValidAndPatientIdInvalid()
    {
        // Arrange
        var therapist = DataSeeder.Therapists.Single(t => t.UserId == DataSeeder.TestUser.Id);
        var appointmentId = therapist.Appointments.Select(a => a.Id).First();

        var dto = new PatchAppointmentDto
        {
            PatientId = -1
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"{AppointmentsPath}/{appointmentId}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Patch_Returns400AndValidationProblemDetails_WhenIdValidAndDtoInvalid()
    {
        // Arrange
        var therapist = DataSeeder.Therapists.Single(t => t.UserId == DataSeeder.TestUser.Id);
        var appointmentId = therapist.Appointments.Select(a => a.Id).First();

        var dto = new PatchAppointmentDto
        {
            DurationInMinutes = -5
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"{AppointmentsPath}/{appointmentId}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }

    [Fact]
    public async Task Patch_Returns409AndProblemDetails_WhenStartTimeConflicting()
    {
        // Arrange
        var therapist = DataSeeder.Therapists.Single(t => t.UserId == DataSeeder.TestUser.Id);
        var appointmentId = therapist.Appointments.Select(a => a.Id).First();
        var minDate = therapist.Appointments.Min(a => a.StartTime);

        var dto = new PatchAppointmentDto
        {
            StartTime = minDate.AddHours(-1), // Conflicting date
            DurationInMinutes = 300
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"{AppointmentsPath}/{appointmentId}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonSerializerOptions);
        Assert.NotNull(problemDetails);
    }
}