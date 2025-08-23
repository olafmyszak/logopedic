namespace LogopedicBackend.Models;

public class Patient
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string ContactInfo { get; set; }
    public string? Notes { get; set; }

    public required int TherapistId { get; set; }
    public required Therapist Therapist { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}