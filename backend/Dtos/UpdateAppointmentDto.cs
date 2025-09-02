using LogopedicBackend.Enums;

namespace LogopedicBackend.Dtos;

public class UpdateAppointmentDto
{
    public DateTimeOffset? StartTime { get; set; }
    public int? DurationInMinutes { get; set; }
    public AppointmentType? Type { get; set; }
    public AppointmentStatus? Status { get; set; }
    public int? PatientId { get; set; }
}