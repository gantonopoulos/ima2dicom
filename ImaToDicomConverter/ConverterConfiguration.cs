using LanguageExt;
using static LanguageExt.Prelude;

namespace ImaToDicomConverter;

/// <summary>
/// Configuration for static DICOM parameters.
/// Generated values (UIDs, instance-specific data) are not included here.
/// Only parameters explicitly provided in the JSON configuration will be set.
/// Null values will cause validation errors during parsing.
/// </summary>
public sealed class ConverterConfiguration
{
    // ---- Geometry ----
    /// <summary>
    /// Image height in pixels (e.g., 512)
    /// </summary>
    public Option<ushort> Rows { get; set; }

    /// <summary>
    /// Image width in pixels (e.g., 512)
    /// </summary>
    public Option<ushort> Columns { get; set; }

    // ---- Pixel Format ----
    /// <summary>
    /// Number of color planes (typically 1 for monochrome)
    /// </summary>
    public Option<ushort> SamplesPerPixel { get; set; }

    /// <summary>
    /// Photometric interpretation (e.g., "MONOCHROME2")
    /// </summary>
    public Option<string> PhotometricInterpretation { get; set; }

    /// <summary>
    /// Bits allocated per sample (typically 16)
    /// </summary>
    public Option<ushort> BitsAllocated { get; set; }

    /// <summary>
    /// Bits actually stored (typically 16)
    /// </summary>
    public Option<ushort> BitsStored { get; set; }

    /// <summary>
    /// High bit position (typically 15 for 16-bit data)
    /// </summary>
    public Option<ushort> HighBit { get; set; }

    /// <summary>
    /// Pixel representation: 0=unsigned, 1=signed (typically 1 for CT)
    /// </summary>
    public Option<ushort> PixelRepresentation { get; set; }

    // ---- CT Scaling ----
    /// <summary>
    /// Rescale slope for converting stored values to real values (typically 1.0)
    /// </summary>
    public Option<double> RescaleSlope { get; set; }

    /// <summary>
    /// Rescale intercept for converting stored values to real values (typically -1024.0 for CT)
    /// </summary>
    public Option<double> RescaleIntercept { get; set; }

    // ---- Display Window ----
    /// <summary>
    /// Window center for display (e.g., 2000.0 for CT)
    /// </summary>
    public Option<double> WindowCenter { get; set; }

    /// <summary>
    /// Window width for display (e.g., 4000.0 for CT)
    /// </summary>
    public Option<double> WindowWidth { get; set; }

    // ---- Spacing ----
    /// <summary>
    /// Slice thickness in mm (e.g., 3.0)
    /// </summary>
    public Option<double> SliceThickness { get; set; }

    /// <summary>
    /// Spacing between slices in mm (e.g., 0.01)
    /// </summary>
    public Option<double> SpacingBetweenSlices { get; set; }

    /// <summary>
    /// Pixel spacing in mm (row spacing, column spacing). Format: "0.48828125,0.48828125"
    /// </summary>
    public Option<string> PixelSpacing { get; set; }

    // ---- Modality ----
    /// <summary>
    /// Modality (e.g., "CT" for Computed Tomography)
    /// </summary>
    public Option<string> Modality { get; set; }
}