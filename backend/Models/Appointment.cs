using LogopedicBackend.Models.Enums;

namespace LogopedicBackend.Models;

public class Appointment
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public int DurationInMinutes { get; set; }
    public AppointmentType Type { get; set; }
    public AppointmentStatus Status { get; set; }
    
    public Patient Patient { get; set; }
    
}