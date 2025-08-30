using LogopedicBackend.Dtos;

namespace LogopedicBackend.Services.Results;

public record AppointmentCreated(AppointmentDto Appointment);

public record PatientNotFound;

public record TimeConflict;

public record AppointmentUpdated;

public record AppointmentNotFound;

public record NoChanges;