using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// Represents a 3D model reference on a KiCad footprint.
/// </summary>
public sealed class KiCadPcb3DModel
{
    /// <summary>
    /// Gets the 3D model file path.
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// Gets the 3D model offset (X, Y in mm).
    /// </summary>
    public CoordPoint Offset { get; set; }

    /// <summary>
    /// Gets the 3D model Z offset in mm.
    /// </summary>
    public double OffsetZ { get; set; }

    /// <summary>
    /// Gets the 3D model X scale factor (unitless).
    /// </summary>
    public double ScaleX { get; set; } = 1.0;

    /// <summary>
    /// Gets the 3D model Y scale factor (unitless).
    /// </summary>
    public double ScaleY { get; set; } = 1.0;

    /// <summary>
    /// Gets the 3D model Z scale factor (unitless).
    /// </summary>
    public double ScaleZ { get; set; } = 1.0;

    /// <summary>
    /// Gets the 3D model X rotation in degrees.
    /// </summary>
    public double RotationX { get; set; }

    /// <summary>
    /// Gets the 3D model Y rotation in degrees.
    /// </summary>
    public double RotationY { get; set; }

    /// <summary>
    /// Gets the 3D model Z rotation in degrees.
    /// </summary>
    public double RotationZ { get; set; }

    /// <summary>
    /// Gets whether this 3D model is hidden.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Gets or sets the 3D model opacity (KiCad 8+). Null means not specified.
    /// </summary>
    public double? Opacity { get; set; }
}
