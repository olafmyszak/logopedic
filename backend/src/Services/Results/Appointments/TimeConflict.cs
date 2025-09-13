using LogopedicBackend.Dtos;

namespace LogopedicBackend.Services.Results.Appointments;

public record TimeConflict(
    DateTimeOffset RequestedStart,
    DateTimeOffset RequestedEnd,
    IReadOnlyList<AppointmentDto> Conflicts);
