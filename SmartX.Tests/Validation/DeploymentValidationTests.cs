using SmartX.Shared.Models;
using SmartX.Shared.Validation;

namespace SmartX.Tests.Validation;

public class DeploymentValidationTests
{
    [Fact]
    public void Validate_ValidHierarchy_ReturnsNoErrors()
    {
        var sensorId = Guid.NewGuid();

        var root = new DeploymentNode
        {
            Name = "Facility A",
            NodeType = "Facility",
            Children =
            [
                new DeploymentNode
                {
                    Name = "Zone 1",
                    NodeType = "Zone",
                    Children =
                    [
                        new DeploymentNode
                        {
                            Name = "Node 42",
                            NodeType = "Node",
                            SensorId = sensorId
                        }
                    ]
                }
            ]
        };

        var errors = DeploymentValidation.Validate(root);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_InvalidRoot_ReturnsErrors()
    {
        var root = new DeploymentNode
        {
            Name = "",
            NodeType = ""
        };

        var errors = DeploymentValidation.Validate(root);

        Assert.Contains(
            "[unnamed root]: Name is required.",
            errors);

        Assert.Contains(
            "[unnamed root]: NodeType is required.",
            errors);
    }

    [Fact]
    public void Validate_InvalidNestedNode_ReturnsNestedError()
    {
        var root = new DeploymentNode
        {
            Name = "Facility A",
            NodeType = "Facility",
            Children =
            [
                new DeploymentNode
                {
                    Name = "Zone 1",
                    NodeType = "Zone",
                    Children =
                    [
                        new DeploymentNode
                        {
                            Name = "Node 42",
                            NodeType = ""
                        }
                    ]
                }
            ]
        };

        var errors = DeploymentValidation.Validate(root);

        Assert.Contains(
            "Facility A / Zone 1 / Node 42: NodeType is required.",
            errors);
    }

    [Fact]
    public void Validate_DeepHierarchy_RecursivelyValidatesAllLevels()
    {
        var root = new DeploymentNode
        {
            Name = "Facility A",
            NodeType = "Facility",
            Children =
            [
                new DeploymentNode
                {
                    Name = "Zone 1",
                    NodeType = "Zone",
                    Children =
                    [
                        new DeploymentNode
                        {
                            Name = "Sub-Zone B",
                            NodeType = "SubZone",
                            Children =
                            [
                                new DeploymentNode
                                {
                                    Name = "Node 42",
                                    NodeType = "Node",
                                    Children =
                                    [
                                        new DeploymentNode
                                        {
                                            Name = "Sensor Node",
                                            NodeType = ""
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var errors = DeploymentValidation.Validate(root);

        Assert.Contains(
            "Facility A / Zone 1 / Sub-Zone B / Node 42 / Sensor Node: NodeType is required.",
            errors);
    }

    [Fact]
    public void Validate_MultipleInvalidChildren_ReturnsAllErrors()
    {
        var root = new DeploymentNode
        {
            Name = "Facility A",
            NodeType = "Facility",
            Children =
            [
                new DeploymentNode
                {
                    Name = "Zone 1",
                    NodeType = ""
                },
                new DeploymentNode
                {
                    Name = "Zone 2",
                    NodeType = ""
                }
            ]
        };

        var errors = DeploymentValidation.Validate(root);

        Assert.Equal(2, errors.Count);

        Assert.Contains(
            "Facility A / Zone 1: NodeType is required.",
            errors);

        Assert.Contains(
            "Facility A / Zone 2: NodeType is required.",
            errors);
    }
}