using LogopedicBackend.Models;
using LogopedicBackend.Models.Enums;

namespace LogopedicBackend.Dtos;

public class AppointmentDto
{
    public required int Id { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required int DurationInMinutes { get; set; }
    public required string Type { get; set; }
    public required string Status { get; set; }
}