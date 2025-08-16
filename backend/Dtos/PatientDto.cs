namespace LogopedicBackend.Dtos;

public class PatientDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public DateTimeOffset DateOfBirth { get; set; }
    public string ContactInfo { get; set; }
    public string Notes { get; set; }
    public List<AppointmentDto> Appointments { get; set; }  
}