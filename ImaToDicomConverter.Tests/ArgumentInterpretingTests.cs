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
            // Both should use the same directory (current directory)
            Assert.Equal(parsed.InputDirectory, parsed.OutputDirectory);
            Assert.NotNull(parsed.Config);
            // Verify it's an absolute path (implementation uses Directory.GetCurrentDirectory())
            Assert.True(Path.IsPathRooted(parsed.InputDirectory));
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
            // Output should be an absolute path (current directory is used)
            Assert.True(Path.IsPathRooted(parsed.OutputDirectory));
            // Output should not be the same as input when only input is specified
            Assert.NotEqual(_testInputDir, parsed.OutputDirectory);
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
            // Input should be an absolute path (current directory is used)
            Assert.True(Path.IsPathRooted(parsed.InputDirectory));
            Assert.Equal(_testOutputDir, parsed.OutputDirectory);
            // Input should not be the same as output when only output is specified
            Assert.NotEqual(_testOutputDir, parsed.InputDirectory);
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
    public void InterpretArguments_NonExistentOutputDirectory_CreatesDirectory()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"new_output_{Guid.NewGuid()}");
        _cleanupPaths.Add(nonExistentDir); // Add to cleanup list
        
        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = nonExistentDir
        };

        // Verify directory doesn't exist before the call
        Assert.False(Directory.Exists(nonExistentDir));

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.Equal(nonExistentDir, parsed.OutputDirectory);
            Assert.True(Directory.Exists(nonExistentDir), "Output directory should have been created");
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

    [Fact]
    public void InterpretArguments_OutputDirectoryWithInvalidCharacters_ReturnsError()
    {
        // Arrange
        // Use invalid path characters that will cause Directory.CreateDirectory to fail
        var invalidPath = Path.Combine(Path.GetTempPath(), "invalid\0path");
        
        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = invalidPath
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.IsType<ArgumentError>(error);
            Assert.Contains("output directory", error.Message.ToLower());
        });
    }

    [Fact]
    public void InterpretArguments_OutputDirectoryAlreadyExists_Succeeds()
    {
        // Arrange
        // Use existing output directory
        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = _testOutputDir // This already exists
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.Equal(_testOutputDir, parsed.OutputDirectory);
            Assert.True(Directory.Exists(_testOutputDir));
        });
    }

    [Fact]
    public void InterpretArguments_NestedNonExistentOutputDirectory_CreatesAllLevels()
    {
        // Arrange
        var nestedDir = Path.Combine(Path.GetTempPath(), $"level1_{Guid.NewGuid()}", "level2", "level3");
        _cleanupPaths.Add(Path.Combine(Path.GetTempPath(), Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(nestedDir))!)));
        
        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = nestedDir
        };

        // Verify directory doesn't exist before the call
        Assert.False(Directory.Exists(nestedDir));

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.Equal(nestedDir, parsed.OutputDirectory);
            Assert.True(Directory.Exists(nestedDir), "Nested output directory should have been created");
        });
    }
}


