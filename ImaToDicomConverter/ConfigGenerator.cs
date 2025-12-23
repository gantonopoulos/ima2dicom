using System.Reflection;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace ImaToDicomConverter;

/// <summary>
/// Utility for generating default configuration files from embedded resources.
/// </summary>
internal static class ConfigGenerator
{
    private const string EmbeddedResourceName = "ImaToDicomConverter.default-config.json";
    
    /// <summary>
    /// Generates a default configuration file in the specified directory.
    /// </summary>
    /// <param name="outputDirectory">Directory where the config file should be created. Defaults to current directory.</param>
    /// <returns>Either the full path to the generated file, or an Error.</returns>
    public static Either<Error, string> GenerateDefaultConfig(string? outputDirectory = null)
    {
        try
        {
            var targetDir = outputDirectory ?? Directory.GetCurrentDirectory();
            var targetPath = Path.Combine(targetDir, "default-config.json");
            
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
}

