namespace ImaToDicomConverter;

/// <summary>
/// Enumeration of known command-line argument names.
/// Provides a single source of truth for argument names to minimize string dependencies.
/// </summary>
internal enum Argument
{
    In,
    Out,
    Config,
    GenerateConfig,
    Help
}

/// <summary>
/// Extension methods for ArgumentName enum to convert to/from strings.
/// </summary>
internal static class ArgumentNameExtensions
{
    extension(Argument argumentName)
    {
        /// <summary>
        /// Converts an ArgumentName to its string representation (without the -- prefix).
        /// </summary>
        public string AsString() => argumentName switch
        {
            Argument.In => "in",
            Argument.Out => "out",
            Argument.Config => "config",
            Argument.GenerateConfig => "genconf",
            Argument.Help => "help",
            _ => throw new ArgumentOutOfRangeException(nameof(argumentName), argumentName, null)
        };

        /// <summary>
        /// Converts an ArgumentName to its CLI representation (with the -- prefix).
        /// </summary>
        public string AsCliString() => $"--{argumentName.AsString()}";
    }
}

