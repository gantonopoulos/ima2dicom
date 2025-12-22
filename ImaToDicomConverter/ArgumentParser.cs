using System.Text.Json;
using ImaToDicomConverter.Errors;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using ArgumentLookUp = System.Collections.Generic.Dictionary<string, string>;

namespace ImaToDicomConverter;

internal record ParsedArguments(
    string InputDirectory,
    string OutputDirectory,
    ConverterConfiguration Config);

internal static class ArgumentParser
{
    public static Either<Error,ParsedArguments> Parse(string[] args)
    {
        return CollectArguments(args)
            .Bind(InterpretArguments);
    }

    private static Either<Error, ArgumentLookUp> CollectArguments(string[] argumentList)
    {
        return argumentList
            .Map(CollectSingleArgument).Sequence()
            .Map(lookUp => lookUp.ToDictionary());
    }

    private static Either<Error, (string, string)> CollectSingleArgument(string argumentPair)
    {
        var parts = argumentPair.Split("=", 2);
        if (parts.Length == 2 && parts[0].StartsWith("--"))
        {
            return (parts[0].TrimStart('-'), parts[1]);
        }

        return new ArgumentError($"Invalid argument format: {argumentPair}");
    }

    private static Either<Error, ParsedArguments> InterpretArguments(ArgumentLookUp lookup)
    {
        var mandatoryParsers = new List<Func<ParsedArguments, Either<Error, ParsedArguments>>>
        {
            knownArguments => ParseSourceDirectory(lookup).Map(dir => knownArguments with { InputDirectory = dir }),
            knownArguments => ParseDestinationDirectory(lookup).Map(dir => knownArguments with { OutputDirectory = dir }),
            knownArguments => ParseConfig(lookup).Map(config => knownArguments with { Config = config })
        };
        
        var initial = new ParsedArguments(string.Empty, string.Empty, new ConverterConfiguration());
        return mandatoryParsers.Fold(
            Right<Error, ParsedArguments>(initial),
            (aggregatedArgument, parser) => aggregatedArgument.Bind(parser));
    }

    private static Either<Error, string> ParseSourceDirectory(ArgumentLookUp lookup)
    {
        return lookup.TryGetValue("in", out var value)
            ? ValidateDirectory(value)
            : Left<Error, string>(new ArgumentError("Missing required argument: --in"));
    }

    private static Either<Error, string> ParseDestinationDirectory(ArgumentLookUp lookup)
    {
        return lookup.TryGetValue("out", out var value)
            ? ValidateDirectory(value)
            : Left<Error, string>(new ArgumentError("Missing required argument: --out"));
    }
    
    private static Either<Error, string> ValidateDirectory(string path)
    {
        return Directory.Exists(path)
            ? Right<Error, string>(path)
            : Left<Error, string>(new ArgumentError($"The directory does not exist: {path}"));
    }

    private static Either<Error, ConverterConfiguration> ParseConfig(ArgumentLookUp lookup)
    {
        var argName = Argument.Config.AsString();
        return lookup.TryGetValue(argName, out var configPath)
            ? File.Exists(configPath)
                ? Try(() => LoadConfig(configPath))
                    .Match(
                        Right<Error, ConverterConfiguration>,
                        ex => Left<Error, ConverterConfiguration>(
                            new ArgumentError($"Failed to load config: {ex.Message}"))
                    )
                : Left<Error, ConverterConfiguration>(
                    new ArgumentError($"The config file does not exist: {configPath}"))
            : Left<Error, ConverterConfiguration>(new ArgumentError($"Missing required argument: {Argument.Config.AsCliString()}"));
    }

    private static ConverterConfiguration LoadConfig(string configPath)
    {
        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<ConverterConfiguration>(json)
               ?? throw new Exception("Failed to deserialize config.");
    }
}