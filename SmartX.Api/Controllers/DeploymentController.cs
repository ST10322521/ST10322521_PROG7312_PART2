using Microsoft.AspNetCore.Mvc;
using SmartX.Shared.Models;
using SmartX.Shared.Validation;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeploymentController : ControllerBase
{
    /// <summary>
    /// Validates a DeploymentNode hierarchy and returns a list of validation error messages. The method uses the DeploymentValidation class to perform the validation, which checks for required properties such as Name and NodeType in each node of the hierarchy. If any validation errors are found, they are returned in the response; otherwise, an empty list is returned indicating that the deployment is valid.
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    [HttpPost("validate")]
    public IActionResult ValidateDeployment([FromBody] DeploymentNode root)
    {
        var errors = DeploymentValidation.Validate(root);

        return Ok(new
        {
            valid = errors.Count == 0,
            errors
        });
    }
}