using LogopedicBackend.Models;
using LogopedicBackend.Models.Enums;

namespace LogopedicBackend.Dtos;

public class AppointmentDto
{
    public int Id { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public int DurationInMinutes { get; set; }
    public string Type { get; set; }
    public string Status { get; set; }
}