using FellowOakDicom;
using ImaToDicomConverter.ApplicationArguments;
using ImaToDicomConverter.DicomConversion;

namespace ImaToDicomConverter;

internal static class Program
{
    private static void Main(string[] args)
    {
        ArgumentCollecting.CollectArguments(args).Match(
            collectedArguments =>
            {
                if (collectedArguments.ContainsKey(Argument.Help.AsString()))
                {
                    PrintUsage();
                }
                else if (collectedArguments.TryGetValue(Argument.GenerateConfig.AsString(),
                             out var generatedConfigPath))
                {
                    HandleConfigGeneration(generatedConfigPath);
                }
                else
                {
                    ArgumentInterpreting.InterpretArguments(collectedArguments).Match(
                        parsedArguments =>
                        {
                            Console.WriteLine($"Input Directory: {parsedArguments.InputDirectory}");
                            Console.WriteLine($"Output Directory: {parsedArguments.OutputDirectory}");
                            Console.WriteLine($"Configuration: " +
                                              $"{System.Text.Json.JsonSerializer.Serialize(parsedArguments.Config)}");

                            var converter = new Ima2DicomConverter();
                            Directory.GetFiles(parsedArguments.InputDirectory, "*.ima")
                                .ToList()
                                .ForEach((file) =>
                                {
                                    DicomFile convertedFile = converter.Ima2Dicom(file, parsedArguments.Config);
                                    convertedFile.Save(Path.Combine(parsedArguments.OutputDirectory,
                                        Path.GetFileName(file).Replace(".ima", ".dcm")));
                                });


                            Console.WriteLine("Converted successfully.");
                        },
                        error =>
                        {
                            Console.WriteLine($"Error: {error.Message}");
                            PrintUsage();
                        }
                    );
                }

            },
            error =>
            {
                Console.WriteLine($"Error collecting arguments: {error.Message}");
                PrintUsage();
            }
        );
    }

    private static void PrintUsage()
    {
        Console.WriteLine("ima2dicom - Convert Siemens .ima files to DICOM format");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine($"  ima2dicom [{Argument.In.AsCliString()}=<path>] [{Argument.Out.AsCliString()}=<path>] [{Argument.Config.AsCliString()}=<path>]");
        Console.WriteLine($"  ima2dicom {Argument.GenerateConfig.AsCliString()}[=<output_dir>]");
        Console.WriteLine($"  ima2dicom {Argument.Help.AsCliString()}");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine($"  {Argument.In.AsCliString()}=<path>");
        Console.WriteLine($"      Input directory containing .ima files to convert.");
        Console.WriteLine($"      Default: Current directory");
        Console.WriteLine();
        Console.WriteLine($"  {Argument.Out.AsCliString()}=<path>");
        Console.WriteLine($"      Output directory for converted DICOM files.");
        Console.WriteLine($"      Default: Current directory");
        Console.WriteLine($"      Note: Directory will be created if it doesn't exist.");
        Console.WriteLine();
        Console.WriteLine($"  {Argument.Config.AsCliString()}=<path>");
        Console.WriteLine($"      Path to JSON configuration file with DICOM tag values.");
        Console.WriteLine($"      Default: Built-in default configuration");
        Console.WriteLine();
        Console.WriteLine($"  {Argument.GenerateConfig.AsCliString()}[=<output_dir>]");
        Console.WriteLine($"      Generate a default configuration file for customization.");
        Console.WriteLine($"      Saves to 'default-config.json' in specified directory or current directory.");
        Console.WriteLine($"      The application exits after generating the file.");
        Console.WriteLine();
        Console.WriteLine($"  {Argument.Help.AsCliString()}");
        Console.WriteLine($"      Display this help message and exit.");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  # Convert files in current directory, output to current directory");
        Console.WriteLine("  ima2dicom");
        Console.WriteLine();
        Console.WriteLine("  # Convert files from specific input directory");
        Console.WriteLine($"  ima2dicom {Argument.In.AsCliString()}=/path/to/ima/files");
        Console.WriteLine();
        Console.WriteLine("  # Convert with custom output directory (will be created)");
        Console.WriteLine($"  ima2dicom {Argument.In.AsCliString()}=/input {Argument.Out.AsCliString()}=/output/dicom");
        Console.WriteLine();
        Console.WriteLine("  # Use custom configuration file");
        Console.WriteLine($"  ima2dicom {Argument.In.AsCliString()}=/input {Argument.Out.AsCliString()}=/output {Argument.Config.AsCliString()}=my-config.json");
        Console.WriteLine();
        Console.WriteLine("  # Generate default config file for editing");
        Console.WriteLine($"  ima2dicom {Argument.GenerateConfig.AsCliString()}");
        Console.WriteLine();
        Console.WriteLine("BEHAVIOR:");
        Console.WriteLine("  - All arguments are optional with sensible defaults");
        Console.WriteLine("  - Input directory must exist (returns error if not found)");
        Console.WriteLine("  - Output directory is created automatically if missing");
        Console.WriteLine("  - If no config is specified, built-in defaults are used");
        Console.WriteLine("  - Help and genconf commands exit immediately after execution");
    }
    
    private static void HandleConfigGeneration(string outputDir)
    {
        // Generate the config file
        ConfigurationGenerator.GenerateDefaultConfig(outputDir)
            .Match(
                path =>
                {
                    Console.WriteLine($"Successfully generated default configuration file:");
                    Console.WriteLine($"  {path}");
                    Console.WriteLine();
                    Console.WriteLine("You can now edit this file and use it with:");
                    Console.WriteLine($"  {Argument.Config.AsCliString()}={path}");
                },
                error =>
                {
                    Console.WriteLine($"Error generating configuration file: {error.Message}");
                }
            );
    }
}