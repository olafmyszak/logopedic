namespace LogopedicBackend.Models;

public class  Patient
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public DateTimeOffset DateOfBirth { get; set; }
    public string ContactInfo { get; set; }
    public string Notes { get; set; }
    
    public ICollection<Appointment> Appointments { get; set; }  
}