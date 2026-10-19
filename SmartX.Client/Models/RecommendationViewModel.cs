namespace SmartX.Client.Models;

public sealed class RecommendationViewModel
{
    public string DeviceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string SuggestedCommand { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
}