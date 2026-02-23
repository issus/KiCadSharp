# KiCad Schematic File Format (.kicad_sch)

Source: https://dev-docs.kicad.org/en/file-formats/sexpr-schematic/index.html

## Overview

This documentation covers the s-expression schematic file format for KiCad versions 6.0 and later. Schematic files use the `.kicad_sch` extension.

## Key Concepts

### Instance Path
Hierarchical sheets use paths composed of universally unique identifiers (UUIDs) separated by forward slashes to represent the schematic instance hierarchy. An example path looks like:
```
"/00000000-0000-0000-0000-00004b3a13a4/00000000-0000-0000-0000-00004b617b88"
```

The first identifier must be the root sheet UUID.

### Label and Pin Shapes

Five shape types apply to global labels, hierarchical labels, and hierarchical sheet pins:

| Token | Definition |
|-------|-----------|
| `input` | Input shape |
| `output` | Output shape |
| `bidirectional` | Bidirectional shape |
| `tri_state` | Tri-state shape |
| `passive` | Passive shape |

## File Structure

A schematic file contains these sections in order:

1. Header
2. Unique Identifier
3. Page Settings
4. Title Block
5. Library Symbols
6. Junctions
7. No Connects
8. Bus Entries
9. Wires and Buses
10. Images
11. Graphical Lines
12. Graphical Text
13. Local Labels
14. Global Labels
15. Hierarchical Labels
16. Symbols
17. Hierarchical Sheets
18. Root Sheet Instance

## Header Section

```
(kicad_sch
  (version VERSION)
  (generator GENERATOR)
  ;; contents...
)
```

- `version`: Uses YYYYMMDD date format
- `generator`: Identifies the program that created the file

Third-party generators should use identifiers other than "eeschema" to distinguish non-KiCad-generated files.

## Unique Identifier Section

```
UNIQUE_IDENTIFIER
```

The UUID identifies the schematic. Only the root schematic UUID serves as the virtual root sheet identifier.

## Library Symbol Section

```
(lib_symbols
  SYMBOL_DEFINITIONS...
)
```

Contains zero or more symbol definitions used in the schematic.

## Junction Section

```
(junction
  POSITION_IDENTIFIER
  (diameter DIAMETER)
  (color R G B A)
  UNIQUE_IDENTIFIER
)
```

- `POSITION_IDENTIFIER`: X and Y coordinates
- `diameter`: Junction diameter (0 = default)
- `color`: RGBA values (all 0 = default color)
- `UNIQUE_IDENTIFIER`: UUID for the junction

The section omits if no junctions exist.

## No Connect Section

```
(no_connect
  POSITION_IDENTIFIER
  UNIQUE_IDENTIFIER
)
```

- `POSITION_IDENTIFIER`: X and Y coordinates
- `UNIQUE_IDENTIFIER`: UUID for the no connect marker

Omitted if no no-connects exist.

## Bus Entry Section

```
(bus_entry
  POSITION_IDENTIFIER
  (size X Y)
  STROKE_DEFINITION
  UNIQUE_IDENTIFIER
)
```

- `POSITION_IDENTIFIER`: X and Y coordinates
- `size`: X and Y distance to end point
- `STROKE_DEFINITION`: Drawing properties
- `UNIQUE_IDENTIFIER`: UUID for the bus entry

Omitted if no bus entries exist.

## Wire and Bus Section

```
(wire | bus
  COORDINATE_POINT_LIST
  STROKE_DEFINITION
  UNIQUE_IDENTIFIER
)
```

- `COORDINATE_POINT_LIST`: Start and end point coordinates
- `STROKE_DEFINITION`: Drawing properties
- `UNIQUE_IDENTIFIER`: UUID for wire/bus

Omitted if no wires or buses exist.

## Image Section

Follows common image format specifications documented in `sexpr-intro.md`.

## Graphical Line Section

```
(polyline
  COORDINATE_POINT_LIST
  STROKE_DEFINITION
  UNIQUE_IDENTIFIER
)
```

- `COORDINATE_POINT_LIST`: Minimum two points required
- `STROKE_DEFINITION`: Line drawing properties
- `UNIQUE_IDENTIFIER`: UUID for the polyline

Omitted if no lines exist.

## Graphical Text Section

```
(text
  "TEXT"
  POSITION_IDENTIFIER
  TEXT_EFFECTS
  UNIQUE_IDENTIFIER
)
```

