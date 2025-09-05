namespace LogopedicBackend.Services.Results.Appointments;

public record InvalidDateRangeError(DateTimeOffset From, DateTimeOffset To);