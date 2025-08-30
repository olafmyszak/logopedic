using LogopedicBackend.Dtos;
using LogopedicBackend.Services.Results;
using OneOf;

namespace LogopedicBackend.Services;

public interface IAppointmentService
{
    Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDto>> GetAllAsync(CancellationToken ct = default);

    Task<OneOf<AppointmentCreated, PatientNotFound, TimeConflict>> CreateAsync(
        CreateAppointmentDto dto,
        CancellationToken ct = default);

    Task<OneOf<AppointmentUpdated, AppointmentNotFound, NoChanges, PatientNotFound>> UpdateAsync(
        int id,
        UpdateAppointmentDto dto,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}