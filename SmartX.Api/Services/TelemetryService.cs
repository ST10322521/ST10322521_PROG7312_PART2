using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;

public class TelemetryService
{
    public TelemetryStatus Process(TelemetryPacket<float> packet, double normalMin, double normalMax)
    {
        return ClassifyNumericValue(packet.Value, normalMin, normalMax);
    }

    public TelemetryStatus Process(TelemetryPacket<int> packet, int normalMin, int normalMax)
    {
        return ClassifyNumericValue(packet.Value, normalMin, normalMax);
    }

    public TelemetryStatus Process(TelemetryPacket<bool> packet)
    {
        return packet.Value ? TelemetryStatus.Normal : TelemetryStatus.Warning;
    }

    private static TelemetryStatus ClassifyNumericValue(double value, double normalMin, double normalMax)
    {
        if (value >= normalMin && value <= normalMax)
        {
            return TelemetryStatus.Normal;
        }
        var range = normalMax - normalMin;
        var warningMargin = range * 0.1; // 10% of the range

        if (value < normalMin && value >= normalMin - warningMargin)
        {
            return TelemetryStatus.Warning;
        }

        return TelemetryStatus.Critical;
    }
}