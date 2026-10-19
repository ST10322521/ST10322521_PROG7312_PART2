using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Models;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly RecommendationService _recommendationService;

    public RecommendationsController(
        RecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }
    /// <summary>
    /// Retrieves a list of recommendations based on the current state of devices and user interactions. The method calls the GetRecommendations method of the RecommendationService to generate the recommendations, which may include suggestions for devices that are in a "Critical" or "Disconnected" state, as well as devices that have been frequently searched while having an active problem. The recommendations are returned as an HTTP 200 OK response with the list of recommendations in the response body.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult GetRecommendations()
    {
        var recommendations = _recommendationService.GetRecommendations();

        return Ok(recommendations);
    }
    /// <summary>
    /// Records a user interaction with a device, such as a command or search action. The method validates the incoming UserInteraction object to ensure that the Action property is provided. If valid, it calls the RecordInteraction method of the RecommendationService to store the interaction for future recommendation generation. The method returns an HTTP 200 OK response with a message indicating that the interaction has been recorded successfully.
    /// </summary>
    /// <param name="interaction"></param>
    /// <returns></returns>
    [HttpPost("interactions")]
    public IActionResult RecordInteraction(UserInteraction interaction)
    {
        if (string.IsNullOrWhiteSpace(interaction.Action))
        {
            return BadRequest(new { error = "Action is required." });
        }

        _recommendationService.RecordInteraction(interaction);

        return Ok(new
        {
            message = "Interaction recorded successfully."
        });
    }
}