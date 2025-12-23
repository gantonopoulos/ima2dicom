// See https://aka.ms/new-console-template for more information

using FellowOakDicom;
using ImaToDicomConverter;


class SomatomArCtConverter
{
    const int Width = 512;
    const int Height = 512;
    const int BytesPerPixel = 2;
    const int PixelBytes = Width * Height * BytesPerPixel;
    
    static void Main(string[] args)
    {
        // Handle help first, before any parsing
        if (args.Length == 0 || args.Contains("--help"))
        {
            PrintUsage();
            return;
        }
        
        // Handle config generation
        if (args.Any(arg => arg.StartsWith($"{Argument.GenerateConfig.AsCliString()}")))
        {
            HandleConfigGeneration(args);
            return;
        }
        
        ArgumentParser.Parse(args)
            .Match(
                parsed =>
                {
                    Console.WriteLine($"Input Directory: {parsed.InputDirectory}");
                    Console.WriteLine($"Output Directory: {parsed.OutputDirectory}");
                    Console.WriteLine($"Configuration: {System.Text.Json.JsonSerializer.Serialize(parsed.Config)}");

                    // Here you would call the conversion logic using parsed.inputDirectory, parsed.outputDirectory, and parsed.config
                    // // Resolve the shell '~' to the actual home directory
                    // var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); // on Linux this maps to $HOME
                    // var inputPath = Path.Combine(home, "Documents", "314447");
                    // var outputPath = Path.Combine(home, "Documents", "314447", "DICOM_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                    //
                    //
                    int sliceIndex = 0;
                    var studyUid = DicomUID.Generate();
                    var seriesUid = DicomUID.Generate();
                    Directory.GetFiles(parsed.InputDirectory, "*.ima")
                        .ToList()
                        .ForEach((file) =>
                        {
                            byte[] pixelData = new SomatomArCtConverter().ReadPixelData(file);
                            DicomFile fileAsDicom = PixelDataToDicom(pixelData, sliceIndex++, studyUid, seriesUid, parsed.Config);
                            Directory.CreateDirectory(parsed.OutputDirectory);
                            fileAsDicom.Save(Path.Combine(parsed.OutputDirectory, Path.GetFileName(file).Replace(".ima", ".dcm")));
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

    private static void PrintUsage()
    {
        Console.WriteLine(
            $"Usage: SomatomArCtConverter " +
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
    
    private static void HandleConfigGeneration(string[] args)
    {
        // Parse the --genconf argument to see if a directory was specified
        var genconfArg = args.FirstOrDefault(arg => arg.StartsWith($"{Argument.GenerateConfig.AsCliString()}"));
        
        if (genconfArg == null)
        {
            Console.WriteLine("Error: Failed to find --genconf argument.");
            return;
        }
        
        string? outputDir = null;
        var parts = genconfArg.Split('=', 2);
        if (parts.Length == 2)
        {
            outputDir = parts[1];
            
            // Validate the directory exists
            if (!Directory.Exists(outputDir))
            {
                Console.WriteLine($"Error: Directory does not exist: {outputDir}");
                return;
            }
        }
        
        // Generate the config file
        ConfigGenerator.GenerateDefaultConfig(outputDir)
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
    
    private byte[] ReadPixelData(string file)
    {
        byte[] pixelData;
        using var fs = File.OpenRead(file);
        var pixelOffset = fs.Length - PixelBytes;

        fs.Seek(pixelOffset, SeekOrigin.Begin);
        pixelData = new byte[PixelBytes];
        fs.ReadExactly(pixelData);

        return pixelData;
    }
    
    private static DicomFile PixelDataToDicom(byte[] pixelData, int sliceIndex, DicomUID studyUid, DicomUID seriesUid, ConverterConfiguration config)
    {
        EnsureLittleEndianInt16(pixelData);
        
        var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian);
        
        // ---- identity (hardcoded, not configurable) ----
        ds.Add(DicomTag.SOPClassUID, DicomUID.CTImageStorage);
        ds.Add(DicomTag.SOPInstanceUID, DicomUID.Generate());
        ds.Add(DicomTag.StudyInstanceUID, studyUid);
        ds.Add(DicomTag.SeriesInstanceUID, seriesUid);
        
        // ---- geometry (configurable) ----
        config.Rows.IfSome(value => ds.Add(DicomTag.Rows, value));
        config.Columns.IfSome(value => ds.Add(DicomTag.Columns, value));
        
        // ---- pixel format (configurable) ----
        config.SamplesPerPixel.IfSome(value => ds.Add(DicomTag.SamplesPerPixel, value));
        config.PhotometricInterpretation.IfSome(value => ds.Add(DicomTag.PhotometricInterpretation, value));
        config.BitsAllocated.IfSome(value => ds.Add(DicomTag.BitsAllocated, value));
        config.BitsStored.IfSome(value => ds.Add(DicomTag.BitsStored, value));
        config.HighBit.IfSome(value => ds.Add(DicomTag.HighBit, value));
        config.PixelRepresentation.IfSome(value => ds.Add(DicomTag.PixelRepresentation, value));
        
        // ---- CT scaling (configurable) ----
        config.RescaleSlope.IfSome(value => ds.Add(DicomTag.RescaleSlope, value));
        config.RescaleIntercept.IfSome(value => ds.Add(DicomTag.RescaleIntercept, value));
        
        // ---- Display window (configurable) ----
        config.WindowCenter.IfSome(value => ds.Add(DicomTag.WindowCenter, value));
        config.WindowWidth.IfSome(value => ds.Add(DicomTag.WindowWidth, value));
        
        // ---- Spacing (configurable) ----
        config.SliceThickness.IfSome(value => ds.Add(DicomTag.SliceThickness, value));
        config.SpacingBetweenSlices.IfSome(value => ds.Add(DicomTag.SpacingBetweenSlices, value));
        config.PixelSpacing.IfSome(value => ds.Add(DicomTag.PixelSpacing, ParsePixelSpacing(value)));
        
        // ---- Modality (configurable) ----
        config.Modality.IfSome(value => ds.Add(DicomTag.Modality, value));
        
        // ---- Pixel data (hardcoded, always required) ----
        ds.Add(DicomTag.PixelData, pixelData);
        
        return new DicomFile(ds);
    }

    private static double[] ParsePixelSpacing(string pixelSpacingStr)
    {
        var parts = pixelSpacingStr.Split(',');
        if (parts.Length == 2 && 
            double.TryParse(parts[0].Trim(), out var row) && 
            double.TryParse(parts[1].Trim(), out var col))
        {
            return new[] { row, col };
        }

        throw new ArgumentException($"Invalid PixelSpacing format: '{pixelSpacingStr}'. Expected format: '0.48828125,0.48828125'");
    }
    
    static void EnsureLittleEndianInt16(byte[] data)
    {
        for (int i = 0; i < data.Length; i += 2)
        {
            // Siemens sometimes stores big-endian
            (data[i], data[i + 1]) = (data[i + 1], data[i]);
        }
    }

}