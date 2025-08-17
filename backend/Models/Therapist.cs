namespace LogopedicBackend.Models;

public class Therapist
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }

    public ICollection<Patient> Patients { get; set; }
    public ICollection<Appointment> Appointments { get; set; }
}