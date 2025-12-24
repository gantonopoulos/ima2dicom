using ImaToDicomConverter.ApplicationArguments;
using ImaToDicomConverter.DicomConversion;
using Xunit;

namespace ImaToDicomConverter.Tests;

/// <summary>
/// Tests for ConfigurationGenerator that may change the current directory.
/// Uses a collection to prevent parallel execution with other tests.
/// </summary>
[Collection("Sequential")]
public class ConfigurationGeneratorTests
{
    private const string TestConfigDirectory = "TestGenConf";

    public ConfigurationGeneratorTests()
    {
        // Ensure test directory exists and is clean
        if (Directory.Exists(TestConfigDirectory))
        {
            Directory.Delete(TestConfigDirectory, true);
        }
        Directory.CreateDirectory(TestConfigDirectory);
    }

    [Fact]
    public void GenerateDefaultConfig_ToCurrentDirectory_ShouldSucceed()
    {
        // Arrange
        var originalDir = Directory.GetCurrentDirectory();
        var testDir = Path.GetFullPath(TestConfigDirectory);
        
        try
        {
            Directory.SetCurrentDirectory(testDir);

            // Act
            var result = ConfigurationGenerator.GenerateDefaultConfig();

            // Assert
            Assert.True(result.IsRight);
            result.IfRight(path =>
            {
                Assert.True(File.Exists(path));
                Assert.Equal(Path.Combine(testDir, "default-config.json"), path);
                
                // Verify it's valid JSON with expected content
                var content = File.ReadAllText(path);
                Assert.Contains("Modality", content);
                Assert.Contains("Rows", content);
                Assert.Contains("Columns", content);
            });
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Fact]
    public void GenerateDefaultConfig_ToSpecifiedDirectory_ShouldSucceed()
    {
        // Arrange
        var outputDir = Path.Combine(TestConfigDirectory, "output");
        Directory.CreateDirectory(outputDir);

        // Act
        var result = ConfigurationGenerator.GenerateDefaultConfig(outputDir);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(path =>
        {
            Assert.True(File.Exists(path));
            Assert.Equal(Path.Combine(outputDir, "default-config.json"), path);
            
            // Verify content
            var content = File.ReadAllText(path);
            Assert.Contains("Modality", content);
        });
    }

    [Fact]
    public void GenerateDefaultConfig_FileAlreadyExists_ShouldReturnError()
    {
        // Arrange
        var outputDir = Path.Combine(TestConfigDirectory, "existing");
        Directory.CreateDirectory(outputDir);
        var existingFile = Path.Combine(outputDir, "default-config.json");
        File.WriteAllText(existingFile, "existing content");

        // Act
        var result = ConfigurationGenerator.GenerateDefaultConfig(outputDir);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.Contains("already exists", error.Message);
        });
    }

    [Fact]
    public void GenerateDefaultConfig_InvalidDirectory_ShouldReturnError()
    {
        // Arrange
        var invalidDir = Path.Combine(TestConfigDirectory, "nonexistent", "deeply", "nested");

        // Act
        var result = ConfigurationGenerator.GenerateDefaultConfig(invalidDir);

        // Assert
        Assert.True(result.IsLeft);
    }

    [Fact]
    public void GeneratedConfig_ShouldBeValidJson()
    {
        // Arrange
        var outputDir = Path.Combine(TestConfigDirectory, "valid");
        Directory.CreateDirectory(outputDir);

        // Act
        var result = ConfigurationGenerator.GenerateDefaultConfig(outputDir);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(path =>
        {
            var content = File.ReadAllText(path);
            
            // Try to parse it as JSON (this will throw if invalid)
            var options = new System.Text.Json.JsonSerializerOptions
            {
                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters = { new OptionJsonConverterFactory() }
            };
            
            var config = System.Text.Json.JsonSerializer.Deserialize<ConvertionParameters>(content, options);
            Assert.NotNull(config);
            
            // Verify expected fields are present
            Assert.True(config.Modality.IsSome);
            Assert.True(config.Rows.IsSome);
            Assert.True(config.Columns.IsSome);
        });
    }
}

