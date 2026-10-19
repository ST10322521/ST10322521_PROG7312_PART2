namespace SmartX.Api.Models;

public sealed record UserInteraction(
    string Action,
    string? Query,
    string? DeviceId,
    string? Command,
    DateTime Timestamp
);