using LogopedicBackend.Dtos;
using LogopedicBackend.Services.Results.Appointments;
using LogopedicBackend.Services.Results.Common.NotFound;
using LogopedicBackend.Services.Results.Common.Paging;
using OneOf;

namespace LogopedicBackend.Services;

public interface IAppointmentService
{
    Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDto>> GetAllAsync(CancellationToken ct = default);

    Task<OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError>> QueryAsync(
        AppointmentQueryParameters query, CancellationToken ct = default);

    // TODO: Validate dto
    Task<OneOf<AppointmentCreated, PatientNotFound, TimeConflict>> CreateAsync(
        CreateAppointmentDto dto,
        CancellationToken ct = default);

    // TODO: Validate dto
    Task<OneOf<AppointmentUpdated, AppointmentNotFound, PatientNotFound>> UpdateAsync(
        int id,
        UpdateAppointmentDto dto,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}