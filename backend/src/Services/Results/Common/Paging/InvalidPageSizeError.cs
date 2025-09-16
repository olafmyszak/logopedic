namespace LogopedicBackend.Services.Results.Common.Paging;

public record InvalidPageSizeError(int Requested, int Min, int Max);
