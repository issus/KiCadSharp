namespace OriginalCircuit.KiCad.Models.Sch;

/// <summary>
/// KiCad-specific graphic style for schematic pins.
/// </summary>
public enum PinGraphicStyle
{
    /// <summary>Simple line (default).</summary>
    Line = 0,

    /// <summary>Inverted (bubble) input.</summary>
    Inverted = 1,

    /// <summary>Clock input (triangle).</summary>
    Clock = 2,

    /// <summary>Inverted clock (bubble + triangle).</summary>
    InvertedClock = 3,

    /// <summary>Input low (active-low input).</summary>
    InputLow = 4,

    /// <summary>Clock low.</summary>
    ClockLow = 5,

    /// <summary>Output low (active-low output).</summary>
    OutputLow = 6,

    /// <summary>Edge clock high.</summary>
    EdgeClockHigh = 7,

    /// <summary>Non-logic pin.</summary>
    NonLogic = 8
}
