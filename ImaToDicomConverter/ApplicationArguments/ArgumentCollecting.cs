using ImaToDicomConverter.DicomConversion;
using ImaToDicomConverter.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ImaToDicomConverter.ApplicationArguments;

internal record ParsedArguments(
    string InputDirectory,
    string OutputDirectory,
    ConvertionParameters Config);

internal static class ArgumentCollecting
{
    public static Either<Error, ArgumentLookUp> CollectArguments(string[] argumentList)
    {
        return argumentList
            .Map(CollectSingleArgument).Sequence()
            .Map(lookUp => lookUp.ToDictionary());
    }

    private static Either<Error, (string, string)> CollectSingleArgument(string argumentPair)
    {
        var parts = argumentPair.Split("=", 2);
        return parts.Length switch
        {
            2 when parts[0].StartsWith("--") => (parts[0].TrimStart('-'), parts[1]),
            1 when parts[0].StartsWith("--") => (parts[0].TrimStart('-'), string.Empty),
            _ => new ArgumentError($"Invalid argument format: {argumentPair}")
        };
    }
}