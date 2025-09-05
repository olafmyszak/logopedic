using LogopedicBackend.Enums;

namespace LogopedicBackend.Dtos;

public class PatchAppointmentDto
{
    public DateTimeOffset? StartTime { get; set; }
    public int? DurationInMinutes { get; set; }
    public AppointmentType? Type { get; set; }
    public AppointmentStatus? Status { get; set; }
    public int? PatientId { get; set; }
}