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
    /// Gets the 3D model scale (X, Y factors).
    /// </summary>
    public CoordPoint Scale { get; set; } = new(Coord.FromMm(1), Coord.FromMm(1));

    /// <summary>
    /// Gets the 3D model Z scale factor.
    /// </summary>
    public double ScaleZ { get; set; } = 1.0;

    /// <summary>
    /// Gets the 3D model rotation (X, Y angles in degrees).
    /// </summary>
    public CoordPoint Rotation { get; set; }

    /// <summary>
    /// Gets the 3D model Z rotation in degrees.
    /// </summary>
    public double RotationZ { get; set; }
}
