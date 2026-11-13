using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using SmartX.Client.Services;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartX.Client.Pages;

/// <summary>
/// Represents the telemetry history page in the SmartX client application. This page allows users to view historical telemetry data for registered sensors, filter the data by device ID, telemetry type, and time range, and display the results in a user-friendly format. The page interacts with the SensorService to retrieve sensor information and uses HttpClient to fetch telemetry history from the server-side API.
/// </summary>
public partial class History
{
    [Inject]
    private SensorService SensorService { get; set; } = null!;

    [Inject]
    private HttpClient Http { get; set; } = null!;

    private List<Sensor> sensors = [];
    private List<string> deviceIds = [];
    private string? selectedDeviceId;

    private TelemetryType selectedTelemetryType = TelemetryType.Temperature;

    private DateTime? startDate;
    private TimeSpan? startTime = TimeSpan.Zero;

    private DateTime? endDate;
    private TimeSpan? endTime = new(23, 59, 59);

    private List<SensorReading> history = [];

    private bool loading;
    private string? error;
    private bool hasQueriedRange;

    /// <summary>
    /// Initializes the telemetry history page by loading the list of registered sensors and their device IDs. The method retrieves sensor information from the SensorService and populates the deviceIds list, allowing users to select a device for viewing its telemetry history. If a device is selected, it automatically loads the corresponding telemetry history. Any errors encountered during the initialization process are captured and displayed to the user.
    /// </summary>
    /// <returns></returns>
    protected override async Task OnInitializedAsync()
    {
        // Load the list of registered sensors and their device IDs
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
    /// Loads the telemetry history for the currently selected device. The method checks if a device is selected and, if so, sends an HTTP GET request to the server-side API to retrieve the historical telemetry data associated with that device. The retrieved data is deserialized into a list of SensorReading objects and stored in the history property for display. Any errors encountered during the loading process are captured and displayed to the user.
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
        hasQueriedRange = false;
        // Clear the previous history data
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
    /// <summary>
    /// Queries the telemetry history for the selected device within a specified time range and telemetry type. The method validates the input parameters, constructs the appropriate API request URL, and sends an HTTP GET request to retrieve the filtered telemetry history data. The results are deserialized into a list of SensorReading objects and stored in the history property for display. Any errors encountered during the query process are captured and displayed to the user.
    /// </summary>
    /// <returns></returns>
    private async Task QueryHistoryAsync()
    {
        error = null;
        history = [];
        hasQueriedRange = false;

        if (string.IsNullOrWhiteSpace(selectedDeviceId))
        {
            error = "Select a device first.";
            return;
        }

        if (!startDate.HasValue || !endDate.HasValue)
        {
            error = "Select both a start date and an end date.";
            return;
        }

        var from = startDate.Value.Date + (startTime ?? TimeSpan.Zero);
        var to = endDate.Value.Date + (endTime ?? new TimeSpan(23, 59, 59));

        if (from > to)
        {
            error = "The start timestamp must be earlier than or equal to the end timestamp.";
            return;
        }

        loading = true;
        // Clear the previous history data
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            options.Converters.Add(new JsonStringEnumConverter());

            var url =
                $"api/telemetry/history/{Uri.EscapeDataString(selectedDeviceId)}/range" +
                $"?telemetryType={selectedTelemetryType}" +
                $"&from={Uri.EscapeDataString(from.ToUniversalTime().ToString("O"))}" +
                $"&to={Uri.EscapeDataString(to.ToUniversalTime().ToString("O"))}";

            history = await Http.GetFromJsonAsync<List<SensorReading>>(
                url,
                options)
                ?? [];

            hasQueriedRange = true;
        }
        catch (Exception ex)
        {
            history = [];
            error = $"Unable to query telemetry history: {ex.Message}";
        }
        finally
        {
            loading = false;
        }
    }
}