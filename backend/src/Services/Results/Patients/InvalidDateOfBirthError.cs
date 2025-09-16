namespace LogopedicBackend.Services.Results.Patients;

public record InvalidDateOfBirthError(DateOnly Requested, DateOnly Min, DateOnly Max);
