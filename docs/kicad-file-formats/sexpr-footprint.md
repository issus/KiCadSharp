# Footprint Library File Format (.kicad_mod)

Source: https://dev-docs.kicad.org/en/file-formats/sexpr-footprint/index.html

## Introduction

The KiCad footprint library file format uses s-expressions and has been in use since version 4.0. Key characteristics include:

- Files use the `.kicad_mod` extension
- Each file contains a single footprint definition
- Footprint libraries consist of folders with one or more footprint library files

Prior to version 6 of KiCad, strings were only quoted when necessary. Starting with KiCad 6.0, all strings are quoted in saved files, though this creates no functional differences.

## Layout

A footprint library file contains two main sections:
1. Header section
2. Footprint definition

## Header Section

The header begins with the `footprint` token and is mandatory. The basic structure is:

```
(footprint "NAME"
  (version VERSION)
  (generator GENERATOR)

  ;; contents of the footprint library file...
)
```

### Header Components

**NAME**: A quoted string specifying the footprint name

**VERSION**: Uses the YYYYMMDD date format to indicate board version

**GENERATOR**: Identifies the program that created the file.

> Third party scripts should not use `pcbnew` as the generator identifier. This prevents confusion between third-party and official KiCad-generated files.

## Footprint Section

The footprint definition follows the s-expression board common definitions format, referenced in the s-expression introduction documentation. See `sexpr-intro.md` for the complete footprint definition syntax including:

- Footprint attributes (`attr`)
- Graphic items (`fp_text`, `fp_line`, `fp_rect`, `fp_circle`, `fp_arc`, `fp_poly`, `fp_curve`)
- Pads (`pad`)
- 3D models (`model`)
- Zones and groups