- `TEXT`: Quoted string content
- `POSITION_IDENTIFIER`: X, Y coordinates and rotation angle
- `TEXT_EFFECTS`: Drawing properties
- `UNIQUE_IDENTIFIER`: UUID for text

## Local Label Section

```
(label
  "TEXT"
  POSITION_IDENTIFIER
  TEXT_EFFECTS
  UNIQUE_IDENTIFIER
)
```

Defines wire or bus label names with same structure as graphical text.

## Global Label Section

```
(global_label
  "TEXT"
  (shape SHAPE)
  [(fields_autoplaced)]
  POSITION_IDENTIFIER
  TEXT_EFFECTS
  UNIQUE_IDENTIFIER
  PROPERTIES
)
```

- `TEXT`: Quoted label string
- `shape`: One of the five shape tokens
- `fields_autoplaced`: Optional flag indicating automatic placement
- `POSITION_IDENTIFIER`: X, Y coordinates and rotation
- `TEXT_EFFECTS`: Drawing properties
- `UNIQUE_IDENTIFIER`: UUID
- `PROPERTIES`: Symbol properties including inter-sheet references

Omitted if no global labels exist.

## Hierarchical Label Section

```
(hierarchical_label
  "TEXT"
  (shape SHAPE)
  POSITION_IDENTIFIER
  TEXT_EFFECTS
  UNIQUE_IDENTIFIER
)
```

Defines labels for hierarchical sheet connections with same shape options as global labels.

## Symbol Section

```
(symbol
  "LIBRARY_IDENTIFIER"
  POSITION_IDENTIFIER
  (unit UNIT)
  (in_bom yes|no)
  (on_board yes|no)
  UNIQUE_IDENTIFIER
  PROPERTIES
  (pin "1" (uuid e148648c-6605-4af1-832a-31eaf808c2f8))
  (instances
    (project "PROJECT_NAME"
      (path "PATH_INSTANCE"
        (reference "REFERENCE")
        (unit UNIT)
      )
    )
  )
)
```

- `LIBRARY_IDENTIFIER`: References symbol from library
- `POSITION_IDENTIFIER`: X, Y coordinates and rotation
- `unit`: Unit number from symbol definition
- `in_bom`: Bill of materials inclusion flag
- `on_board`: Footprint export flag
- `UNIQUE_IDENTIFIER`: Symbol UUID
- `PROPERTIES`: Symbol attributes
- `pin`: Pin UUID mapping
- `instances`: Per-project instance data with reference designators and unit numbers

## Hierarchical Sheet Section

```
(sheet
  POSITION_IDENTIFIER
  (size WIDTH HEIGHT)
  [(fields_autoplaced)]
  STROKE_DEFINITION
  FILL_DEFINITION
  UNIQUE_IDENTIFIER
  SHEET_NAME_PROPERTY
  FILE_NAME_PROPERTY
  HIERARCHICAL_PINS
  (instances
    (project "PROJECT_NAME"
      (path "PATH_INSTANCE"
        (page "PAGE_NUMBER")
      )
    )
  )
)
```

- `POSITION_IDENTIFIER`: X, Y coordinates and rotation
- `size`: Sheet width and height
- `fields_autoplaced`: Optional automatic placement flag
- `STROKE_DEFINITION`: Outline drawing properties
- `FILL_DEFINITION`: Fill properties
- `UNIQUE_IDENTIFIER`: Sheet UUID for mapping symbol and sheet instances
- `SHEET_NAME_PROPERTY`: Mandatory sheet name property
- `FILE_NAME_PROPERTY`: Mandatory file name property
- `HIERARCHICAL_PINS`: List of sheet pin definitions
- `instances`: Per-project sheet instances with page numbers

### Hierarchical Sheet Pin Definition

```
(pin
  "NAME"
  input | output | bidirectional | tri_state | passive
  POSITION_IDENTIFIER
  TEXT_EFFECTS
  UNIQUE_IDENTIFIER
)
```

- `NAME`: Must match identically named hierarchical label in associated file
- Electrical type: One of five connection type tokens
- `POSITION_IDENTIFIER`: X, Y coordinates and rotation
- `TEXT_EFFECTS`: Pin name drawing properties
- `UNIQUE_IDENTIFIER`: Pin UUID

## Root Sheet Instance Section

```
(path
  "/"
  (page "PAGE")
)
```

- Instance path is always "/" (no sheets point to root)
- `page`: Root sheet page number (any valid string)
