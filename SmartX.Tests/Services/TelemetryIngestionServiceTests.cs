using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Api.Data;
using SmartX.Api.Services;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Tests.Services;

public class TelemetryIngestionServiceTests
{
    private static DbContextOptions<SmartXDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<SmartXDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static async Task<(
        SmartXDbContext Context,
        TelemetryIngestionService Service,
        ServiceProvider Provider)> CreateServiceAsync(Sensor? sensor = null)
    {
        var options = CreateOptions();

        var context = new SmartXDbContext(options);

        if (sensor is not null)
        {
            context.Sensors.Add(sensor);
            await context.SaveChangesAsync();
        }

        var services = new ServiceCollection();

        services.AddLogging();

        services.AddScoped<SmartXDbContext>(_ =>
            new SmartXDbContext(options));

        services.AddSingleton<LiveSensorRegistry>();
        services.AddSingleton<TelemetryService>();
        services.AddSingleton<TelemetryHistoryService>();
        services.AddSingleton<TelemetryReadingState>();

        var provider = services.BuildServiceProvider();

        var registry =
            provider.GetRequiredService<LiveSensorRegistry>();

        await registry.RefreshAsync();

        var service = new TelemetryIngestionService(
            provider.GetRequiredService<TelemetryService>(),
            registry,
            provider.GetRequiredService<TelemetryHistoryService>(),
            provider.GetRequiredService<TelemetryReadingState>());

        return (context, service, provider);
    }

    [Fact]
    public async Task ReceiveTelemetry_PowerIntegerValue_ReturnsNormal()
    {
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            DeviceId = "TEST-POWER-001",
            MacAddress = "AA:BB:CC:DD:EE:20",
            Location = "Test Facility / Power",
            Category = SensorCategory.Power,
            TelemetryType = TelemetryType.Power,
            SamplingIntervalSeconds = 5,
            NormalMin = 500,
            NormalMax = 2000
        };

        var (context, service, provider) =
            await CreateServiceAsync(sensor);

        var request = new TelemetryRequest
        {
            DeviceId = "TEST-POWER-001",
            TelemetryType = TelemetryType.Power,
            Value = 1450
        };

        var result =
            await service.ReceiveTelemetryAsync(request);

        Assert.NotNull(result);
        Assert.Equal("TEST-POWER-001", result.DeviceId);
        Assert.Equal(TelemetryType.Power, result.TelemetryType);
        Assert.Equal(1450d, result.Value);
        Assert.Equal(TelemetryStatus.Normal, result.Status);

        await provider.DisposeAsync();
        await context.DisposeAsync();
    }

    [Fact]
    public async Task ReceiveTelemetry_RelayBooleanValue_ReturnsNormal()
    {
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            DeviceId = "TEST-RELAY-001",
            MacAddress = "AA:BB:CC:DD:EE:21",
            Location = "Test Facility / Relay",
            Category = SensorCategory.Actuator,
            TelemetryType = TelemetryType.RelayState,
            SamplingIntervalSeconds = 5,
            NormalMin = 0,
            NormalMax = 0
        };

        var (context, service, provider) =
            await CreateServiceAsync(sensor);

        var request = new TelemetryRequest
        {
            DeviceId = "TEST-RELAY-001",
            TelemetryType = TelemetryType.RelayState,
            Value = true
        };

        var result =
            await service.ReceiveTelemetryAsync(request);

        Assert.NotNull(result);
        Assert.Equal("TEST-RELAY-001", result.DeviceId);
        Assert.Equal(TelemetryType.RelayState, result.TelemetryType);
        Assert.Equal(true, result.Value);
        Assert.Equal(TelemetryStatus.Normal, result.Status);

        await provider.DisposeAsync();
        await context.DisposeAsync();
    }

    [Fact]
    public async Task ReceiveTelemetry_WrongTelemetryType_ThrowsInvalidOperationException()
    {
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            DeviceId = "TEST-POWER-002",
            MacAddress = "AA:BB:CC:DD:EE:22",
            Location = "Test Facility / Power",
            Category = SensorCategory.Power,
            TelemetryType = TelemetryType.Power,
            SamplingIntervalSeconds = 5,
            NormalMin = 500,
            NormalMax = 2000
        };

        var (context, service, provider) =
            await CreateServiceAsync(sensor);

        var request = new TelemetryRequest
        {
            DeviceId = "TEST-POWER-002",
            TelemetryType = TelemetryType.Temperature,
            Value = 25
        };

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ReceiveTelemetryAsync(request));

        Assert.Equal(
            "The provided telemetry type does not match the registered sensor's telemetry type.",
            exception.Message);

        await provider.DisposeAsync();
        await context.DisposeAsync();
    }

    [Fact]
    public async Task ReceiveTelemetry_InvalidNumericValue_ThrowsArgumentException()
    {
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            DeviceId = "TEST-POWER-003",
            MacAddress = "AA:BB:CC:DD:EE:23",
            Location = "Test Facility / Power",
            Category = SensorCategory.Power,
            TelemetryType = TelemetryType.Power,
            SamplingIntervalSeconds = 5,
            NormalMin = 500,
            NormalMax = 2000
        };

        var (context, service, provider) =
            await CreateServiceAsync(sensor);

        var request = new TelemetryRequest
        {
            DeviceId = "TEST-POWER-003",
            TelemetryType = TelemetryType.Power,
            Value = "not-a-number"
        };

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.ReceiveTelemetryAsync(request));

        Assert.Equal(
            "Telemetry value must be numeric.",
            exception.Message);

        await provider.DisposeAsync();
        await context.DisposeAsync();
    }

    [Fact]
    public async Task ReceiveTelemetry_UnknownDevice_ReturnsNull()
    {
        var (context, service, provider) =
            await CreateServiceAsync();

        var request = new TelemetryRequest
        {
            DeviceId = "DOES-NOT-EXIST",
            TelemetryType = TelemetryType.Temperature,
            Value = 25
        };

        var result =
            await service.ReceiveTelemetryAsync(request);

        Assert.Null(result);

        await provider.DisposeAsync();
        await context.DisposeAsync();
    }
}