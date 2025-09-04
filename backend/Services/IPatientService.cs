using LogopedicBackend.Dtos;
using LogopedicBackend.Models;
using LogopedicBackend.Services.Results.Common.NotFound;
using LogopedicBackend.Services.Results.Common.Paging;
using LogopedicBackend.Services.Results.Patients;
using OneOf;

namespace LogopedicBackend.Services;

public interface IPatientService
{
    Task<bool> ExistsForTherapistAsync(int patientId, CancellationToken ct = default);
    Task<Patient?> GetByIdAsync(int patientId, CancellationToken ct = default);
    Task<PatientDto?> GetPatientDtoByIdAsync(int patientId, CancellationToken ct = default);
    Task<IReadOnlyList<PatientDto>> GetAllAsync(CancellationToken ct = default);
    Task<OneOf<PagedResultDto<PatientDto>, InvalidPageSizeError>> QueryAsync(PatientQueryParameters query, CancellationToken ct = default);
    Task<PatientCreated> CreateAsync(CreatePatientDto dto, CancellationToken ct = default);

    Task<OneOf<PatientUpdated, PatientNotFound>> UpdateAsync(int id, UpdatePatientDto dto,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}