using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Api.Services;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using SmartX.Api.Hubs;
using Microsoft.Extensions.DependencyInjection;

namespace SmartX.Tests.Services;

public class TelemetryIngestionServiceTests
{
    private static SmartXDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SmartXDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SmartXDbContext(options);
    }

    private static IHubContext<SmartXHub> CreateHubContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSignalR();
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IHubContext<SmartXHub>>();
    }

    [Fact]
    public async Task ReceiveTelemetry_PowerIntegerValue_ReturnsNormal()
    {
        await using var context = CreateContext();

        context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            DeviceId = "TEST-POWER-001",
            MacAddress = "AA:BB:CC:DD:EE:20",
            Location = "Test Facility / Power",
            Category = SensorCategory.Power,
            TelemetryType = TelemetryType.Power,
            SamplingIntervalSeconds = 5,
            NormalMin = 500,
            NormalMax = 2000,
            IsActive = true
        });

        await context.SaveChangesAsync();

        var service = new TelemetryIngestionService(
            context,
            new TelemetryService(),
            CreateHubContext());

        var request = new TelemetryRequest
        {
            DeviceId = "TEST-POWER-001",
            TelemetryType = TelemetryType.Power,
            Value = 1450
        };

        var result = await service.ReceiveTelemetryAsync(request);

        Assert.NotNull(result);
        Assert.Equal("TEST-POWER-001", result.DeviceId);
        Assert.Equal(TelemetryType.Power, result.TelemetryType);
        Assert.Equal(1450d, result.Value);
        Assert.Equal(TelemetryStatus.Normal, result.Status);
    }

    [Fact]
    public async Task ReceiveTelemetry_RelayBooleanValue_ReturnsNormal()
    {
        await using var context = CreateContext();

        context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            DeviceId = "TEST-RELAY-001",
            MacAddress = "AA:BB:CC:DD:EE:21",
            Location = "Test Facility / Relay",
            Category = SensorCategory.Actuator,
            TelemetryType = TelemetryType.RelayState,
            SamplingIntervalSeconds = 5,
            NormalMin = 0,
            NormalMax = 0,
            IsActive = true
        });

        await context.SaveChangesAsync();

        var service = new TelemetryIngestionService(
            context,
            new TelemetryService(),
            CreateHubContext());

        var request = new TelemetryRequest
        {
            DeviceId = "TEST-RELAY-001",
            TelemetryType = TelemetryType.RelayState,
            Value = true
        };

        var result = await service.ReceiveTelemetryAsync(request);

        Assert.NotNull(result);
        Assert.Equal("TEST-RELAY-001", result.DeviceId);
        Assert.Equal(TelemetryType.RelayState, result.TelemetryType);
        Assert.Equal(true, result.Value);
        Assert.Equal(TelemetryStatus.Normal, result.Status);
    }

    [Fact]
    public async Task ReceiveTelemetry_WrongTelemetryType_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();

        context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            DeviceId = "TEST-POWER-002",
            MacAddress = "AA:BB:CC:DD:EE:22",
            Location = "Test Facility / Power",
            Category = SensorCategory.Power,
            TelemetryType = TelemetryType.Power,
            SamplingIntervalSeconds = 5,
            NormalMin = 500,
            NormalMax = 2000,
            IsActive = true
        });

        await context.SaveChangesAsync();

        var service = new TelemetryIngestionService(
            context,
            new TelemetryService(),
            CreateHubContext());

        var request = new TelemetryRequest
        {
            DeviceId = "TEST-POWER-002",
            TelemetryType = TelemetryType.Temperature,
            Value = 25
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ReceiveTelemetryAsync(request));

        Assert.Equal(
            "The provided telemetry type does not match the registered sensor's telemetry type.",
            exception.Message);
    }

    [Fact]
    public async Task ReceiveTelemetry_InvalidNumericValue_ThrowsArgumentException()
    {
        await using var context = CreateContext();

        context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            DeviceId = "TEST-POWER-003",
            MacAddress = "AA:BB:CC:DD:EE:23",
            Location = "Test Facility / Power",
            Category = SensorCategory.Power,
            TelemetryType = TelemetryType.Power,
            SamplingIntervalSeconds = 5,
            NormalMin = 500,
            NormalMax = 2000,
            IsActive = true
        });

        await context.SaveChangesAsync();

        var service = new TelemetryIngestionService(
            context,
            new TelemetryService(),
            CreateHubContext());

        var request = new TelemetryRequest
        {
            DeviceId = "TEST-POWER-003",
            TelemetryType = TelemetryType.Power,
            Value = "not-a-number"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReceiveTelemetryAsync(request));

        Assert.Equal(
            "Telemetry value must be numeric.",
            exception.Message);
    }

    [Fact]
    public async Task ReceiveTelemetry_UnknownDevice_ReturnsNull()
    {
        await using var context = CreateContext();

        var service = new TelemetryIngestionService(
            context,
            new TelemetryService(),
            CreateHubContext());

        var request = new TelemetryRequest
        {
            DeviceId = "DOES-NOT-EXIST",
            TelemetryType = TelemetryType.Temperature,
            Value = 25
        };

        var result = await service.ReceiveTelemetryAsync(request);

        Assert.Null(result);
    }
}