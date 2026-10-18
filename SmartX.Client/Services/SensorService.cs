using System.Net.Http.Json;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Client.Services;

public class SensorService
{
    private readonly HttpClient _httpClient;

    public SensorService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    /// <summary>
    /// Retrieves a list of sensors from the API asynchronously. If the API call is successful, it returns the list of sensors; otherwise, it returns an empty list. The method uses the HttpClient to send a GET request to the "api/sensors" endpoint and deserializes the JSON response into a List of Sensor objects.
    /// </summary>
    /// <returns></returns>
    public async Task<List<Sensor>> GetSensorsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<Sensor>>("api/sensors") ?? new List<Sensor>();
    }
    /// <summary>
    /// Creates a new sensor by sending a POST request to the API asynchronously. The method takes a CreateSensorRequest object as input, which contains the necessary information for creating the sensor. It uses the HttpClient to send the request to the "api/sensors" endpoint and ensures that the response indicates a successful operation. If the API call fails, an exception is thrown.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task CreateSensorAsync(CreateSensorRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/sensors", request);

        response.EnsureSuccessStatusCode();
    }
    /// <summary>
    /// Updates an existing sensor by sending a PUT request to the API asynchronously. The method takes a sensor ID (Guid) and a CreateSensorRequest object containing the updated information for the sensor. It constructs the request and sends it to the "api/sensors/{id}" endpoint. The method ensures that the response indicates a successful operation, throwing an exception if the API call fails.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task UpdateSensorAsync(Guid id, CreateSensorRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/sensors/{id}", request);

        response.EnsureSuccessStatusCode();
    }
    /// <summary>
    /// Updates the active status of a sensor by sending a PUT request to the API asynchronously. The method takes a sensor ID (Guid) and a boolean value indicating whether the sensor should be active or inactive. It constructs an UpdateSensorActiveRequest object with the provided isActive value and sends it to the "api/sensors/{id}/active" endpoint. The method ensures that the response indicates a successful operation, throwing an exception if the API call fails.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isActive"></param>
    /// <returns></returns>
    public async Task UpdateActiveAsync(Guid id, bool isActive)
    {
        var request = new UpdateSensorActiveRequest(isActive);

        var response = await _httpClient.PutAsJsonAsync($"api/sensors/{id}/active", request);

        response.EnsureSuccessStatusCode();
    }
    public async Task DeleteSensorAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/sensors/{id}");

        response.EnsureSuccessStatusCode();
    }
}
