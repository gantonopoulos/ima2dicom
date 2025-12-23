using ImaToDicomConverter.Errors;
using Xunit;

namespace ImaToDicomConverter.Tests;

/// <summary>
/// Tests for the ArgumentCollecting class that handles parsing raw command-line arguments
/// into a dictionary lookup.
/// </summary>
public class ArgumentCollectingTests
{
    [Fact]
    public void CollectArguments_NoArguments_ReturnsEmptyDictionary()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(Assert.Empty);
    }

    [Fact]
    public void CollectArguments_ValidArguments_ReturnsDictionary()
    {
        // Arrange
        var args = new[]
        {
            "--in=/path/to/input",
            "--out=/path/to/output",
            "--config=/path/to/config.json"
        };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(lookup =>
        {
            Assert.Equal(3, lookup.Count);
            Assert.Equal("/path/to/input", lookup["in"]);
            Assert.Equal("/path/to/output", lookup["out"]);
            Assert.Equal("/path/to/config.json", lookup["config"]);
        });
    }

    [Fact]
    public void CollectArguments_FlagWithoutValue_ReturnsEmptyString()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(lookup =>
        {
            Assert.Single(lookup);
            Assert.True(lookup.ContainsKey("help"));
            Assert.Equal(string.Empty, lookup["help"]);
        });
    }

    [Fact]
    public void CollectArguments_InvalidFormat_ReturnsError()
    {
        // Arrange
        var args = new[] { "invalid_format" };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.IsType<ArgumentError>(error);
            Assert.Contains("Invalid argument format", error.Message);
        });
    }

    [Fact]
    public void CollectArguments_MissingDoubleDash_ReturnsError()
    {
        // Arrange
        var args = new[] { "in=/path/to/input" };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.IsType<ArgumentError>(error);
        });
    }

    [Fact]
    public void CollectArguments_ValueContainsEquals_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "--config=/path/with=equals/config.json" };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(lookup =>
        {
            Assert.Equal("/path/with=equals/config.json", lookup["config"]);
        });
    }

    [Fact]
    public void CollectArguments_MultipleArguments_AllParsed()
    {
        // Arrange
        var args = new[]
        {
            "--in=/input",
            "--out=/output",
            "--config=/config.json",
            "--genconf"
        };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(lookup =>
        {
            Assert.Equal(4, lookup.Count);
            Assert.True(lookup.ContainsKey("in"));
            Assert.True(lookup.ContainsKey("out"));
            Assert.True(lookup.ContainsKey("config"));
            Assert.True(lookup.ContainsKey("genconf"));
        });
    }

    [Fact]
    public void CollectArguments_OneInvalidAmongMany_ReturnsError()
    {
        // Arrange
        var args = new[]
        {
            "--in=/input",
            "invalid",
            "--out=/output"
        };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsLeft);
    }
}

