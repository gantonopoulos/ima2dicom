using Xunit;

namespace ImaToDicomConverter.Tests;

/// <summary>
/// Tests for the --genconf argument functionality.
/// When present, the application should generate a default configuration file and terminate.
/// </summary>
public class GenConfArgumentTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly List<string> _cleanupPaths = new();

    public GenConfArgumentTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"genconf_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _cleanupPaths.Add(_testDirectory);
    }

    public void Dispose()
    {
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
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CollectArguments_GenConfArgument_CollectsSuccessfully()
    {
        // Arrange
        var args = new[] { "--genconf" };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(lookup =>
        {
            Assert.True(lookup.ContainsKey("genconf"));
        });
    }

    [Fact]
    public void CollectArguments_GenConfWithOutputDir_CollectsValue()
    {
        // Arrange
        var args = new[] { $"--genconf={_testDirectory}" };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(lookup =>
        {
            Assert.True(lookup.ContainsKey("genconf"));
            Assert.Equal(_testDirectory, lookup["genconf"]);
        });
    }

    [Fact]
    public void GenerateDefaultConfig_NoOutputDir_CreatesInCurrentDirectory()
    {
        // Arrange
        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        try
        {
            // Act
            var result = ConfigGenerator.GenerateDefaultConfig();

            // Assert
            Assert.True(result.IsRight);
            result.IfRight(generatedPath =>
            {
                Assert.True(File.Exists(generatedPath));
                Assert.Equal(Path.Combine(_testDirectory, ConfigGenerator.DefaultConfigJson), generatedPath);
                _cleanupPaths.Add(generatedPath);
            });
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Fact]
    public void GenerateDefaultConfig_WithOutputDir_CreatesInSpecifiedDirectory()
    {
        // Arrange & Act
        var result = ConfigGenerator.GenerateDefaultConfig(_testDirectory);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(generatedPath =>
        {
            Assert.True(File.Exists(generatedPath));
            Assert.Contains(_testDirectory, generatedPath);
            Assert.EndsWith(ConfigGenerator.DefaultConfigJson, generatedPath);
        });
    }

    [Fact]
    public void GenerateDefaultConfig_FileAlreadyExists_ReturnsError()
    {
        // Arrange
        var existingFile = Path.Combine(_testDirectory, ConfigGenerator.DefaultConfigJson);
        File.WriteAllText(existingFile, "existing content");
        _cleanupPaths.Add(existingFile);

        // Act
        var result = ConfigGenerator.GenerateDefaultConfig(_testDirectory);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.Contains("already exists", error.Message);
        });
    }

    [Fact]
    public void GenerateDefaultConfig_CreatesValidJsonFile()
    {
        // Arrange & Act
        var result = ConfigGenerator.GenerateDefaultConfig(_testDirectory);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(generatedPath =>
        {
            var json = File.ReadAllText(generatedPath);
            Assert.NotEmpty(json);

            // Verify it's valid JSON by trying to parse it (with comments allowed)
            var exception = Record.Exception(() =>
            {
                var options = new System.Text.Json.JsonDocumentOptions
                {
                    CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                System.Text.Json.JsonDocument.Parse(json, options);
            });
            Assert.Null(exception);
        });
    }

    [Fact]
    public void ArgumentNameEnum_GenerateConfig_ConvertsToCorrectString()
    {
        // Arrange & Act
        var genconfString = Argument.GenerateConfig.AsString();
        var genconfCliString = Argument.GenerateConfig.AsCliString();

        // Assert
        Assert.Equal("genconf", genconfString);
        Assert.Equal("--genconf", genconfCliString);
    }

    [Fact]
    public void CollectArguments_GenConfWithOtherArguments_CollectsAll()
    {
        // Arrange
        var args = new[] { "--in=/some/path", "--genconf", "--out=/other/path" };

        // Act
        var result = ArgumentCollecting.CollectArguments(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(lookup =>
        {
            Assert.True(lookup.ContainsKey("genconf"));
            Assert.True(lookup.ContainsKey("in"));
            Assert.True(lookup.ContainsKey("out"));
        });
    }
}

