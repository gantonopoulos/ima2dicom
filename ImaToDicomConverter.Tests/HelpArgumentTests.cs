using Xunit;

namespace ImaToDicomConverter.Tests;

/// <summary>
/// Tests for the help argument functionality.
/// When --help is present, the application should print usage and terminate without processing other arguments.
/// </summary>
public class HelpArgumentTests
{
    [Fact]
    public void CollectArguments_HelpArgument_CollectsSuccessfully()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(lookup =>
        {
            Assert.True(lookup.ContainsKey("help"));
        });
    }

    [Fact]
    public void CollectArguments_HelpWithOtherArguments_CollectsAll()
    {
        // Arrange
        var args = new[] { "--in=/some/path", "--help", "--out=/other/path" };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(lookup =>
        {
            Assert.True(lookup.ContainsKey("help"));
            Assert.True(lookup.ContainsKey("in"));
            Assert.True(lookup.ContainsKey("out"));
        });
    }

    [Fact]
    public void CollectArguments_HelpAsLastArgument_CollectsSuccessfully()
    {
        // Arrange
        var args = new[] { "--in=/some/path", "--out=/other/path", "--help" };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(lookup =>
        {
            Assert.True(lookup.ContainsKey("help"));
        });
    }

    [Fact]
    public void ArgumentNameEnum_Help_ConvertsToCorrectString()
    {
        // Arrange & Act
        var helpString = Argument.Help.AsString();
        var helpCliString = Argument.Help.AsCliString();

        // Assert
        Assert.Equal("help", helpString);
        Assert.Equal("--help", helpCliString);
    }
}

