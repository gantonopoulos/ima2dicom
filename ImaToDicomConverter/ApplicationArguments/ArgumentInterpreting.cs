using System.Text.Json;
using ImaToDicomConverter.DicomConversion;
using ImaToDicomConverter.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ImaToDicomConverter.ApplicationArguments;

internal static class ArgumentInterpreting
{
    public static Either<Error, ParsedArguments> InterpretArguments(ArgumentLookUp lookup)
    {
        var mandatoryParsers = new List<Func<ParsedArguments, Either<Error, ParsedArguments>>>
        {
            knownArguments => ValidateDirectory(ParseSourceDirectory(lookup))
                .Map(dir => knownArguments with { InputDirectory = dir }),
            knownArguments => ValidateDirectory(ParseDestinationDirectory(lookup))
                .Map(dir => knownArguments with { OutputDirectory = dir }),
            knownArguments => ParseConfig(lookup).Map(config => knownArguments with { Config = config })
        };
        
        var initial = new ParsedArguments(string.Empty, string.Empty, new ConvertionParameters());
        return mandatoryParsers.Fold(Prelude.Right<Error, ParsedArguments>(initial),
            (aggregatedArgument, parser) => aggregatedArgument.Bind(parser));
    }

    private static string ParseSourceDirectory(ArgumentLookUp lookup)
    {
        return lookup.TryGetValue(Argument.In.AsString(), out var value) ? value : Directory.GetCurrentDirectory();
    }

    private static string ParseDestinationDirectory(ArgumentLookUp lookup)
    {
        return lookup.TryGetValue(Argument.Out.AsString(), out var value) ? value : Directory.GetCurrentDirectory();
    }

    private static Either<Error, ConvertionParameters> ParseConfig(ArgumentLookUp lookup)
    {
        var argName = Argument.Config.AsString();
        return lookup.TryGetValue(argName, out var configPath)
            ? File.Exists(configPath)
                ? Prelude.Try(() => LoadConfig(configPath))
                    .Match(Prelude.Right<Error, ConvertionParameters>,
                        ex => Prelude.Left<Error, ConvertionParameters>(
                            new ArgumentError($"Failed to load config: {ex.Message}"))
                    )
                : Prelude.Left<Error, ConvertionParameters>(
                    new ArgumentError($"The config file does not exist: {configPath}"))
            : ConfigurationGenerator.LoadDefaultConfigToTempFile().Map(LoadConfig);
    }

    private static ConvertionParameters LoadConfig(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new OptionJsonConverterFactory() }
        };
        return JsonSerializer.Deserialize<ConvertionParameters>(json, options)
               ?? throw new Exception("Failed to deserialize config.");
    }
    
    private static Either<Error, string> ValidateDirectory(string path)
    {
        return Directory.Exists(path)
            ? Prelude.Right<Error, string>(path)
            : Prelude.Left<Error, string>(new ArgumentError($"The directory does not exist: {path}"));
    }
    
}