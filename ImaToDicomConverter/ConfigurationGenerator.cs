using System.Reflection;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace ImaToDicomConverter;

/// <summary>
/// Utility for generating default configuration files from embedded resources.
/// </summary>
internal static class ConfigurationGenerator
{
    private const string EmbeddedResourceName = "ImaToDicomConverter.default-config.json";
    
    public const string DefaultConfigJson = "default-config.json";

    /// <summary>
    /// Generates a default configuration file in the specified directory.
    /// </summary>
    /// <param name="outputDirectory">Directory where the config file should be created. Defaults to current directory.</param>
    /// <returns>Either the full path to the generated file, or an Error.</returns>
    public static Either<Error, string> GenerateDefaultConfig(string? outputDirectory = null)
    {
        try
        {
            var targetDir = string.IsNullOrEmpty(outputDirectory) ? Directory.GetCurrentDirectory(): outputDirectory;
            var targetPath = Path.Combine(targetDir, DefaultConfigJson);
            
            // Check if file already exists
            if (File.Exists(targetPath))
            {
                return Left<Error, string>(
                    new Exception($"Configuration file already exists: {targetPath}"));
            }
            
            // Read embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
            
            if (stream == null)
            {
                return Left<Error, string>(
                    new Exception($"Failed to load embedded resource: {EmbeddedResourceName}"));
            }
            
            // Write to target file
            using var fileStream = File.Create(targetPath);
            stream.CopyTo(fileStream);
            
            return Right<Error, string>(targetPath);
        }
        catch (Exception ex)
        {
            return Left<Error, string>(ex);
        }
    }

    /// <summary>
    /// Loads the default configuration directly from the embedded resource without generating a file.
    /// </summary>
    /// <returns>The path to a temporary file containing the default configuration, or an Error.</returns>
    public static Either<Error, string> LoadDefaultConfigToTempFile()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
            
            if (stream == null)
            {
                return Left<Error, string>(
                    new Exception($"Failed to load embedded resource: {EmbeddedResourceName}"));
            }

            // Create a temporary file
            var tempPath = Path.Combine(Path.GetTempPath(), $"ima2dicom-config-{Guid.NewGuid()}.json");
            using var fileStream = File.Create(tempPath);
            stream.CopyTo(fileStream);
            
            return Right<Error, string>(tempPath);
        }
        catch (Exception ex)
        {
            return Left<Error, string>(ex);
        }
    }
}

