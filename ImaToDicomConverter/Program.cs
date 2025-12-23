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
                                    Directory.CreateDirectory(parsedArguments.OutputDirectory);
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
        Console.WriteLine(
            $"Usage: ima2dicom " +
            $"{Argument.In.AsCliString()}=<input_directory> " +
            $"{Argument.Out.AsCliString()}=<output_directory> " +
            $"{Argument.Config.AsCliString()}=<config_file>");
        Console.WriteLine("Options:");
        Console.WriteLine($"  {Argument.In.AsCliString()}     Directory containing .ima files to convert.");
        Console.WriteLine($"  {Argument.Out.AsCliString()}    Directory to save converted DICOM files.");
        Console.WriteLine($"  {Argument.Config.AsCliString()} Path to the configuration file.");
        Console.WriteLine($"  {Argument.GenerateConfig.AsCliString()}[=<output_dir>]  Generate default configuration file. Optional: specify output directory.");
        Console.WriteLine("  --help             Show this help message.");
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