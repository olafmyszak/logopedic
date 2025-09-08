using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using LogopedicBackend.Dtos;

namespace LogopedicBackend.IntegrationTests;

public class AppointmentsControllerTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    // private readonly HttpClient _client = factory.CreateClient();

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task GetById_Returns200AndAppointmentDto_WhenAppointmentExists()
    {
        // Arrange
        var validAppointmentId = DataSeeder.Appointments[0].Id;

        // Act
        var response = await Client.GetAsync($"/api/Appointments/{validAppointmentId}");

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
        var response = await Client.GetAsync($"/api/Appointments/{nonExistingAppointmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns404_WhenIdIsInvalid()
    {
        // Arrange
        const string invalidId = "sdfsfdsdf";

        // Act
        var response = await Client.GetAsync($"/api/Appointments/{invalidId}");

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
            await Client.GetAsync($"api/Appointments/{id}", cts.Token));
    }
}