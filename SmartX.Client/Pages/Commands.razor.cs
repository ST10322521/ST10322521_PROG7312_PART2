using System.Net.Http.Json;
using MudBlazor;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Client.Pages;

public partial class Commands : IDisposable
{


    private string deviceId = string.Empty;
    private string selectedCommand = "Start";
    private string commandValue = string.Empty;

    private bool isSending;
    private string message = string.Empty;
    private Severity messageSeverity = Severity.Info;

    private readonly List<DeviceCommandRequest> commandHistory = [];

    protected override async Task OnInitializedAsync()
    {
        SignalR.CommandReceived += OnCommandReceived;

        if (SignalR.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
        {
            await SignalR.StartAsync();
        }
    }

    private async void OnCommandReceived(DeviceCommandRequest command)
    {
        commandHistory.Insert(0, command);

        await InvokeAsync(StateHasChanged);
    }

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

    public void Dispose()
    {
        SignalR.CommandReceived -= OnCommandReceived;
    }
}