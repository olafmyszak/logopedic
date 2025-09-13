namespace LogopedicBackend.Dtos;

public class PatientQueryParameters
{
    // Paging
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    // Sorting: comma-separated "field:dir" pairs, e.g. "fullName:asc,id:desc"
    // Acceptable values: id, fullname, contactinfo
    public string Sort { get; init; } = "fullName:asc";

    // Search
    public string? Search { get; init; }
}
