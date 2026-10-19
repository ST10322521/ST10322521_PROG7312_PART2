using SmartX.Shared.Models;

namespace SmartX.Shared.Validation;

public static class DeploymentValidation
{
    /// <summary>
    /// Validates a DeploymentNode hierarchy, returning a list of validation error messages.
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public static List<string> Validate(DeploymentNode root)
    {
        var errors = new List<string>();

        var rootPath = string.IsNullOrWhiteSpace(root.Name)
            ? "[unnamed root]"
            : root.Name;

        ValidateNode(root, rootPath, errors);

        return errors;
    }
    /// <summary>
    /// Recursively validates a DeploymentNode and its children, collecting any validation errors.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="path"></param>
    /// <param name="errors"></param>
    private static void ValidateNode(
        DeploymentNode node,
        string path,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(node.Name))
        {
            errors.Add($"{path}: Name is required.");
        }

        if (string.IsNullOrWhiteSpace(node.NodeType))
        {
            errors.Add($"{path}: NodeType is required.");
        }

        for (var childIndex = 0; childIndex < node.Children.Count; childIndex++)
        {
            var child = node.Children[childIndex];

            var childPath = string.IsNullOrWhiteSpace(child.Name)
                ? $"{path} / [unnamed child {childIndex + 1}]"
                : $"{path} / {child.Name}";

            ValidateNode(child, childPath, errors);
        }
    }
}