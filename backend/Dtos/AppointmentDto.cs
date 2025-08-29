using LogopedicBackend.Enums;

namespace LogopedicBackend.Dtos;

public class AppointmentDto
{
    public required int Id { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required int DurationInMinutes { get; set; }
    public required AppointmentType Type { get; set; }
    public required AppointmentStatus Status { get; set; }
}