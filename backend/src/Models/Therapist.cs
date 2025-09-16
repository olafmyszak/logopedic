namespace LogopedicBackend.Models;

public class Therapist
{
    public int Id { get; set; }
    public required string FullName { get; set; }

    public required string UserId { get; set; }
    public required User User { get; set; }

    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
