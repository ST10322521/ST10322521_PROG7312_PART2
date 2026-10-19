using System.Net.Http.Json;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/sealed
// https://learn.microsoft.com/en-us/dotnet/api/system.timespan.fromseconds?view=net-10.0
// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.completedtask?view=net-10.0

namespace SmartX.SensorSimulator;

public static class Program
{
    private const string ApiBaseUrl = "http://localhost:5017";

    public static async Task Main()
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("SmartX Live IoT Sensor Simulator");
        Console.WriteLine("===========================================");
        Console.WriteLine($"API: {ApiBaseUrl}");
        Console.WriteLine();

        using var httpClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl), Timeout = TimeSpan.FromSeconds(10) };

        await using var commandConnection = new HubConnectionBuilder()
            .WithUrl($"{ApiBaseUrl}/smartxHub")
            .WithAutomaticReconnect()
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(
                    LogLevel.Debug);
            })
            .Build();

        commandConnection.Reconnecting += error =>
        {
            Console.WriteLine($"[COMMAND] SignalR command stream reconnecting: {error?.Message}");
            return Task.CompletedTask;
        };

        commandConnection.Reconnected += connectionId =>
        {
            Console.WriteLine($"[COMMAND] SignalR command stream reconnected. Connection ID: {connectionId}");
            return Task.CompletedTask;
        };

        commandConnection.Closed += error =>
        {
            Console.WriteLine($"[COMMAND] SignalR command stream closed: {error?.Message}");
            return Task.CompletedTask;
        };

        using var shutdown = new CancellationTokenSource();

        // Handle Ctrl+C or SIGTERM to gracefully shut down the simulator
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;

            Console.WriteLine();
            Console.WriteLine("Shutting requested...");

            shutdown.Cancel();
        };

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        var fleet = new SimulatedDeviceFleet(httpClient, jsonOptions);

        commandConnection.On<DeviceCommandRequest>("DeviceCommandReceived", async command =>
        {
            Console.WriteLine($"[COMMAND] Received command: Device={command.DeviceId} | Command={command.Command}");

            await fleet.HandleCommand(command);
        });

        await commandConnection.StartAsync();

        Console.WriteLine("[COMMAND] SignalR command stream connected.");

        try
        {
            await fleet.RunAsync(shutdown.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Shutdown completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Simulator stopped unexpectedly: Error: {ex.Message}");
        }

        Console.WriteLine("Simulator stopped.");
    }
    /// <summary>
    /// Represents a fleet of simulated devices that can be reconciled with the API to ensure that the simulator reflects the current state of the devices in the system.
    /// </summary>
    public sealed class SimulatedDeviceFleet
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<Guid, SimulatedDevice> _devices = new();
        private static readonly TimeSpan ReconciliationInterval = TimeSpan.FromSeconds(2);

        public SimulatedDeviceFleet(HttpClient httpClient, JsonSerializerOptions jsonOptions)
        {
            _httpClient = httpClient;
            _jsonOptions = jsonOptions;
        }
        public async Task HandleCommand(DeviceCommandRequest command)
        {
            if (!_devices.Values.Any(device => string.Equals(device.DeviceId, command.DeviceId, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"[COMMAND] Device not found: {command.DeviceId}");
                return;
            }

            var device = _devices.Values.First(device => string.Equals(device.DeviceId, command.DeviceId, StringComparison.OrdinalIgnoreCase));
            await device.HandleCommand(command);
        }
        /// <summary>
        /// Runs the fleet reconciliation loop, periodically checking the API for the current state of devices and updating the simulator accordingly. The loop continues until the provided cancellation token is triggered, at which point all devices are stopped gracefully.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting live fleet reconciliation...");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ReconcileAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FLEET] Reconciliation error: {ex.Message}");
                }

                try
                {
                    await Task.Delay(ReconciliationInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            await StopAllAsync();

            Console.WriteLine("[FLEET] All devices stopped.");
        }
        /// <summary>
        /// Reconciles the current state of the simulated devices with the state of the devices in the API. It fetches the list of sensors from the API, updates existing devices, adds new devices, and removes any devices that are no longer present in the API. The method ensures that the simulator reflects the current state of the system accurately.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReconcileAsync(CancellationToken cancellationToken)
        {
            var sensors = await _httpClient.GetFromJsonAsync<List<Sensor>>("/api/sensors", _jsonOptions, cancellationToken);
            if (sensors is null)
            {
                Console.WriteLine("[FLEET] No sensors found.");
                return;
            }

            var apiSensorId = sensors.Select(sensor => sensor.Id).ToHashSet();

            // Update existing devices and add new devices
            foreach (var sensor in sensors)
            {
                if (_devices.TryGetValue(sensor.Id, out var existing))
                {
                    await existing.UpdateConfigurationAsync(sensor, cancellationToken);

                    continue;
                }

                var device = new SimulatedDevice(sensor, _httpClient);

                _devices.Add(sensor.Id, device);

                Console.WriteLine($"[FLEET] Device discovered: {sensor.DeviceId} | Type: {sensor.TelemetryType}");

                device.Start();
            }

            var removedDevices = _devices.Where(pair => !apiSensorId.Contains(pair.Key)).Select(pair => pair.Value).ToList();

            // Remove devices that are no longer present in the API
            foreach (var device in removedDevices)
            {
                Console.WriteLine($"[FLEET] Device removed from registry: {device.DeviceId}");

                await device.StopAsync();

                _devices.Remove(device.SensorId);

                PrintFleetStatus();
            }
        }
        /// <summary>
        /// Stops all running simulated devices in the fleet and clears the internal device collection. This method is called during the shutdown process to ensure that all devices are stopped gracefully before the simulator exits.
        /// </summary>
        /// <returns></returns>
        private async Task StopAllAsync()
        {
            foreach (var device in _devices.Values)
            {
                await device.StopAsync();
            }

            _devices.Clear();
        }
        /// <summary>
        /// Prints the current status of the fleet, including the total number of devices, the number of running devices, and the number of idle devices. This method is called after device updates to provide a quick overview of the fleet's state.
        /// </summary>
        private void PrintFleetStatus()
        {
            var running = _devices.Values.Count(device => device.IsRunning);
            var idle = _devices.Count - running;

            Console.WriteLine($"[FLEET] Status: Total: {_devices.Count} | Running: {running} | Idle: {idle}");
        }
    }
    /// <summary>
    /// Represents a simulated device that can be controlled and monitored within the simulator.
    /// </summary>
    private sealed class SimulatedDevice
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _commandLock = new(1, 1);

        private Sensor _configuration;
        private CancellationTokenSource? _runCancellation;
        private Task? _runTask;
        private int _telemetryCycle;
        public bool IsRunning => _runTask is not null && !_runTask.IsCompleted;
        public Guid SensorId => _configuration.Id;
        public string DeviceId => _configuration.DeviceId;
        private bool? _commandedActuatorState;

        // Cycle lengths for different telemetry states
        private const int NormalCycleLength = 10;
        private const int SpikeCycleLength = 3;
        private const int DropCycleLength = 3;
        private const int InvalidCycleLength = 3;
        private const int StaleCycleLength = 3;
        private const int DisconnectCycleLength = 3;
        private int TotalAnomalyCycleLength => NormalCycleLength + SpikeCycleLength + DropCycleLength + InvalidCycleLength + StaleCycleLength + DisconnectCycleLength;

        public SimulatedDevice(Sensor configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }
        public async Task HandleCommand(DeviceCommandRequest command)
        {
            await _commandLock.WaitAsync();

            try
            {
                switch (command.Command.ToLowerInvariant())
                {
                    case "start":
                        Start();
                        Console.WriteLine(
                            $"[DEVICE: {DeviceId}] Command executed: Start");
                        break;

                    case "stop":
                        await StopAsync();
                        Console.WriteLine(
                            $"[DEVICE: {DeviceId}] Command executed: Stop");
                        break;

                    case "setstate":
                        await HandleSetStateCommandAsync(command);
                        break;

                    default:
                        Console.WriteLine(
                            $"[COMMAND] Unsupported command: {command.Command}");
                        break;
                }
            }
            finally
            {
                _commandLock.Release();
            }
        }
        private async Task HandleSetStateCommandAsync(DeviceCommandRequest command)
        {
            if (_configuration.TelemetryType is not (TelemetryType.RelayState or TelemetryType.ValveState))
            {
                Console.WriteLine($"[DEVICE: {DeviceId}] SetState rejected: device is not an actuator");
                return;
            }

            if (!bool.TryParse(command.Value?.ToString(), out var state))
            {
                Console.WriteLine($"[DEVICE: {DeviceId}] SetState rejected: value must be true or false.");
                return;
            }

            _commandedActuatorState = state;

            Console.WriteLine($"[DEVICE: {DeviceId}] Command executed: SET STATE = {state}");

            await Task.CompletedTask;
        }
        /// <summary>
        /// Updates the configuration of the simulated device based on the provided sensor configuration. If the telemetry type, sampling interval, or normal range has changed, the method logs the updated configuration. This allows the simulator to reflect changes made to the device's settings in the API.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UpdateConfigurationAsync(Sensor configuration, CancellationToken cancellationToken)
        {
            var telemetryChanged = _configuration.TelemetryType != configuration.TelemetryType;

            var intervalChanged = _configuration.SamplingIntervalSeconds != configuration.SamplingIntervalSeconds;

            var rangeChanged = _configuration.NormalMin != configuration.NormalMin || _configuration.NormalMax != configuration.NormalMax;

            _configuration = configuration;

            if (telemetryChanged || intervalChanged || rangeChanged)
            {
                Console.WriteLine($"[DEVICE: {DeviceId}] Configuration updated | Type: {configuration.TelemetryType} | Interval: {configuration.SamplingIntervalSeconds}s | Range: [{configuration.NormalMin}, {configuration.NormalMax}]");
            }
        }
        /// <summary>
        /// Starts the telemetry streaming for the device if it is not already running. This method initializes a cancellation token source and starts the asynchronous telemetry generation and sending loop.
        /// </summary>
        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            _runCancellation?.Dispose();

            _runCancellation = new CancellationTokenSource();

            _runTask = RunAsync(_runCancellation.Token);
        }
        /// <summary>
        /// Stops the telemetry streaming for the device if it is currently running. This method cancels the running task and waits for it to complete, ensuring that all resources are cleaned up properly. Any exceptions during cancellation are caught and logged.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            if (_runCancellation is null)
            {
                return;
            }
            // Cancel the running task and wait for it to complete
            try
            {
                _runCancellation.Cancel();

                if (_runTask is not null)
                {
                    await _runTask;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during cancellation
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEVICE: {DeviceId}] Error stopping device: {ex.Message}");
            }
            finally
            {
                _runCancellation.Dispose();
                _runCancellation = null;
                _runTask = null;
            }
        }
        /// <summary>
        /// Runs the telemetry generation and sending loop for the device. The method continuously generates telemetry data based on the device's configuration and sends it to the API until the provided cancellation token is triggered. It handles anomalies, logs the results of telemetry sends, and manages delays between telemetry transmissions based on the configured sampling interval.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _telemetryCycle++;

                var cyclePosition = (_telemetryCycle - 1) % TotalAnomalyCycleLength;

                var anomaly = GetAnomalyType(cyclePosition);

                if (anomaly == "Disconnect")
                {
                    Console.WriteLine($"[DEVICE: {DeviceId}] ANOMALY: Simulated disconnect - telemetry skipped.");
                }
                else
                {
                    var telemetry = CreateTelemetry(anomaly);

                    try
                    {
                        var response = await _httpClient.PostAsJsonAsync(
                            "/api/telemetry",
                            telemetry,
                            cancellationToken);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine(
                                $"[DEVICE: {DeviceId}] Telemetry sent: {telemetry.Value} | " +
                                $"Type: {telemetry.TelemetryType} | " +
                                $"Timestamp: {telemetry.Timestamp:HH:mm:ss}");
                        }
                        else
                        {
                            var body = await response.Content.ReadAsStringAsync(cancellationToken);

                            Console.WriteLine($"[DEVICE: {DeviceId}] Telemetry send failed: {response.StatusCode} | Anomaly: {anomaly} | Body: {body}");
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"[DEVICE: {DeviceId}] API connection error: {ex.Message}");
                    }
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _configuration.SamplingIntervalSeconds)), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        /// <summary>
        /// Determines the type of anomaly to simulate based on the current position in the telemetry cycle. The method uses predefined cycle lengths for different anomaly types (Normal, Spike, Drop, Invalid, Stale, Disconnect) and returns the corresponding anomaly type as a string. If the cycle position exceeds all defined lengths, it defaults to "Disconnect".
        /// </summary>
        /// <param name="cyclePosition"></param>
        /// <returns></returns>
        private string GetAnomalyType(int cyclePosition)
        {
            if (cyclePosition < NormalCycleLength)
            {
                return "Normal";
            }

            cyclePosition -= NormalCycleLength;

            if (cyclePosition < SpikeCycleLength)
            {
                return "Spike";
            }

            cyclePosition -= SpikeCycleLength;

            if (cyclePosition < DropCycleLength)
            {
                return "Drop";
            }

            cyclePosition -= DropCycleLength;

            if (cyclePosition < InvalidCycleLength)
            {
                return "Invalid";
            }

            cyclePosition -= InvalidCycleLength;

            if (cyclePosition < StaleCycleLength)
            {
                return "Stale";
            }
            else
            {
                return "Disconnect";
            }
        }
        /// <summary>
        /// Creates a telemetry request based on the device's configuration. The method generates a random value within the specified range for the configured telemetry type and constructs a TelemetryRequest object with the device ID, telemetry type, generated value, and current timestamp. If the telemetry type is unsupported, an InvalidOperationException is thrown.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private TelemetryRequest CreateTelemetry(string anomaly)
        {
            object value = _configuration.TelemetryType switch
            {
                TelemetryType.Temperature => GenerateFloatValue(anomaly),

                TelemetryType.Humidity => GenerateFloatValue(anomaly),

                TelemetryType.SoilMoisture => GenerateFloatValue(anomaly),

                TelemetryType.Power => GenerateIntegerValue(anomaly),

                TelemetryType.Voltage => GenerateIntegerValue(anomaly),

                TelemetryType.Current => GenerateIntegerValue(anomaly),

                TelemetryType.RelayState => _commandedActuatorState ?? Random.Shared.Next(0, 2) == 1,

                TelemetryType.ValveState => _commandedActuatorState ?? Random.Shared.Next(0, 2) == 1,

                _ => throw new InvalidOperationException($"Unsupported telemetry type: {_configuration.TelemetryType}")
            };

            var timestamp = anomaly == "Stale" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow;

            return new TelemetryRequest
            {
                DeviceId = _configuration.DeviceId,
                TelemetryType = _configuration.TelemetryType,
                Value = value,
                Timestamp = timestamp
            };
        }
        /// <summary>
        /// Generates a random float value within the configured normal range for the device. If the maximum value is less than or equal to the minimum value, the method returns the minimum value. Otherwise, it generates a random float between the minimum and maximum values (inclusive) using a uniform distribution. The method also simulates anomalies such as spikes, drops, and invalid values based on the provided anomaly type.
        /// </summary>
        /// <param name="anomaly"></param>
        /// <returns></returns>
        private float GenerateFloatValue(string anomaly)
        {
            var min = (float)_configuration.NormalMin;
            var max = (float)_configuration.NormalMax;

            if (max <= min)
            {
                return min;
            }

            return anomaly switch
            {
                "Spike" => max + (float)(Random.Shared.NextDouble() * (max - min) * 0.5), // Spike above max
                "Drop" => min - (float)(Random.Shared.NextDouble() * (max - min) * 0.5), // Drop below min
                "Invalid" => float.NaN, // Invalid value
                _ => (float)(min + Random.Shared.NextDouble() * (max - min)) // Normal value
            };
        }
        /// <summary>
        /// Generates a random integer value within the configured normal range for the device. If the maximum value is less than or equal to the minimum value, the method returns the minimum value. Otherwise, it generates a random integer between the minimum and maximum values (inclusive) using a uniform distribution. The method also simulates anomalies such as spikes, drops, and invalid values based on the provided anomaly type.
        /// </summary>
        /// <param name="anomaly"></param>
        /// <returns></returns>
        private int GenerateIntegerValue(string anomaly)
        {
            var min = (int)Math.Round(_configuration.NormalMin);
            var max = (int)Math.Round(_configuration.NormalMax);

            if (max <= min)
            {
                return min;
            }

            var range = Math.Max(1, max - min);

            return anomaly switch
            {
                "Spike" => max + Random.Shared.Next(1, range / 2 + 1), // Spike above max
                "Drop" => min - Random.Shared.Next(1, range / 2 + 1), // Drop below min
                "Invalid" => int.MinValue, // Invalid value
                _ => Random.Shared.Next(min, max + 1) // Normal value
            };
        }
    }
}
