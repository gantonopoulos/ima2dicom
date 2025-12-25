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
        var currentDir = Directory.GetCurrentDirectory();

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            // Input should use current directory
            Assert.Equal(currentDir, parsed.InputDirectory);
            // Output should be a timestamped copy of current directory since it exists
            Assert.NotEqual(currentDir, parsed.OutputDirectory);
            Assert.StartsWith(currentDir, parsed.OutputDirectory);
            Assert.Matches(@"\d{14}$", parsed.OutputDirectory);
            Assert.NotNull(parsed.Config);
            // Verify both are absolute paths
            Assert.True(Path.IsPathRooted(parsed.InputDirectory));
            Assert.True(Path.IsPathRooted(parsed.OutputDirectory));
            
            // Cleanup the timestamped directory
            _cleanupPaths.Add(parsed.OutputDirectory);
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
            // Since _testOutputDir already exists, a timestamped copy should be created
            Assert.NotEqual(_testOutputDir, parsed.InputDirectory);
            Assert.StartsWith(_testOutputDir, parsed.OutputDirectory);
            Assert.True(Directory.Exists(parsed.OutputDirectory));
            
            // Cleanup the timestamped directory
            _cleanupPaths.Add(parsed.OutputDirectory);
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
            // Since _testOutputDir already exists, a timestamped copy should be created
            Assert.NotEqual(_testOutputDir, parsed.OutputDirectory);
            Assert.StartsWith(_testOutputDir, parsed.OutputDirectory);
            Assert.True(Directory.Exists(parsed.OutputDirectory));
            
            // Cleanup the timestamped directory
            _cleanupPaths.Add(parsed.OutputDirectory);
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
    public void InterpretArguments_OutputDirectoryAlreadyExists_CreatesTimestampedCopy()
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
            // Should create a timestamped copy, not use the original
            Assert.NotEqual(_testOutputDir, parsed.OutputDirectory);
            Assert.StartsWith(_testOutputDir, parsed.OutputDirectory);
            // The new directory should have a timestamp suffix (format: yyyyMMddHHmmss)
            Assert.Matches(@"\d{14}$", parsed.OutputDirectory);
            Assert.True(Directory.Exists(parsed.OutputDirectory), "Timestamped directory should exist");
            
            // Cleanup the timestamped directory
            _cleanupPaths.Add(parsed.OutputDirectory);
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

    [Fact]
    public void InterpretArguments_OutputDirectoryExists_TimestampFormatIsCorrect()
    {
        // Arrange
        var existingDir = Path.Combine(Path.GetTempPath(), $"existing_{Guid.NewGuid()}");
        Directory.CreateDirectory(existingDir);
        _cleanupPaths.Add(existingDir);

        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = existingDir
        };

        // Act
        var beforeTime = DateTime.Now;
        var result = ArgumentInterpreting.InterpretArguments(lookup);
        var afterTime = DateTime.Now;

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            // Extract the timestamp part
            var timestampStr = parsed.OutputDirectory.Substring(existingDir.Length);
            Assert.Matches(@"^\d{14}$", timestampStr); // Format: yyyyMMddHHmmss
            
            // Verify the timestamp is within the expected time range
            var timestamp = DateTime.ParseExact(timestampStr, "yyyyMMddHHmmss", null);
            Assert.True(timestamp >= beforeTime.AddSeconds(-1) && timestamp <= afterTime.AddSeconds(1));
            
            _cleanupPaths.Add(parsed.OutputDirectory);
        });
    }

    [Fact]
    public void InterpretArguments_MultipleCallsWithSameExistingDir_CreatesDifferentTimestamps()
    {
        // Arrange
        var existingDir = Path.Combine(Path.GetTempPath(), $"multi_existing_{Guid.NewGuid()}");
        Directory.CreateDirectory(existingDir);
        _cleanupPaths.Add(existingDir);

        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = existingDir
        };

        // Act
        var result1 = ArgumentInterpreting.InterpretArguments(lookup);
        Thread.Sleep(1100); // Wait more than 1 second to ensure different timestamps
        var result2 = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result1.IsRight);
        Assert.True(result2.IsRight);
        
        string? dir1 = null;
        string? dir2 = null;
        
        result1.IfRight(parsed => dir1 = parsed.OutputDirectory);
        result2.IfRight(parsed => dir2 = parsed.OutputDirectory);

        Assert.NotNull(dir1);
        Assert.NotNull(dir2);
        Assert.NotEqual(dir1, dir2);
        Assert.True(Directory.Exists(dir1));
        Assert.True(Directory.Exists(dir2));
        
        _cleanupPaths.Add(dir1);
        _cleanupPaths.Add(dir2);
    }

    [Fact]
    public void InterpretArguments_NonExistentOutputDirectory_UsesExactPath()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"brand_new_{Guid.NewGuid()}");
        _cleanupPaths.Add(nonExistentDir);
        
        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = nonExistentDir
        };

        // Verify directory doesn't exist
        Assert.False(Directory.Exists(nonExistentDir));

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            // Should use the exact path, not create a timestamped version
            Assert.Equal(nonExistentDir, parsed.OutputDirectory);
            Assert.True(Directory.Exists(nonExistentDir));
            // Ensure no timestamp was added
            Assert.DoesNotMatch(@"\d{14}$", parsed.OutputDirectory);
        });
    }

    [Fact]
    public void InterpretArguments_ExistingDirWithTrailingSlash_HandlesCorrectly()
    {
        // Arrange
        var existingDir = Path.Combine(Path.GetTempPath(), $"trailing_slash_{Guid.NewGuid()}");
        Directory.CreateDirectory(existingDir);
        _cleanupPaths.Add(existingDir);

        var dirWithSlash = existingDir + Path.DirectorySeparatorChar;
        var lookup = new Dictionary<string, string>
        {
            ["in"] = _testInputDir,
            ["out"] = dirWithSlash
        };

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            // Should handle trailing slash and create timestamped directory
            Assert.StartsWith(existingDir, parsed.OutputDirectory);
            Assert.True(Directory.Exists(parsed.OutputDirectory));
            
            _cleanupPaths.Add(parsed.OutputDirectory);
        });
    }
}



