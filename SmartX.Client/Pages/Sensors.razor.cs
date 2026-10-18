using Microsoft.AspNetCore.Components;
using SmartX.Client.Services;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Client.Pages;

public partial class Sensors
{
    [Inject]
    private SensorService SensorService { get; set; } = null!;

    private List<Sensor> sensors = new();

    private CreateSensorRequest form = NewForm();

    private bool loading;
    private bool showForm;
    private bool editing;
    private Guid editingId;
    private string? error;

    protected override async Task OnInitializedAsync()
    {
        await LoadSensors();
    }
    /// <summary>
    /// Loads the list of sensors from the SensorService asynchronously. The method sets the loading state to true, clears any previous error messages, and attempts to retrieve the sensors. If successful, it updates the sensors list; if an exception occurs, it captures the error message. Finally, it resets the loading state to false.
    /// </summary>
    /// <returns></returns>
    private async Task LoadSensors()
    {
        loading = true;
        error = null;

        try
        {
            sensors = await SensorService.GetSensorsAsync();
        }
        catch (Exception ex)
        {
            error = $"Unable to load sensors: {ex.Message}";
        }
        finally
        {
            loading = false;
        }
    }
    /// <summary>
    /// Opens the form for creating a new sensor. It resets the editing state, clears the form fields, and sets the showForm flag to true, allowing the user to input details for a new sensor.
    /// </summary>
    private void OpenCreate()
    {
        editing = false;
        editingId = Guid.Empty;
        form = NewForm();
        error = null;
        showForm = true;
    }
    /// <summary>
    /// Opens the form for editing an existing sensor. It sets the editing state to true, populates the form fields with the selected sensor's data, and sets the showForm flag to true, allowing the user to modify the sensor's details.
    /// </summary>
    /// <param name="sensor"></param>
    private void OpenEdit(Sensor sensor)
    {
        editing = true;
        editingId = sensor.Id;

        form = new CreateSensorRequest
        {
            DeviceId = sensor.DeviceId,
            MacAddress = sensor.MacAddress,
            Location = sensor.Location,
            Category = sensor.Category,
            TelemetryType = sensor.TelemetryType,
            SamplingIntervalSeconds = sensor.SamplingIntervalSeconds,
            NormalMin = sensor.NormalMin,
            NormalMax = sensor.NormalMax
        };

        error = null;
        showForm = true;
    }
    /// <summary>
    /// Closes the sensor form by setting the showForm flag to false, effectively hiding the form from the user interface. This method is typically called when the user cancels the operation or after successfully saving a sensor.
    /// </summary>
    private void CloseForm()
    {
        showForm = false;
    }
    /// <summary>
    /// Saves the sensor data entered in the form. If the form is in editing mode, it updates the existing sensor; otherwise, it creates a new sensor. The method handles exceptions and displays error messages if the save operation fails. After a successful save, it reloads the list of sensors and closes the form.
    /// </summary>
    /// <returns></returns>
    private async Task SaveSensor()
    {
        error = null;

        try
        {
            if (editing)
            {
                await SensorService.UpdateSensorAsync(editingId, form);
            }
            else
            {
                await SensorService.CreateSensorAsync(form);
            }

            showForm = false;
            await LoadSensors();
        }
        catch (Exception ex)
        {
            error = $"Save failed: {ex.Message}";
        }
    }
    /// <summary>
    /// Toggles the active status of a sensor. If the sensor is currently active, it will be deactivated, and vice versa. The method calls the SensorService to update the sensor's active status and reloads the list of sensors afterward. If an error occurs during the operation, it captures and displays the error message.
    /// </summary>
    /// <param name="sensor"></param>
    /// <returns></returns>
    private async Task ToggleActive(Sensor sensor)
    {
        error = null;

        try
        {
            await SensorService.UpdateActiveAsync(sensor.Id, !sensor.IsActive);

            await LoadSensors();
        }
        catch (Exception ex)
        {
            error = $"Unable to change sensor status: {ex.Message}";
        }
    }
    /// <summary>
    /// Deletes a sensor from the system. The method calls the SensorService to remove the sensor based on its ID and reloads the list of sensors afterward. If an error occurs during the deletion process, it captures and displays the error message.
    /// </summary>
    /// <param name="sensor"></param>
    /// <returns></returns>
    private async Task DeleteSensor(Sensor sensor)
    {
        error = null;

        try
        {
            await SensorService.DeleteSensorAsync(sensor.Id);

            await LoadSensors();
        }
        catch (Exception ex)
        {
            error = $"Unable to delete sensor: {ex.Message}";
        }
    }
    /// <summary>
    /// Creates a new instance of CreateSensorRequest with default values for the sensor form. This method is used to initialize the form fields when creating a new sensor, ensuring that all necessary properties are set to their default states.
    /// </summary>
    /// <returns></returns>
    private static CreateSensorRequest NewForm()
    {
        return new CreateSensorRequest
        {
            DeviceId = "",
            MacAddress = "",
            Location = "",
            Category = SensorCategory.Environmental,
            TelemetryType = TelemetryType.Temperature,
            SamplingIntervalSeconds = 5,
            NormalMin = 18,
            NormalMax = 28
        };
    }
}