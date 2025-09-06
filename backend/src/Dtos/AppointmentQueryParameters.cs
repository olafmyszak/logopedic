using LogopedicBackend.Enums;

namespace LogopedicBackend.Dtos;

public class AppointmentQueryParameters
{
    // Range
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }

    // Paging
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 200;

    // Sorting: comma-separated "field:dir" pairs, e.g. "startTime:asc,id:desc"
    public string Sort { get; init; } = "startTime:asc";

    // Filtering
    public int? PatientId { get; init; }

    // Allow repeated query params: ?status=Scheduled&status=Cancelled
    public AppointmentStatus[]? Status { get; init; }
    public AppointmentType[]? Type { get; init; }
}