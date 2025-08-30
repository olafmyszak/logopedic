using LogopedicBackend.Enums;

namespace LogopedicBackend.Dtos;

public class CreateAppointmentDto
{
    public required DateTimeOffset StartTime { get; set; }
    public required int DurationInMinutes { get; set; }
    public required AppointmentType Type { get; set; }
    public required AppointmentStatus Status { get; set; }
    public required int PatientId { get; set; }
}