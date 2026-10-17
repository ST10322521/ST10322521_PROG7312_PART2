namespace SmartX.Api.Results;

public enum ServiceStatus
{
    Success,
    ValidationFailure,
    NotFound,
    Conflict,
    InvalidRequest
}

public sealed record ServiceResult<T>(
    ServiceStatus Status,
    T? Data = default,
    string? Error = null,
    IReadOnlyList<string>? Errors = null);