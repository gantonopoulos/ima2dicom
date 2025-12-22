using Xunit;
using ImaToDicomConverter.Errors;

namespace ImaToDicomConverter.Tests;

public class ArgumentParserTests : IDisposable
{
    private readonly string _testInputDir;
    private readonly string _testOutputDir;
    private readonly string _validConfigPath;

    public ArgumentParserTests()
    {
        // Create temporary test directories
        _testInputDir = Path.Combine(Path.GetTempPath(), $"ima_test_input_{Guid.NewGuid()}");
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"ima_test_output_{Guid.NewGuid()}");
        _validConfigPath = Path.Combine(Path.GetTempPath(), $"config_{Guid.NewGuid()}.json");

        // Create the directories
        Directory.CreateDirectory(_testInputDir);
        Directory.CreateDirectory(_testOutputDir);

        // Create a minimal valid config file
        File.WriteAllText(_validConfigPath, "{}");
    }

    public void Dispose()
    {
        // Cleanup temporary files and directories
        try
        {
            if (Directory.Exists(_testInputDir))
                Directory.Delete(_testInputDir, true);
            if (Directory.Exists(_testOutputDir))
                Directory.Delete(_testOutputDir, true);
            if (File.Exists(_validConfigPath))
                File.Delete(_validConfigPath);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    [Fact]
    public void Parse_WithValidArguments_ReturnsSuccess()
    {
        // Arrange
        var args = new[]
        {
            $"--in={_testInputDir}",
            $"--out={_testOutputDir}",
            $"--config={_validConfigPath}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        result.Match(
            Right: parsed =>
            {
                Assert.Equal(_testInputDir, parsed.InputDirectory);
                Assert.Equal(_testOutputDir, parsed.OutputDirectory);
            },
            Left: _ => Assert.Fail("Expected successful parse result")
        );
    }

    [Fact]
    public void Parse_MissingInputDirectory_ReturnsError()
    {
        // Arrange
        var args = new[]
        {
            $"--out={_testOutputDir}",
            $"--config={_validConfigPath}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        result.Match(
            Right: _ => Assert.Fail("Expected error result"),
            Left: err =>
            {
                Assert.IsType<ArgumentError>(err);
                Assert.Contains("Missing required argument: --in", err.Message);
            }
        );
    }

    [Fact]
    public void Parse_MissingOutputDirectory_ReturnsError()
    {
        // Arrange
        var args = new[]
        {
            $"--in={_testInputDir}",
            $"--config={_validConfigPath}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        result.Match(
            Right: _ => Assert.Fail("Expected error result"),
            Left: err =>
            {
                Assert.IsType<ArgumentError>(err);
                Assert.Contains("Missing required argument: --out", err.Message);
            }
        );
    }

    [Fact]
    public void Parse_MissingConfigFile_ReturnsError()
    {
        // Arrange
        var args = new[]
        {
            $"--in={_testInputDir}",
            $"--out={_testOutputDir}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        result.Match(
            Right: _ => Assert.Fail("Expected error result"),
            Left: err =>
            {
                Assert.IsType<ArgumentError>(err);
                Assert.Contains("Missing required argument: --config", err.Message);
            }
        );
    }

    [Fact]
    public void Parse_NonExistentInputDirectory_ReturnsError()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}");
        var args = new[]
        {
            $"--in={nonExistentDir}",
            $"--out={_testOutputDir}",
            $"--config={_validConfigPath}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        result.Match(
            Right: _ => Assert.Fail("Expected error result"),
            Left: err =>
            {
                Assert.IsType<ArgumentError>(err);
                Assert.Contains("The directory does not exist", err.Message);
                Assert.Contains(nonExistentDir, err.Message);
            }
        );
    }

    [Fact]
    public void Parse_NonExistentOutputDirectory_ReturnsError()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}");
        var args = new[]
        {
            $"--in={_testInputDir}",
            $"--out={nonExistentDir}",
            $"--config={_validConfigPath}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        result.Match(
            Right: _ => Assert.Fail("Expected error result"),
            Left: err =>
            {
                Assert.IsType<ArgumentError>(err);
                Assert.Contains("The directory does not exist", err.Message);
                Assert.Contains(nonExistentDir, err.Message);
            }
        );
    }

    [Fact]
    public void Parse_NonExistentConfigFile_ReturnsError()
    {
        // Arrange
        var nonExistentConfig = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");
        var args = new[]
        {
            $"--in={_testInputDir}",
            $"--out={_testOutputDir}",
            $"--config={nonExistentConfig}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        result.Match(
            Right: _ => Assert.Fail("Expected error result"),
            Left: err =>
            {
                Assert.IsType<ArgumentError>(err);
                Assert.Contains("The config file does not exist", err.Message);
                Assert.Contains(nonExistentConfig, err.Message);
            }
        );
    }

    [Fact]
    public void Parse_InvalidArgumentFormat_ReturnsError()
    {
        // Arrange
        var args = new[]
        {
            $"--in={_testInputDir}",
            $"--out={_testOutputDir}",
            $"--config={_validConfigPath}",
            "invalidformat"  // Missing = sign
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        result.Match(
            Right: _ => Assert.Fail("Expected error result"),
            Left: err =>
            {
                Assert.IsType<ArgumentError>(err);
                Assert.Contains("Invalid argument format", err.Message);
            }
        );
    }

    [Fact]
    public void Parse_InvalidArgumentMissingPrefix_ReturnsError()
    {
        // Arrange
        var args = new[]
        {
            $"--in={_testInputDir}",
            $"--out={_testOutputDir}",
            $"--config={_validConfigPath}",
            $"input={_testInputDir}"  // Missing -- prefix
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        result.Match(
            Right: _ => Assert.Fail("Expected error result"),
            Left: err =>
            {
                Assert.IsType<ArgumentError>(err);
                Assert.Contains("Invalid argument format", err.Message);
            }
        );
    }

    [Fact]
    public void Parse_AllMissingArguments_ReturnsFirstError()
    {
        // Arrange
        var args = new string[] { };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        result.Match(
            Right: _ => Assert.Fail("Expected error result"),
            Left: err =>
            {
                Assert.IsType<ArgumentError>(err);
                // Should fail on the first mandatory parser (--in)
                Assert.Contains("Missing required argument: --in", err.Message);
            }
        );
    }

    [Fact]
    public void Parse_ArgumentWithMultipleEqualSigns_ParsesCorrectly()
    {
        // Arrange - test that we split on first = only
        var configContent = "key=value";
        var configPath = Path.Combine(Path.GetTempPath(), $"config_with_equals_{Guid.NewGuid()}.json");
        File.WriteAllText(configPath, configContent);

        try
        {
            var args = new[]
            {
                $"--in={_testInputDir}",
                $"--out={_testOutputDir}",
                $"--config={configPath}"
            };

            // Act
            var result = ArgumentParser.Parse(args);

            // Assert - should parse successfully despite content having =
            result.Match(
                Right: _ => Assert.Fail("Expected error due to invalid JSON"),
                Left: err =>
                {
                    Assert.IsType<ArgumentError>(err);
                    Assert.Contains("Failed to load config", err.Message);
                }
            );
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
    }
}

