using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using SmartX.Shared.DTOs;

namespace SmartX.Client.Pages;

public partial class Commands : IDisposable
{
    [SupplyParameterFromQuery(Name = "deviceId")]
    private string? DeviceIdFromQuery { get; set; }

    private string deviceId = string.Empty;
    private string selectedCommand = "Start";
    private string commandValue = string.Empty;

    private bool isSending;
    private string message = string.Empty;
    private Severity messageSeverity = Severity.Info;

    private readonly List<DeviceCommandRequest> commandHistory = [];
    /// <summary>
    /// Asynchronously initializes the component by subscribing to the SignalR CommandReceived event and starting the SignalR connection if it is not already connected. If a DeviceId is provided in the query parameters, it sets the deviceId field accordingly. The method ensures that the component is ready to receive command updates and interact with the server for sending commands.
    /// </summary>
    /// <returns></returns>
    protected override async Task OnInitializedAsync()
    {
        SignalR.CommandReceived += OnCommandReceived;

        if (!string.IsNullOrWhiteSpace(DeviceIdFromQuery))
        {
            deviceId = DeviceIdFromQuery;
        }

        if (SignalR.State != HubConnectionState.Connected)
        {
            await SignalR.StartAsync();
        }
    }
    /// <summary>
    /// Handles the CommandReceived event from the SignalR service. When a command is received, it inserts the command at the beginning of the commandHistory list and triggers a UI update by invoking StateHasChanged asynchronously. This allows the component to display the most recent commands at the top of the history list, ensuring that users can see the latest commands executed on devices in real-time.
    /// </summary>
    /// <param name="command"></param>
    private async void OnCommandReceived(DeviceCommandRequest command)
    {
        commandHistory.Insert(0, command);

        await InvokeAsync(StateHasChanged);
    }
    /// <summary>
    /// Asynchronously sends a command to the server for the specified device. The method validates the input fields, constructs a DeviceCommandRequest object, and sends it to the "api/commands" endpoint via an HTTP POST request. It handles success and error responses by updating the message and messageSeverity fields accordingly. The method also manages the isSending flag to indicate when a command is being sent, ensuring that the UI reflects the current state of the operation.
    /// </summary>
    /// <returns></returns>
    private async Task SendCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            message = "Device ID is required.";
            messageSeverity = Severity.Error;
            return;
        }

        isSending = true;
        message = string.Empty;

        try
        {
            var request = new DeviceCommandRequest
            {
                DeviceId = deviceId.Trim(),
                Command = selectedCommand,
                Value = selectedCommand == "SetState"
                    ? commandValue
                    : null,
                Timestamp = DateTime.UtcNow
            };

            var response = await Http.PostAsJsonAsync("api/commands", request);

            if (response.IsSuccessStatusCode)
            {
                message = "Command queued successfully.";
                messageSeverity = Severity.Success;
            }
            else
            {
                message = await response.Content.ReadAsStringAsync();
                messageSeverity = Severity.Error;
            }
        }
        catch (Exception ex)
        {
            message = $"Failed to send command: {ex.Message}";
            messageSeverity = Severity.Error;
        }
        finally
        {
            isSending = false;
        }
    }
    /// <summary>
    /// Asynchronously undoes the last device command that was processed. The method sends an HTTP POST request to the "api/commands/undo" endpoint and handles the response. If the request is successful, it updates the message and messageSeverity fields to indicate that the last command has been undone successfully. In case of an error or exception, it captures the error message and sets the appropriate severity level for user feedback.
    /// </summary>
    /// <returns></returns>
    private async Task UndoAsync()
    {
        try
        {
            var response = await Http.PostAsync("api/commands/undo", null);

            if (response.IsSuccessStatusCode)
            {
                message = "Last command undone successfully.";
                messageSeverity = Severity.Success;
            }
            else
            {
                message = await response.Content.ReadAsStringAsync();
                messageSeverity = Severity.Warning;
            }
        }
        catch (Exception ex)
        {
            message = $"Undo failed: {ex.Message}";
            messageSeverity = Severity.Error;
        }
    }
    /// <summary>
    /// Disposes of the component by unsubscribing from the SignalR CommandReceived event. This ensures that when the component is no longer in use, it does not continue to receive command updates, preventing potential memory leaks and unintended behavior. The method is called automatically when the component is removed from the UI or when the application is shutting down.
    /// </summary>
    public void Dispose()
    {
        SignalR.CommandReceived -= OnCommandReceived;
    }
}