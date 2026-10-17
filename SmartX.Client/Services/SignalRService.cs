using Microsoft.AspNetCore.SignalR.Client;

namespace SmartX.Client.Services;

public sealed class SignalRService : IAsyncDisposable
{
    private readonly HubConnection _connection;
    /// <summary>
    /// Initializes a new instance of the SignalRService class, setting up a connection to the SmartXHub at the specified URL and enabling automatic reconnection.
    /// </summary>
    public SignalRService()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5017/smartxHub")
            .WithAutomaticReconnect()
            .Build();
    }
    /// <summary>
    /// Starts the SignalR connection to the SmartXHub. This method should be called before attempting to send or receive messages through the hub.
    /// </summary>
    /// <returns></returns>
    public async Task StartAsync()
    {
        await _connection.StartAsync();
    }
    /// <summary>
    /// Disposes of the SignalR connection asynchronously, ensuring that all resources are released properly. This method should be called when the SignalRService is no longer needed to prevent resource leaks and maintain application performance.
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}