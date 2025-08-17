using LogopedicBackend.Models.Enums;

namespace LogopedicBackend.Models;

public class Appointment
{
    public int Id { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required int DurationInMinutes { get; set; }
    public required AppointmentType Type { get; set; }
    public required AppointmentStatus Status { get; set; }

    public required int TherapistId { get; set; }
    public Therapist Therapist { get; set; }

    public required int PatientId { get; set; }
    public Patient Patient { get; set; }
}