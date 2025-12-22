// See https://aka.ms/new-console-template for more information

using FellowOakDicom;
using FellowOakDicom.Imaging;
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
                            DicomFile fileAsDicom = PixelDataToDicom(pixelData, sliceIndex++, studyUid, seriesUid);
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
        Console.WriteLine($"  {Argument.Config.AsCliString()}           Path to the configuration file.");
        Console.WriteLine("  --help             Show this help message.");
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
    
    private static DicomFile PixelDataToDicom(byte[] pixelData, int sliceIndex, DicomUID studyUid, DicomUID seriesUid)
    {
        EnsureLittleEndianInt16(pixelData);
        double sliceSpacing = 0.01;
        double sliceThickness = 3.0;
        double z = sliceIndex * sliceSpacing;
        var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
        {
            // ---- identity ----
            { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
            { DicomTag.SOPInstanceUID, DicomUID.Generate() },
            { DicomTag.StudyInstanceUID, studyUid },
            { DicomTag.SeriesInstanceUID, seriesUid },
            { DicomTag.Modality, "CT" },
            // ---- geometry ----
            { DicomTag.Rows, (ushort)Height },
            { DicomTag.Columns, (ushort)Width },
            // ---- pixel format ----
            { DicomTag.SamplesPerPixel, (ushort)1 },
            { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
            { DicomTag.BitsAllocated, (ushort)16 },
            { DicomTag.BitsStored, (ushort)16 },
            { DicomTag.HighBit, (ushort)15 },
            { DicomTag.PixelRepresentation, (ushort)1 }, // SIGNED
            // ---- CT scaling ----
            { DicomTag.RescaleSlope, 1.0 },
            { DicomTag.RescaleIntercept, -1024.0 },
            // { DicomTag.RescaleType, "HU" },

            // ---- Display window (helps viewers) ----
            { DicomTag.WindowCenter, 2000.0 },
            { DicomTag.WindowWidth, 4000.0 },
            { DicomTag.SliceThickness, sliceThickness },
            { DicomTag.SpacingBetweenSlices, sliceSpacing },
            
            { DicomTag.PixelSpacing, 0.48828125, 0.48828125 },
            
            
            // {
            //     DicomTag.ImageOrientationPatient, new double[]
            //     {
            //         1, 0, 0, // row direction
            //         0, 1, 0 // column direction
            //     }
            // },
            //{ DicomTag.ImagePositionPatient, 0.0, 0.0, z },
            // { DicomTag.SliceLocation, z },
            { DicomTag.PixelData, pixelData }
        };
        
        return new DicomFile(ds);
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