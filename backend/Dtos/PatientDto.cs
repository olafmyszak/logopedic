namespace LogopedicBackend.Dtos;

public class PatientDto
{
    public required int Id { get; set; }
    public required string FullName { get; set; }
    public required DateTimeOffset DateOfBirth { get; set; }
    public required string ContactInfo { get; set; }
    public string? Notes { get; set; }
}