using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using SmartX.Client.Services;
using SmartX.Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartX.Client.Pages;

public partial class History
{
    [Inject]
    private SensorService SensorService { get; set; } = null!;

    [Inject]
    private HttpClient Http { get; set; } = null!;

    private List<Sensor> sensors = [];
    private List<string> deviceIds = [];
    private string? selectedDeviceId;

    private List<SensorReading> history = [];

    private bool loading;
    private string? error;
    /// <summary>
    /// Initializes the component by loading the list of sensors and their corresponding device IDs. It retrieves the sensors from the SensorService and populates the deviceIds list with non-empty device IDs, sorted in ascending order. The first available device ID is selected by default, and if a valid device ID is found, it triggers the loading of telemetry history for that device. If any errors occur during this process, an error message is set to inform the user.
    /// </summary>
    /// <returns></returns>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            sensors = await SensorService.GetSensorsAsync();

            deviceIds = sensors
                .Select(sensor => sensor.DeviceId)
                .Where(deviceId => !string.IsNullOrWhiteSpace(deviceId))
                .OrderBy(deviceId => deviceId)
                .ToList();

            selectedDeviceId = deviceIds.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(selectedDeviceId))
            {
                await LoadHistoryAsync();
            }
        }
        catch (Exception ex)
        {
            error = $"Unable to load registered sensors: {ex.Message}";
        }
    }
    /// <summary>
    /// Loads the telemetry history for the currently selected device asynchronously. If no device is selected, the method returns early. It sets a loading flag to true and clears any previous error messages. The method then attempts to retrieve the telemetry history from the API using an HTTP GET request, deserializing the response into a list of SensorReading objects. If an exception occurs during this process, it captures the error message and sets it for display. Finally, it resets the loading flag to false, indicating that the operation has completed.
    /// </summary>
    /// <returns></returns>
    private async Task LoadHistoryAsync()
    {
        if (string.IsNullOrWhiteSpace(selectedDeviceId))
        {
            return;
        }

        loading = true;
        error = null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            options.Converters.Add(new JsonStringEnumConverter());

            history = await Http.GetFromJsonAsync<List<SensorReading>>(
                $"api/telemetry/history/{Uri.EscapeDataString(selectedDeviceId)}",
                options)
                ?? [];
        }
        catch (Exception ex)
        {
            history = [];
            error = $"Unable to load telemetry history: {ex.Message}";
        }
        finally
        {
            loading = false;
        }
    }
}