using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Client.Services;

public class SensorService
{
    private readonly HttpClient _httpClient;
    private List<Sensor>? _cachedSensors;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public SensorService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    /// <summary>
    /// Retrieves a list of sensors from the API asynchronously. The method checks if the sensors are already cached; if so, it returns the cached list. Otherwise, it sends a GET request to the "api/sensors" endpoint, deserializes the response into a list of Sensor objects, and caches the result for future calls. If the response is null, it returns an empty list.
    /// </summary>
    /// <returns></returns>
    public async Task<List<Sensor>> GetSensorsAsync()
    {
        if (_cachedSensors != null)
        {
            return _cachedSensors;
        }

        _cachedSensors =
            await _httpClient.GetFromJsonAsync<List<Sensor>>(
                "api/sensors",
                JsonOptions) ?? new List<Sensor>();

        return _cachedSensors;
    }
    /// <summary>
    /// Creates a new sensor by sending a POST request to the API asynchronously. The method takes a CreateSensorRequest object as input, which contains the necessary information for creating the sensor. It sends the request to the "api/sensors" endpoint and checks the response for success. If the API call fails, it throws an exception with the error message from the response body.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task CreateSensorAsync(CreateSensorRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/sensors", request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create sensor: {body}");
        }
    }
    /// <summary>
    /// Updates an existing sensor by sending a PUT request to the API asynchronously. The method takes a sensor ID (Guid) and a CreateSensorRequest object containing the updated information for the sensor. It sends the request to the "api/sensors/{id}" endpoint and checks the response for success. If the API call fails, it throws an exception with the error message from the response body.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task UpdateSensorAsync(Guid id, CreateSensorRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/sensors/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to update sensor: {body}");
        }

        _cachedSensors = null;
    }
    /// <summary>
    /// Updates the active status of a sensor by sending a PATCH request to the API asynchronously.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isActive"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task UpdateActiveAsync(Guid id, bool isActive)
    {
        var request = new UpdateSensorActiveRequest(isActive);

        var response = await _httpClient.PatchAsJsonAsync($"api/sensors/{id}/active", request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to update sensor active status: {body}");
        }

        _cachedSensors = null;
    }
    /// <summary>
    /// Deletes a sensor by sending a DELETE request to the API asynchronously.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task DeleteSensorAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/sensors/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to delete sensor: {body}");
        }


        _cachedSensors = null;
    }
}
