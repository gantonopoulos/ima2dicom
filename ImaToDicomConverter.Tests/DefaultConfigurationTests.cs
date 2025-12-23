using System.Reflection;
using System.Text.Json;
using Xunit;

namespace ImaToDicomConverter.Tests;

/// <summary>
/// Tests for default configuration loading functionality.
/// The default configuration should be loaded from an embedded resource when no config file is specified.
/// </summary>
public class DefaultConfigurationTests
{
    [Fact]
    public void GenerateDefaultConfig_ShouldCreateValidJsonFile()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"default_config_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            // Act
            var result = ConfigGenerator.GenerateDefaultConfig(testDir);

            // Assert
            Assert.True(result.IsRight);
            result.IfRight(path =>
            {
                Assert.True(File.Exists(path));
                var json = File.ReadAllText(path);
                Assert.NotEmpty(json);

                // Verify it's valid JSON (with comments allowed)
                var exception = Record.Exception(() =>
                {
                    var options = new JsonDocumentOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };
                    JsonDocument.Parse(json, options);
                });
                Assert.Null(exception);
            });
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }

    [Fact]
    public void EmbeddedDefaultConfigResource_ShouldBeAccessible()
    {
        // Arrange
        var assembly = typeof(ConfigGenerator).Assembly;
        var resourceName = "ImaToDicomConverter.default-config.json";

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);

        // Assert
        Assert.NotNull(stream);
    }

    [Fact]
    public void EmbeddedDefaultConfigResource_ShouldContainValidJson()
    {
        // Arrange
        var assembly = typeof(ConfigGenerator).Assembly;
        var resourceName = "ImaToDicomConverter.default-config.json";

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        // Assert
        Assert.NotEmpty(json);
        var exception = Record.Exception(() =>
        {
            var options = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            JsonDocument.Parse(json, options);
        });
        Assert.Null(exception);
    }

    [Fact]
    public void InterpretArguments_MissingConfigParameter_UsesDefaultConfig()
    {
        // Arrange
        var lookup = new Dictionary<string, string>();

        // Act
        var result = ArgumentInterpreting.InterpretArguments(lookup);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.NotNull(parsed.Config);
            // The config should be loaded from default embedded resource
        });
    }

    [Fact]
    public void DefaultConfiguration_ShouldDeserializeToConverterConfiguration()
    {
        // Arrange
        var assembly = typeof(ConfigGenerator).Assembly;
        var resourceName = "ImaToDicomConverter.default-config.json";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        // Act
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new OptionJsonConverterFactory() }
        };

        var exception = Record.Exception(() =>
        {
            var config = JsonSerializer.Deserialize<ConverterConfiguration>(json, options);
            Assert.NotNull(config);
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void GeneratedDefaultConfig_WhenLoaded_ShouldHaveExpectedStructure()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"default_config_structure_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            var result = ConfigGenerator.GenerateDefaultConfig(testDir);
            Assert.True(result.IsRight);

            string? configPath = null;
            result.IfRight(path => configPath = path);
            Assert.NotNull(configPath);

            // Act
            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters = { new OptionJsonConverterFactory() }
            };
            var config = JsonSerializer.Deserialize<ConverterConfiguration>(json, options);

            // Assert
            Assert.NotNull(config);
            // Add specific assertions based on what your default config should contain
            // For example:
            // Assert.True(config.Rows.IsSome);
            // Assert.True(config.Columns.IsSome);
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}

