using ImaToDicomConverter.ApplicationArguments;
using ImaToDicomConverter.Errors;
using Xunit;

namespace ImaToDicomConverter.Tests;

/// <summary>
/// Tests for the ArgumentInterpreting class that interprets collected arguments
/// and applies business logic (defaults, validation, config loading).
/// </summary>
public class ArgumentInterpretingTests : IDisposable
{
    private readonly string _testInputDir;
    private readonly string _testOutputDir;
    private readonly string _validConfigPath;
    private readonly List<string> _cleanupPaths = new();

    public ArgumentInterpretingTests()
    {
        // Create temporary test directories
        _testInputDir = Path.Combine(Path.GetTempPath(), $"ima_test_input_{Guid.NewGuid()}");
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"ima_test_output_{Guid.NewGuid()}");
        _validConfigPath = Path.Combine(Path.GetTempPath(), $"config_{Guid.NewGuid()}.json");

        // Create the directories
        Directory.CreateDirectory(_testInputDir);
        Directory.CreateDirectory(_testOutputDir);

        _cleanupPaths.Add(_testInputDir);
        _cleanupPaths.Add(_testOutputDir);

        // Create a minimal valid config file
        var minimalConfig = """
        {
            "Rows": 512,
            "Columns": 512
        }
        """;
        File.WriteAllText(_validConfigPath, minimalConfig);
        _cleanupPaths.Add(_validConfigPath);
    }

    public void Dispose()
    {
        // Cleanup temporary files and directories
        foreach (var path in _cleanupPaths)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
                else if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    [Fact]
    public void InterpretArguments_NoArguments_UsesCurrentDirectoryForInAndOut()
    {
        // Arrange
        var lookup = new Dictionary<string, string>();

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.Equal(Directory.GetCurrentDirectory(), parsed.InputDirectory);
            Assert.Equal(Directory.GetCurrentDirectory(), parsed.OutputDirectory);
            Assert.NotNull(parsed.Config);
        });
    }

    [Fact]
    public void InterpretArguments_OnlyInputDirectory_UsesCurrentDirForOutput()
    {
        // Arrange
        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.Equal(_testInputDir, parsed.InputDirectory);
            Assert.Equal(Directory.GetCurrentDirectory(), parsed.OutputDirectory);
        });
    }

    [Fact]
    public void InterpretArguments_OnlyOutputDirectory_UsesCurrentDirForInput()
    {
        // Arrange
        var lookup = new Dictionary<string, string>
        {
            ["out"] = _testOutputDir
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.Equal(Directory.GetCurrentDirectory(), parsed.InputDirectory);
            Assert.Equal(_testOutputDir, parsed.OutputDirectory);
        });
    }

    [Fact]
    public void InterpretArguments_BothDirectoriesSpecified_UsesBoth()
    {
        // Arrange
        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = _testOutputDir
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.Equal(_testInputDir, parsed.InputDirectory);
            Assert.Equal(_testOutputDir, parsed.OutputDirectory);
        });
    }

    [Fact]
    public void InterpretArguments_NonExistentInputDirectory_ReturnsError()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}");
        var lookup = new Dictionary<string, string>
        {
            ["in"] = nonExistentDir
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.IsType<ArgumentError>(error);
            Assert.Contains("does not exist", error.Message);
            Assert.Contains(nonExistentDir, error.Message);
        });
    }

    [Fact]
    public void InterpretArguments_NonExistentOutputDirectory_ReturnsError()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}");
        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = nonExistentDir
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.IsType<ArgumentError>(error);
            Assert.Contains("does not exist", error.Message);
        });
    }

    [Fact]
    public void InterpretArguments_CustomConfigFile_LoadsConfiguration()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), $"custom_config_{Guid.NewGuid()}.json");
        var json = """
        {
            "Modality": "CT",
            "Rows": 256,
            "Columns": 256
        }
        """;
        File.WriteAllText(configPath, json);
        _cleanupPaths.Add(configPath);

        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = _testOutputDir,
            ["config"] = configPath
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.True(parsed.Config.Modality.IsSome);
            parsed.Config.Modality.IfSome(v => Assert.Equal("CT", v));
            Assert.True(parsed.Config.Rows.IsSome);
            parsed.Config.Rows.IfSome(v => Assert.Equal((ushort)256, v));
        });
    }

    [Fact]
    public void InterpretArguments_NoConfigFile_LoadsDefaultConfiguration()
    {
        // Arrange
        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = _testOutputDir
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.NotNull(parsed.Config);
            // The default config should be loaded
        });
    }

    [Fact]
    public void InterpretArguments_NonExistentConfigFile_ReturnsError()
    {
        // Arrange
        var nonExistentConfig = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");
        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = _testOutputDir,
            ["config"] = nonExistentConfig
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.IsType<ArgumentError>(error);
            Assert.Contains("does not exist", error.Message);
            Assert.Contains(nonExistentConfig, error.Message);
        });
    }

    [Fact]
    public void InterpretArguments_InvalidJsonInConfigFile_ReturnsError()
    {
        // Arrange
        var invalidConfigPath = Path.Combine(Path.GetTempPath(), $"invalid_config_{Guid.NewGuid()}.json");
        File.WriteAllText(invalidConfigPath, "{ invalid json }");
        _cleanupPaths.Add(invalidConfigPath);

        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = _testOutputDir,
            ["config"] = invalidConfigPath
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.IsType<ArgumentError>(error);
            Assert.Contains("Failed to load config", error.Message);
        });
    }
}

