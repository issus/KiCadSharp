using OriginalCircuit.Eda.Models;
using OriginalCircuit.Eda.Models.Pcb;
using OriginalCircuit.Eda.Primitives;

namespace OriginalCircuit.KiCad.Models.Pcb;

/// <summary>
/// KiCad PCB via, mapped from <c>(via [TYPE] (at X Y) (size D) (drill D) (layers L1 L2) (net N) (tstamp UUID))</c>.
/// </summary>
public sealed class KiCadPcbVia : IPcbVia
{
    /// <inheritdoc />
    public CoordPoint Location { get; set; }

    /// <inheritdoc />
    public Coord Diameter { get; set; }

    /// <inheritdoc />
    public Coord HoleSize { get; set; }

    /// <inheritdoc />
    public int StartLayer { get; set; }

    /// <inheritdoc />
    public int EndLayer { get; set; }

    /// <summary>
    /// Gets the start layer name.
    /// </summary>
    public string? StartLayerName { get; set; }

    /// <summary>
    /// Gets the end layer name.
    /// </summary>
    public string? EndLayerName { get; set; }

    /// <summary>
    /// Gets the via type (through, blind, micro).
    /// </summary>
    public ViaType ViaType { get; set; }

    /// <summary>
    /// Gets the net number.
    /// </summary>
    public int Net { get; set; }

    /// <summary>
    /// Gets the UUID / tstamp.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets or sets the token name used for the UUID node (<c>uuid</c> or <c>tstamp</c>).
    /// </summary>
    public string UuidToken { get; set; } = "uuid";

    /// <summary>
    /// Gets or sets whether the UUID value is a bare symbol (unquoted) vs a quoted string.
    /// </summary>
    public bool UuidIsSymbol { get; set; }

    /// <summary>
    /// Gets whether this via is free (not locked to a net).
    /// </summary>
    public bool IsFree { get; set; }

    /// <summary>
    /// Gets whether this via is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets whether the locked flag uses child node format <c>(locked yes)</c> instead of bare symbol.
    /// </summary>
    public bool LockedIsChildNode { get; set; }

    /// <summary>
    /// Gets whether to remove unused layers.
    /// <c>null</c> means the node is not present; <c>true</c>/<c>false</c> means <c>(remove_unused_layers yes/no)</c>.
    /// </summary>
    public bool? RemoveUnusedLayers { get; set; }

    /// <summary>
    /// Gets whether to keep end layers.
    /// <c>null</c> means the node is not present; <c>true</c>/<c>false</c> means <c>(keep_end_layers yes/no)</c>.
    /// </summary>
    public bool? KeepEndLayers { get; set; }

    /// <summary>
    /// Gets or sets the status flags.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>Gets or sets whether teardrops are enabled on this via.</summary>
    public bool TeardropEnabled { get; set; }
    /// <summary>Gets or sets the teardrop best length ratio.</summary>
    public double? TeardropBestLengthRatio { get; set; }
    /// <summary>Gets or sets the teardrop max length.</summary>
    public Coord? TeardropMaxLength { get; set; }
    /// <summary>Gets or sets the teardrop best width ratio.</summary>
    public double? TeardropBestWidthRatio { get; set; }
    /// <summary>Gets or sets the teardrop max width.</summary>
    public Coord? TeardropMaxWidth { get; set; }
    /// <summary>Gets or sets whether teardrop curved edges are enabled.</summary>
    public bool? TeardropCurvedEdges { get; set; }
    /// <summary>Gets or sets the teardrop filter ratio.</summary>
    public double? TeardropFilterRatio { get; set; }
    /// <summary>Gets or sets whether two-segment teardrops are allowed.</summary>
    public bool? TeardropAllowTwoSegments { get; set; }
    /// <summary>Gets or sets whether zone connections are preferred.</summary>
    public bool? TeardropPreferZoneConnections { get; set; }
    /// <summary>Gets or sets whether tenting was present.</summary>
    public bool HasTenting { get; set; }
    /// <summary>Gets or sets the front tenting state.</summary>
    public bool? TentingFront { get; set; }
    /// <summary>Gets or sets the back tenting state.</summary>
    public bool? TentingBack { get; set; }
    /// <summary>Gets or sets whether tenting uses child node format.</summary>
    public bool TentingIsChildNode { get; set; }
    /// <summary>Gets or sets the front tenting value string (none/yes/no).</summary>
    public string? TentingFrontValue { get; set; }
    /// <summary>Gets or sets the back tenting value string (none/yes/no).</summary>
    public string? TentingBackValue { get; set; }
    /// <summary>Gets or sets the zone layer connections.</summary>
    public List<string>? ZoneLayerConnections { get; set; }

    /// <summary>Gets or sets the capping value (yes/no/none).</summary>
    public string? Capping { get; set; }
    /// <summary>Gets or sets the filling value (yes/no/none).</summary>
    public string? Filling { get; set; }
    /// <summary>Gets or sets whether covering section was present.</summary>
    public bool HasCovering { get; set; }
    /// <summary>Gets or sets the covering front value.</summary>
    public string? CoveringFront { get; set; }
    /// <summary>Gets or sets the covering back value.</summary>
    public string? CoveringBack { get; set; }
    /// <summary>Gets or sets whether plugging section was present.</summary>
    public bool HasPlugging { get; set; }
    /// <summary>Gets or sets the plugging front value.</summary>
    public string? PluggingFront { get; set; }
    /// <summary>Gets or sets the plugging back value.</summary>
    public string? PluggingBack { get; set; }

    /// <inheritdoc />
    public CoordRect Bounds
    {
        get
        {
            var half = Diameter / 2;
            return new CoordRect(
                new CoordPoint(Location.X - half, Location.Y - half),
                new CoordPoint(Location.X + half, Location.Y + half));
        }
    }
}
