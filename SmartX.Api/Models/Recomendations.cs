namespace SmartX.Api.Models;

public sealed record Recommendation(
    string DeviceId,
    string Title,
    string Reason,
    string SuggestedCommand,
    string Priority);