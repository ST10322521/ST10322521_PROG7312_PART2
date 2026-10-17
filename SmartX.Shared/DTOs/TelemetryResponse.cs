using SmartX.Shared.Enums;

namespace SmartX.Shared.DTOs;

public sealed record TelemetryResponse(
    string DeviceId,
    DateTime Timestamp,
    TelemetryType TelemetryType,
    object Value,
    TelemetryStatus Status);