namespace SmartX.Client.Models;

public sealed class ActiveAlertsResponse
{
    public List<string> ActiveErrors { get; set; } = new();
    public List<string> DisconnectedDevices { get; set; } = new();
}