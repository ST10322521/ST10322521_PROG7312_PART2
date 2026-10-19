using Microsoft.AspNetCore.SignalR.Client;
using SmartX.Shared.DTOs;

namespace SmartX.Client.Services;

public sealed class SignalRService : IAsyncDisposable
{
    private readonly HubConnection _connection;
    public event Action<TelemetryResponse>? TelemetryReceived;
    public event Action<DeviceCommandRequest>? CommandReceived;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRService"/> class and sets up the SignalR connection with automatic reconnection and event handlers for connection state changes.
    /// </summary>
    public SignalRService()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5017/smartxHub")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<TelemetryResponse>("TelemetryReceived", response =>
        {
            Console.WriteLine($"Telemetry received: DeviceId={response.DeviceId}, Value={response.Value}, Status={response.Status}");
            TelemetryReceived?.Invoke(response);
        });

        _connection.On<DeviceCommandRequest>("DeviceCommandReceived", command =>
        {
            Console.WriteLine($"Command received: DeviceId={command.DeviceId}, Command={command.Command}");

            CommandReceived?.Invoke(command);
        });

        _connection.Reconnecting += error =>
        {
            Console.WriteLine($"SignalR connection lost. Attempting to reconnect... Error: {error?.Message}");
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            Console.WriteLine($"SignalR reconnected. Connection ID: {connectionId}");
            return Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            Console.WriteLine($"SignalR connection closed. Error: {error?.Message}");
            return Task.CompletedTask;
        };
    }
    /// <summary>
    /// Gets the current state of the SignalR connection.
    /// </summary>
    public HubConnectionState State => _connection.State;
    /// <summary>
    /// Starts the SignalR connection asynchronously and logs the connection ID upon successful connection.
    /// </summary>
    /// <returns></returns>
    public async Task StartAsync()
    {
        await _connection.StartAsync();

        Console.WriteLine($"SignalR connection started: {_connection.ConnectionId}");
    }
    /// <summary>
    /// Disposes the SignalR connection asynchronously when the service is no longer needed.
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}