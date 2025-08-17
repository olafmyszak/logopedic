namespace LogopedicBackend.Dtos;

public class CreatePatientDto
{
    public required string FullName { get; set; }
    public required DateTimeOffset DateOfBirth { get; set; }
    public required string ContactInfo { get; set; }
    public string? Notes { get; set; }
    public required int TherapistId { get; set; }
}