using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SmartX.Client.Services;
using SmartX.Shared.DTOs;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartX.Client.Pages;

public partial class Sensors
{
    [Inject]
    private SensorService SensorService { get; set; } = null!;

    [Inject]
    private SensorAttachmentService AttachmentService { get; set; } = null!;

    private List<Sensor> sensors = new();

    private CreateSensorRequest form = NewForm();

    private bool loading;
    private bool showForm;
    private bool editing;
    private Guid editingId;
    private string? error;

    private bool showAttachments;
    private Sensor? selectedSensor;
    private List<SensorAttachment> attachments = new();

    private bool uploading;
    private SensorAttachmentType selectedAttachmentType = SensorAttachmentType.Configuration;

    protected override async Task OnInitializedAsync()
    {
        await LoadSensors();
    }
    /// <summary>
    /// Asynchronously loads the list of sensors from the SensorService. The method sets the loading state to true and clears any previous error messages. It then attempts to retrieve the list of sensors using the GetSensorsAsync method of the SensorService. If successful, it updates the sensors list; if an exception occurs, it captures the error message. Finally, it sets the loading state back to false, regardless of success or failure.
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
    /// Opens the form for creating a new sensor. The method resets the editing state, clears the editing ID, initializes a new form with default values, clears any previous error messages, and sets the showForm flag to true, indicating that the form should be displayed to the user.
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
    /// Opens the form for editing an existing sensor. The method takes a Sensor object as input, sets the editing state to true, stores the sensor's ID in editingId, and populates the form with the sensor's current values. It also clears any previous error messages and sets the showForm flag to true, indicating that the form should be displayed to the user for editing.
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
    /// Closes the sensor form. The method sets the showForm flag to false, indicating that the form should be hidden from the user. This action effectively cancels any ongoing creation or editing of a sensor without saving changes.
    /// </summary>
    private void CloseForm()
    {
        showForm = false;
    }
    /// <summary>
    /// Asynchronously saves the sensor data entered in the form. The method first clears any previous error messages. It then checks if the form is in editing mode; if so, it calls the UpdateSensorAsync method of the SensorService to update the existing sensor with the provided ID and form data. If not in editing mode, it calls the CreateSensorAsync method to create a new sensor with the form data. After a successful save operation, it hides the form and reloads the list of sensors. If an exception occurs during the save process, it captures and displays the error message.
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
    /// Asynchronously deletes a sensor. The method takes a Sensor object as input, clears any previous error messages, and attempts to delete the sensor by calling the DeleteSensorAsync method of the SensorService with the sensor's ID. After successfully deleting the sensor, it reloads the list of sensors. If an exception occurs during the deletion process, it captures and displays the error message.
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

            if (selectedSensor?.Id == sensor.Id)
            {
                CloseAttachments();
            }
        }
        catch (Exception ex)
        {
            error = $"Unable to delete sensor: {ex.Message}";
        }
    }
    /// <summary>
    /// Opens the attachments view for a specific sensor. The method takes a Sensor object as input, sets it as the selected sensor, and displays the attachments view by setting the showAttachments flag to true. It then attempts to retrieve the list of attachments for the selected sensor by calling the GetForSensorAsync method of the SensorAttachmentService. If successful, it populates the attachments list; if an exception occurs, it captures and displays the error message.
    /// </summary>
    /// <param name="sensor"></param>
    /// <returns></returns>
    private async Task OpenAttachments(Sensor sensor)
    {
        selectedSensor = sensor;
        showAttachments = true;
        error = null;

        try
        {
            attachments = await AttachmentService.GetForSensorAsync(sensor.Id);
        }
        catch (Exception ex)
        {
            error = $"Unable to load attachments: {ex.Message}";
        }
    }
    /// <summary>
    /// Closes the attachments view. The method resets the showAttachments flag to false, clears the selected sensor, and empties the attachments list. This action effectively hides the attachments view from the user and clears any previously displayed attachment data.
    /// </summary>
    private void CloseAttachments()
    {
        showAttachments = false;
        selectedSensor = null;
        attachments.Clear();
    }
    /// <summary>
    /// Asynchronously uploads a sensor attachment. The method takes an InputFileChangeEventArgs object as input, which contains information about the selected file. It first checks if a sensor is currently selected; if not, it returns early. It then retrieves the selected file and checks if it is null. If a file is present, it sets the uploading state to true and clears any previous error messages. The method attempts to upload the file by calling the UploadAsync method of the SensorAttachmentService with the selected sensor's ID, the selected attachment type, and the file. After a successful upload, it refreshes the list of attachments for the selected sensor. If an exception occurs during the upload process, it captures and displays the error message. Finally, it resets the uploading state to false.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    private async Task UploadAttachment(InputFileChangeEventArgs e)
    {
        if (selectedSensor is null)
        {
            return;
        }

        var file = e.File;

        if (file is null)
        {
            return;
        }

        uploading = true;
        error = null;

        try
        {
            await AttachmentService.UploadAsync(
                selectedSensor.Id,
                selectedAttachmentType,
                file);

            attachments =
                await AttachmentService.GetForSensorAsync(selectedSensor.Id);
        }
        catch (Exception ex)
        {
            error = $"Upload failed: {ex.Message}";
        }
        finally
        {
            uploading = false;
        }
    }
    /// <summary>
    /// Asynchronously deletes a specific sensor attachment. The method takes a SensorAttachment object as input, checks if a sensor is currently selected, and clears any previous error messages. It then attempts to delete the attachment by calling the DeleteAsync method of the SensorAttachmentService with the selected sensor's ID and the attachment's ID. After successfully deleting the attachment, it refreshes the list of attachments for the selected sensor. If an exception occurs during the deletion process, it captures and displays the error message.
    /// </summary>
    /// <param name="attachment"></param>
    /// <returns></returns>
    private async Task DeleteAttachment(SensorAttachment attachment)
    {
        if (selectedSensor is null)
        {
            return;
        }

        error = null;

        try
        {
            await AttachmentService.DeleteAsync(
                selectedSensor.Id,
                attachment.Id);

            attachments =
                await AttachmentService.GetForSensorAsync(selectedSensor.Id);
        }
        catch (Exception ex)
        {
            error = $"Unable to delete attachment: {ex.Message}";
        }
    }
    /// <summary>
    /// Generates a download URL for a specific sensor attachment. The method takes a SensorAttachment object as input and returns a string representing the URL that can be used to download the attachment. It utilizes the GetDownloadUrl method of the SensorAttachmentService, passing in the sensor's ID and the attachment's ID to construct the appropriate download link.
    /// </summary>
    /// <param name="attachment"></param>
    /// <returns></returns>
    private string GetDownloadUrl(SensorAttachment attachment)
    {
        return AttachmentService.GetDownloadUrl(
            attachment.SensorId,
            attachment.Id);
    }
    /// <summary>
    /// Determines if a sensor attachment is an image. The method takes a SensorAttachment object as input and returns a boolean value indicating whether the attachment is an image based on its content type.
    /// </summary>
    /// <param name="attachment"></param>
    /// <returns></returns>
    private static bool IsImage(SensorAttachment attachment)
    {
        return attachment.ContentType.StartsWith(
            "image/",
            StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>
    /// Creates a new instance of CreateSensorRequest with default values. This method is used to initialize the form for creating a new sensor, providing default settings for various properties such as DeviceId, MacAddress, Location, Category, TelemetryType, SamplingIntervalSeconds, NormalMin, and NormalMax.
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
