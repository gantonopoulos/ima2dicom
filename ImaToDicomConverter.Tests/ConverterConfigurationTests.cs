using System.Text.Json;
using ImaToDicomConverter.Errors;
using LanguageExt;
using Xunit;

namespace ImaToDicomConverter.Tests;

public class ConverterConfigurationTests
{
    private const string TestConfigDirectory = "TestConfigs";

    public ConverterConfigurationTests()
    {
        // Ensure test config directory exists
        if (!Directory.Exists(TestConfigDirectory))
        {
            Directory.CreateDirectory(TestConfigDirectory);
        }
    }

    private static ConverterConfiguration DeserializeConfig(string json)
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new OptionJsonConverterFactory() }
        };
        return JsonSerializer.Deserialize<ConverterConfiguration>(json, options)
               ?? throw new Exception("Failed to deserialize config.");
    }

    [Fact]
    public void Deserialize_ValidCompleteConfiguration_ShouldSucceed()
    {
        // Arrange
        var json = """
        {
            "Modality": "CT",
            "Rows": 512,
            "Columns": 512,
            "SamplesPerPixel": 1,
            "PhotometricInterpretation": "MONOCHROME2",
            "BitsAllocated": 16,
            "BitsStored": 16,
            "HighBit": 15,
            "PixelRepresentation": 1,
            "RescaleSlope": 1.0,
            "RescaleIntercept": -1024.0,
            "WindowCenter": 2000.0,
            "WindowWidth": 4000.0,
            "SliceThickness": 3.0,
            "SpacingBetweenSlices": 0.01,
            "PixelSpacing": "0.48828125,0.48828125"
        }
        """;

        // Act
        var config = DeserializeConfig(json);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Modality.IsSome);
        config.Modality.IfSome(v => Assert.Equal("CT", v));
        
        Assert.True(config.Rows.IsSome);
        config.Rows.IfSome(v => Assert.Equal((ushort)512, v));
        
        Assert.True(config.Columns.IsSome);
        config.Columns.IfSome(v => Assert.Equal((ushort)512, v));
        
        Assert.True(config.SamplesPerPixel.IsSome);
        config.SamplesPerPixel.IfSome(v => Assert.Equal((ushort)1, v));
        
        Assert.True(config.PhotometricInterpretation.IsSome);
        config.PhotometricInterpretation.IfSome(v => Assert.Equal("MONOCHROME2", v));
        
        Assert.True(config.BitsAllocated.IsSome);
        config.BitsAllocated.IfSome(v => Assert.Equal((ushort)16, v));
        
        Assert.True(config.BitsStored.IsSome);
        config.BitsStored.IfSome(v => Assert.Equal((ushort)16, v));
        
        Assert.True(config.HighBit.IsSome);
        config.HighBit.IfSome(v => Assert.Equal((ushort)15, v));
        
        Assert.True(config.PixelRepresentation.IsSome);
        config.PixelRepresentation.IfSome(v => Assert.Equal((ushort)1, v));
        
        Assert.True(config.RescaleSlope.IsSome);
        config.RescaleSlope.IfSome(v => Assert.Equal(1.0, v));
        
        Assert.True(config.RescaleIntercept.IsSome);
        config.RescaleIntercept.IfSome(v => Assert.Equal(-1024.0, v));
        
        Assert.True(config.WindowCenter.IsSome);
        config.WindowCenter.IfSome(v => Assert.Equal(2000.0, v));
        
        Assert.True(config.WindowWidth.IsSome);
        config.WindowWidth.IfSome(v => Assert.Equal(4000.0, v));
        
        Assert.True(config.SliceThickness.IsSome);
        config.SliceThickness.IfSome(v => Assert.Equal(3.0, v));
        
        Assert.True(config.SpacingBetweenSlices.IsSome);
        config.SpacingBetweenSlices.IfSome(v => Assert.Equal(0.01, v));
        
        Assert.True(config.PixelSpacing.IsSome);
        config.PixelSpacing.IfSome(v => Assert.Equal("0.48828125,0.48828125", v));
    }

    [Fact]
    public void Deserialize_PartialConfiguration_ShouldLeaveUnsetFieldsNone()
    {
        // Arrange
        var json = """
        {
            "Modality": "CT",
            "Rows": 512,
            "Columns": 512
        }
        """;

        // Act
        var config = DeserializeConfig(json);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Modality.IsSome);
        Assert.True(config.Rows.IsSome);
        Assert.True(config.Columns.IsSome);
        Assert.True(config.SamplesPerPixel.IsNone);
        Assert.True(config.PhotometricInterpretation.IsNone);
        Assert.True(config.BitsAllocated.IsNone);
        Assert.True(config.RescaleSlope.IsNone);
    }

    [Fact]
    public void Deserialize_EmptyConfiguration_ShouldReturnAllNoneFields()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = DeserializeConfig(json);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Modality.IsNone);
        Assert.True(config.Rows.IsNone);
        Assert.True(config.Columns.IsNone);
        Assert.True(config.SamplesPerPixel.IsNone);
        Assert.True(config.PhotometricInterpretation.IsNone);
    }

    [Fact]
    public void Deserialize_ExplicitNullValues_ShouldSetFieldsToNone()
    {
        // Arrange
        var json = """
        {
            "Modality": null,
            "Rows": null,
            "Columns": 512
        }
        """;

        // Act
        var config = DeserializeConfig(json);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Modality.IsNone);
        Assert.True(config.Rows.IsNone);
        Assert.True(config.Columns.IsSome);
        config.Columns.IfSome(v => Assert.Equal((ushort)512, v));
    }

    [Fact]
    public void Deserialize_InvalidJson_ShouldThrowException()
    {
        // Arrange
        var invalidJson = "{ this is not valid json }";

        // Act & Assert
        Assert.Throws<JsonException>(() => DeserializeConfig(invalidJson));
    }

    [Fact]
    public void Deserialize_InvalidTypeConversion_ShouldThrowException()
    {
        // Arrange - Rows should be ushort, not a string
        var json = """
        {
            "Rows": "not a number"
        }
        """;

        // Act & Assert
        Assert.Throws<JsonException>(() => DeserializeConfig(json));
    }

    [Fact]
    public void Deserialize_NegativeValueForUshort_ShouldThrowException()
    {
        // Arrange - ushort cannot be negative
        var json = """
        {
            "Rows": -512
        }
        """;

        // Act & Assert
        Assert.Throws<JsonException>(() => DeserializeConfig(json));
    }

    [Fact]
    public void Deserialize_ValueTooLargeForUshort_ShouldThrowException()
    {
        // Arrange - ushort max value is 65535
        var json = """
        {
            "Rows": 70000
        }
        """;

        // Act & Assert
        Assert.Throws<JsonException>(() => DeserializeConfig(json));
    }

    [Fact]
    public void Deserialize_ValidDoubleValues_ShouldSucceed()
    {
        // Arrange
        var json = """
        {
            "RescaleSlope": 1.5,
            "RescaleIntercept": -1024.0,
            "WindowCenter": 2000.0,
            "WindowWidth": 4000.0,
            "SliceThickness": 3.0,
            "SpacingBetweenSlices": 0.01
        }
        """;

        // Act
        var config = DeserializeConfig(json);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.RescaleSlope.IsSome);
        config.RescaleSlope.IfSome(v => Assert.Equal(1.5, v));
        
        Assert.True(config.RescaleIntercept.IsSome);
        config.RescaleIntercept.IfSome(v => Assert.Equal(-1024.0, v));
        
        Assert.True(config.WindowCenter.IsSome);
        config.WindowCenter.IfSome(v => Assert.Equal(2000.0, v));
        
        Assert.True(config.WindowWidth.IsSome);
        config.WindowWidth.IfSome(v => Assert.Equal(4000.0, v));
        
        Assert.True(config.SliceThickness.IsSome);
        config.SliceThickness.IfSome(v => Assert.Equal(3.0, v));
        
        Assert.True(config.SpacingBetweenSlices.IsSome);
        config.SpacingBetweenSlices.IfSome(v => Assert.Equal(0.01, v));
    }

    [Fact]
    public void Deserialize_InvalidDoubleValue_ShouldThrowException()
    {
        // Arrange
        var json = """
        {
            "RescaleSlope": "not a double"
        }
        """;

        // Act & Assert
        Assert.Throws<JsonException>(() => DeserializeConfig(json));
    }

    [Fact]
    public void Deserialize_JsonWithComments_ShouldSucceed()
    {
        // Arrange
        var json = """
        {
            // This is a single-line comment
            "Modality": "CT",
            /* This is a 
               multi-line comment */
            "Rows": 512,
            // Another comment
            "Columns": 512
        }
        """;

        // Act
        var config = DeserializeConfig(json);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Modality.IsSome);
        config.Modality.IfSome(v => Assert.Equal("CT", v));
        Assert.True(config.Rows.IsSome);
        config.Rows.IfSome(v => Assert.Equal((ushort)512, v));
        Assert.True(config.Columns.IsSome);
        config.Columns.IfSome(v => Assert.Equal((ushort)512, v));
    }

    [Fact]
    public void Deserialize_JsonWithTrailingCommas_ShouldSucceed()
    {
        // Arrange
        var json = """
        {
            "Modality": "CT",
            "Rows": 512,
        }
        """;

        // Act
        var config = DeserializeConfig(json);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Modality.IsSome);
        Assert.True(config.Rows.IsSome);
    }

    [Fact]
    public void ParseArguments_ValidConfigFile_ShouldLoadSuccessfully()
    {
        // Arrange
        var configPath = Path.Combine(TestConfigDirectory, "valid-config.json");
        var json = """
        {
            "Modality": "CT",
            "Rows": 512,
            "Columns": 512,
            "SamplesPerPixel": 1,
            "PhotometricInterpretation": "MONOCHROME2",
            "BitsAllocated": 16,
            "BitsStored": 16,
            "HighBit": 15,
            "PixelRepresentation": 1,
            "RescaleSlope": 1.0,
            "RescaleIntercept": -1024.0,
            "WindowCenter": 2000.0,
            "WindowWidth": 4000.0,
            "SliceThickness": 3.0,
            "SpacingBetweenSlices": 0.01,
            "PixelSpacing": "0.48828125,0.48828125"
        }
        """;
        File.WriteAllText(configPath, json);

        // Create input and output directories for the test
        var inputDir = Path.Combine(TestConfigDirectory, "input");
        var outputDir = Path.Combine(TestConfigDirectory, "output");
        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);

        var args = new[]
        {
            $"--in={inputDir}",
            $"--out={outputDir}",
            $"--config={configPath}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.True(parsed.Config.Modality.IsSome);
            parsed.Config.Modality.IfSome(v => Assert.Equal("CT", v));
            
            Assert.True(parsed.Config.Rows.IsSome);
            parsed.Config.Rows.IfSome(v => Assert.Equal((ushort)512, v));
            
            Assert.True(parsed.Config.Columns.IsSome);
            parsed.Config.Columns.IfSome(v => Assert.Equal((ushort)512, v));
        });

        // Cleanup
        File.Delete(configPath);
        Directory.Delete(inputDir);
        Directory.Delete(outputDir);
    }

    [Fact]
    public void ParseArguments_MissingConfigFile_ShouldReturnError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(TestConfigDirectory, "does-not-exist.json");
        
        // Create input and output directories for the test
        var inputDir = Path.Combine(TestConfigDirectory, "input");
        var outputDir = Path.Combine(TestConfigDirectory, "output");
        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);

        var args = new[]
        {
            $"--in={inputDir}",
            $"--out={outputDir}",
            $"--config={nonExistentPath}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.IsType<ArgumentError>(error);
            Assert.Contains("does not exist", error.Message);
        });

        // Cleanup
        Directory.Delete(inputDir);
        Directory.Delete(outputDir);
    }

    [Fact]
    public void ParseArguments_InvalidJsonInConfigFile_ShouldReturnError()
    {
        // Arrange
        var configPath = Path.Combine(TestConfigDirectory, "invalid-config.json");
        File.WriteAllText(configPath, "{ this is not valid json }");

        // Create input and output directories for the test
        var inputDir = Path.Combine(TestConfigDirectory, "input");
        var outputDir = Path.Combine(TestConfigDirectory, "output");
        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);

        var args = new[]
        {
            $"--in={inputDir}",
            $"--out={outputDir}",
            $"--config={configPath}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.IsType<ArgumentError>(error);
            Assert.Contains("Failed to load config", error.Message);
        });

        // Cleanup
        File.Delete(configPath);
        Directory.Delete(inputDir);
        Directory.Delete(outputDir);
    }

    [Fact]
    public void ParseArguments_ConfigFileWithTypeError_ShouldReturnError()
    {
        // Arrange
        var configPath = Path.Combine(TestConfigDirectory, "type-error-config.json");
        var json = """
        {
            "Rows": "not a number",
            "Columns": 512
        }
        """;
        File.WriteAllText(configPath, json);

        // Create input and output directories for the test
        var inputDir = Path.Combine(TestConfigDirectory, "input");
        var outputDir = Path.Combine(TestConfigDirectory, "output");
        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);

        var args = new[]
        {
            $"--in={inputDir}",
            $"--out={outputDir}",
            $"--config={configPath}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        Assert.True(result.IsLeft);
        result.IfLeft(error =>
        {
            Assert.IsType<ArgumentError>(error);
            Assert.Contains("Failed to load config", error.Message);
        });

        // Cleanup
        File.Delete(configPath);
        Directory.Delete(inputDir);
        Directory.Delete(outputDir);
    }

    [Fact]
    public void ParseArguments_PartialConfigFile_ShouldLoadWithNoneValues()
    {
        // Arrange
        var configPath = Path.Combine(TestConfigDirectory, "partial-config.json");
        var json = """
        {
            "Modality": "CT",
            "Rows": 256
        }
        """;
        File.WriteAllText(configPath, json);

        // Create input and output directories for the test
        var inputDir = Path.Combine(TestConfigDirectory, "input");
        var outputDir = Path.Combine(TestConfigDirectory, "output");
        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);

        var args = new[]
        {
            $"--in={inputDir}",
            $"--out={outputDir}",
            $"--config={configPath}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.True(parsed.Config.Modality.IsSome);
            parsed.Config.Modality.IfSome(v => Assert.Equal("CT", v));
            
            Assert.True(parsed.Config.Rows.IsSome);
            parsed.Config.Rows.IfSome(v => Assert.Equal((ushort)256, v));
            
            Assert.True(parsed.Config.Columns.IsNone);
            Assert.True(parsed.Config.SamplesPerPixel.IsNone);
            Assert.True(parsed.Config.PhotometricInterpretation.IsNone);
        });

        // Cleanup
        File.Delete(configPath);
        Directory.Delete(inputDir);
        Directory.Delete(outputDir);
    }

    [Fact]
    public void ParseArguments_ConfigFileWithExplicitNulls_ShouldHandleCorrectly()
    {
        // Arrange
        var configPath = Path.Combine(TestConfigDirectory, "nulls-config.json");
        var json = """
        {
            "Modality": null,
            "Rows": 512,
            "Columns": null
        }
        """;
        File.WriteAllText(configPath, json);

        // Create input and output directories for the test
        var inputDir = Path.Combine(TestConfigDirectory, "input");
        var outputDir = Path.Combine(TestConfigDirectory, "output");
        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);

        var args = new[]
        {
            $"--in={inputDir}",
            $"--out={outputDir}",
            $"--config={configPath}"
        };

        // Act
        var result = ArgumentParser.Parse(args);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(parsed =>
        {
            Assert.True(parsed.Config.Modality.IsNone);
            
            Assert.True(parsed.Config.Rows.IsSome);
            parsed.Config.Rows.IfSome(v => Assert.Equal((ushort)512, v));
            
            Assert.True(parsed.Config.Columns.IsNone);
        });

        // Cleanup
        File.Delete(configPath);
        Directory.Delete(inputDir);
        Directory.Delete(outputDir);
    }
}

