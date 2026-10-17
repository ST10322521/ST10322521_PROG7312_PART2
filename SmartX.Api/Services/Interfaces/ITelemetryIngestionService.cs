using SmartX.Shared.DTOs;

namespace SmartX.Api.Services.Interfaces;

public interface ITelemetryIngestionService
{
    Task<TelemetryResponse?> ReceiveTelemetryAsync(
        TelemetryRequest request);
}