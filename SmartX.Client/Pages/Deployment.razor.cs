using System.Net.Http.Json;
using MudBlazor;
using SmartX.Shared.Models;

namespace SmartX.Client.Pages;

public partial class Deployment
{
    private DeploymentNode root = new()
    {
        Name = "SmartX Deployment",
        NodeType = "Root"
    };

    private readonly List<string> validationErrors = [];

    private string resultMessage = string.Empty;

    private Severity resultSeverity = Severity.Info;
    /// <summary>
    /// Validates the current deployment structure by sending it to the server for validation. The method clears any previous validation errors and result messages, then sends a POST request to the "api/deployment/validate" endpoint with the root deployment node as the payload. It checks the response status and processes the validation result, updating the validationErrors list and setting the resultMessage and resultSeverity accordingly. If an exception occurs during the request, it sets an error message and severity.
    /// </summary>
    /// <returns></returns>
    private void AddChild()
    {
        root.Children.Add(new DeploymentNode
        {
            Name = string.Empty,
            NodeType = string.Empty
        });
    }
    /// <summary>
    /// Removes a child deployment node from the root node's children list. The method takes a DeploymentNode object as input and removes it from the root.Children collection. This allows for dynamic modification of the deployment structure by removing specific child nodes.
    /// </summary>
    /// <param name="child"></param>
    private void RemoveChild(DeploymentNode child)
    {
        root.Children.Remove(child);
    }
    /// <summary>
    /// Validates the current deployment structure by sending it to the server for validation. The method clears any previous validation errors and result messages, then sends a POST request to the "api/deployment/validate" endpoint with the root deployment node as the payload. It checks the response status and processes the validation result, updating the validationErrors list and setting the resultMessage and resultSeverity accordingly. If an exception occurs during the request, it sets an error message and severity.
    /// </summary>
    /// <returns></returns>
    private async Task ValidateDeployment()
    {
        validationErrors.Clear();
        resultMessage = string.Empty;

        try
        {
            var response = await Http.PostAsJsonAsync("api/deployment/validate", root);

            if (!response.IsSuccessStatusCode)
            {
                resultMessage = $"Validation request failed: {response.StatusCode}";
                resultSeverity = Severity.Error;
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<DeploymentValidationResult>();

            if (result is null)
            {
                resultMessage = "No validation result was returned.";
                resultSeverity = Severity.Error;
                return;
            }

            validationErrors.AddRange(result.Errors);

            if (result.Valid)
            {
                resultMessage = "Deployment structure is valid.";
                resultSeverity = Severity.Success;
            }
            else
            {
                resultMessage = $"Deployment contains {result.Errors.Count} validation error(s).";
                resultSeverity = Severity.Error;
            }
        }
        catch (Exception ex)
        {
            resultMessage = $"Validation failed: {ex.Message}";
            resultSeverity = Severity.Error;
        }
    }
    /// <summary>
    /// Represents the result of a deployment validation operation, including whether the deployment is valid and any associated error messages. This class is used to encapsulate the outcome of validating a deployment structure, providing a boolean flag indicating validity and a list of error messages detailing any issues found during validation.
    /// </summary>
    private sealed class DeploymentValidationResult
    {
        public bool Valid { get; set; }

        public List<string> Errors { get; set; } = [];
    }
}