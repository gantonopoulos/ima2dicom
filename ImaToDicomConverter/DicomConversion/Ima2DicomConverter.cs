using FellowOakDicom;

namespace ImaToDicomConverter.DicomConversion;

internal class Ima2DicomConverter
{
    private const int Width = 512;
    private const int Height = 512;
    private const int BytesPerPixel = 2;
    private const int PixelBytes = Width * Height * BytesPerPixel;
    private readonly DicomUID _studyUid = DicomUID.Generate();
    private readonly DicomUID _seriesUid = DicomUID.Generate();

    private static byte[] ReadPixelData(string file)
    {
        using var fs = File.OpenRead(file);
        var pixelOffset = fs.Length - PixelBytes;

        fs.Seek(pixelOffset, SeekOrigin.Begin);
        var pixelData = new byte[PixelBytes];
        fs.ReadExactly(pixelData);

        return pixelData;
    }

    public DicomFile Ima2Dicom(string imaFilePath, ConvertionParameters config)
    {
        var pixelData = ReadPixelData(imaFilePath);
        EnsureLittleEndianInt16(pixelData);

        var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian);

        // ---- identity (hardcoded, not configurable) ----
        ds.Add(DicomTag.SOPClassUID, DicomUID.CTImageStorage);
        ds.Add(DicomTag.SOPInstanceUID, DicomUID.Generate());
        ds.Add(DicomTag.StudyInstanceUID, _studyUid);
        ds.Add(DicomTag.SeriesInstanceUID, _seriesUid);

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
            return [row, col];
        }

        throw new ArgumentException(
            $"Invalid PixelSpacing format: '{pixelSpacingStr}'. Expected format: '0.48828125,0.48828125'");
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