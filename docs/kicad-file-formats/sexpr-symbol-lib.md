# Symbol Library File Format (.kicad_sym)

Source: https://dev-docs.kicad.org/en/file-formats/sexpr-symbol-lib/index.html

## Overview

KiCad's symbol library file format is documented for versions 6.0 and later. Symbol library files use the `.kicad_sym` extension and can contain one or more symbol definitions.

## Document Structure

The documentation is organized into three main sections:

1. **Header Section** - Required metadata for the library file
2. **Symbol Section** - Individual symbol definitions
3. **Layout** - Overall file organization

## Header Section

The header begins with the `kicad_symbol_lib` token, which identifies the file as a KiCad symbol library. The basic structure is:

```
(kicad_symbol_lib
  (version VERSION)
  (generator GENERATOR)
  ;; symbol definitions...
)
```

**Key components:**

- **version**: Uses YYYYMMDD date format to indicate the symbol library version
- **generator**: Identifies the program that created the file

> Third party scripts should not use `kicad_symbol_editor` as the generator identifier to prevent confusion with officially-generated files.

## Symbol Section

The `symbol` token defines individual symbols within the library. Symbol library files can contain zero or more symbol definitions. Each symbol definition follows the common symbol definition format. See `sexpr-intro.md` for the complete symbol definition syntax including:

- Symbol properties (Reference, Value, Footprint, Datasheet, and custom properties)
- Graphic items (arc, circle, bezier, polyline, rectangle, text)
- Pins (with electrical types and graphic styles)
- Units and styles
- Extended symbols (derived from parent symbols)

### Symbol Definition Structure

```
(symbol
  "LIBRARY_ID" | "UNIT_ID"
  [(extends "LIBRARY_ID")]
  [(pin_numbers hide)]
  [(pin_names [(offset OFFSET)] hide)]
  (in_bom yes | no)
  (on_board yes | no)
  SYMBOL_PROPERTIES...
  GRAPHIC_ITEMS...
  PINS...
  UNITS...
  [(unit_name "UNIT_NAME")]
)
```

### Mandatory Properties for Parent Symbols

- `Reference` - Component designator (e.g., R, C, U)
- `Value` - Component value
- `Footprint` - Library identifier for the associated footprint
- `Datasheet` - URL or path to the datasheet

### Reserved Property Keys

These keys cannot be user-defined:
- `ki_keywords` - Search keywords
- `ki_description` - Component description
- `ki_locked` - Lock status
- `ki_fp_filters` - Footprint filters
