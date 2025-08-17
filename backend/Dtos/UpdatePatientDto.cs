namespace LogopedicBackend.Dtos;

public class UpdatePatientDto
{
    public string? FullName { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public string? ContactInfo { get; set; }
    public string? Notes { get; set; }
    public int? TherapistId { get; set; }
}