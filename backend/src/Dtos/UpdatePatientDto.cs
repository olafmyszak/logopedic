namespace LogopedicBackend.Dtos;

public class UpdatePatientDto
{
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ContactInfo { get; set; }
    public string? Notes { get; set; }
}
