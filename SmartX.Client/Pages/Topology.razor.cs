using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SmartX.Shared.Models.Topology;

namespace SmartX.Client.Pages;

/// <summary>
/// Represents the Topology page in the SmartX client application, which displays the current network topology, including sensors and their statuses. The page retrieves the topology data from the API and provides visual indicators for online/offline sensor statuses.
/// </summary>
public partial class Topology
{
    [Inject]
    private HttpClient Http { get; set; } = null!;

    private NetworkTopology? topology;
    private bool loading = true;
    private string? errorMessage;

    /// <summary>
    /// Gets the count of online nodes in the network topology.
    /// </summary>
    private int OnlineNodeCount => topology?.Nodes.Count(n =>
            string.Equals(n.Status, "Online", StringComparison.OrdinalIgnoreCase)) ?? 0;

    /// <summary>
    /// Gets the count of offline nodes in the network topology.
    /// </summary>
    private int OfflineNodeCount => topology?.Nodes.Count(n =>
            string.Equals(n.Status, "Offline", StringComparison.OrdinalIgnoreCase)) ?? 0;

    /// <summary>
    /// Asynchronously initializes the component by loading the network topology data from the API. If an error occurs during the data retrieval, an error message is set for display. The loading state is managed to provide feedback to the user while the data is being fetched.
    /// </summary>
    /// <returns></returns>
    protected override async Task OnInitializedAsync()
    {
        await LoadTopologyAsync();
    }

    /// <summary>
    /// Asynchronously loads the network topology data from the API. The method sets the loading state to true while fetching the data and handles any exceptions that may occur during the HTTP request. If successful, the topology data is stored; otherwise, an error message is set for display. Finally, the loading state is reset to false.
    /// </summary>
    /// <returns></returns>
    private async Task LoadTopologyAsync()
    {
        loading = true;
        errorMessage = null;

        try
        {
            topology = await Http.GetFromJsonAsync<NetworkTopology>(
                "api/NetworkTopology");
        }
        catch (Exception ex)
        {
            errorMessage = $"Unable to load network topology: {ex.Message}";
        }
        finally
        {
            loading = false;
        }
    }

    /// <summary>
    /// Gets the color associated with a given sensor status. The method maps the status string to a corresponding MudBlazor Color enumeration value, which can be used for visual indicators in the UI. The mapping is case-insensitive and defaults to Color.Default for unrecognized statuses.
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    private static Color GetStatusColor(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "online" => Color.Success,
            "offline" => Color.Error,
            "warning" => Color.Warning,
            _ => Color.Default
        };
    }

    /// <summary>
    /// Gets the severity level associated with a given sensor status. The method maps the status string to a corresponding Severity enumeration value, which can be used for visual indicators in the UI. The mapping is case-insensitive and defaults to Severity.Info for unrecognized statuses.
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    private static Severity GetStatusSeverity(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "online" => Severity.Success,
            "offline" => Severity.Error,
            "warning" => Severity.Warning,
            _ => Severity.Info
        };
    }
}