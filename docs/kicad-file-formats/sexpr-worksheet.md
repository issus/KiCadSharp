# Work Sheet / Drawing Sheet File Format (.kicad_wks)

Source: https://dev-docs.kicad.org/en/file-formats/sexpr-worksheet/index.html

## Overview

The KiCad Work Sheet File Format documentation describes the s-expression format used in `.kicad_wks` files from version 6.0 onward. These files customize the default border and title block for schematics and boards.

## Key Specifications

**Coordinate System**: The minimum internal unit for work sheet files is 1 micrometer so there is maximum resolution of three decimal places or 0.001 mm.

**File Extension**: `.kicad_wks`

## Core Structure

Work sheet files contain three main sections:

1. **Header Section** - Identifies the file type with the `kicad_wks` token
2. **Set Up Section** - Configuration information
3. **Drawing Object Section** - Visual elements

## Header Section

```
(kicad_wks
  (version VERSION)
  (generator GENERATOR)
  ;; contents of the work sheet file...
)
```

The `version` attribute uses YYYYMMDD format. The `generator` identifies the program that created the file.

> Third party scripts should not use `pl_editor` as the generator identifier. Please use some other identifier so that bugs introduced by third party generators are not confused with a work sheet file created by KiCad.

## Set Up Section

```
(setup
  (textsize WIDTH HEIGHT)
  (linewidth WIDTH)
  (textlinewidth WIDTH)
  (left_margin DISTANCE)
  (right_margin DISTANCE)
  (top_margin DISTANCE)
  (bottom_margin DISTANCE)
)
```

This defines default text sizes, line widths, and page margins.

## Drawing Objects

The document details four object types plus images:

### Title Block Text

```
(tbtext
  "TEXT"
  (name "NAME")
  (pos X Y [CORNER])
  (font [(size WIDTH HEIGHT)] [bold] [italic])
  [(repeat COUNT)]
  [(incrx DISTANCE)]
  [(incry DISTANCE)]
  [(comment "COMMENT")]
)
```

### Graphical Line

```
(line
  (name "NAME")
  (start X Y [CORNER])
  (end X Y [CORNER])
  [(repeat COUNT)]
  [(incrx DISTANCE)]
  [(incry DISTANCE)]
  [(comment "COMMENT")]
)
```

### Graphical Rectangle

```
(rect
  (name "NAME")
  (start X Y [CORNER])
  (end X Y [CORNER])
  [(repeat COUNT)]
  [(incrx DISTANCE)]
  [(incry DISTANCE)]
  [(comment "COMMENT")]
)
```

### Graphical Polygon

```
(polygon
  (name "NAME")
  (pos X Y [CORNER])
  [(rotate ANGLE)]
  [(linewidth WIDTH)]
  COORDINATE_POINT_LIST
  [(repeat COUNT)]
  [(incrx DISTANCE)]
  [(incry DISTANCE)]
  [(comment "COMMENT")]
)
```

Requires minimum of two coordinate points.

### Image (Bitmap)

```
(bitmap
  (name "NAME")
  (pos X Y)
  (scale SCALAR)
  [(repeat COUNT)]
  [(incrx DISTANCE)]
  [(incry DISTANCE)]
  [(comment "COMMENT")]
  (pngdata IMAGE_DATA)
)
```

Image data is stored in PNG format as hexadecimal bytes:

```
(data XX1 ... XXN)
```

Maximum 32 bytes per `data` token, repeated until all image data is defined.

## Object Incrementing

Objects support repeating with incremental positioning via corner definitions:

| Token | Description |
|-------|-------------|
| ltcorner | Top left corner |
| lbcorner | Bottom left corner |
| rbcorner | Bottom right corner |
| rtcorner | Top right corner |

The `repeat`, `incrx`, and `incry` attributes control repetition count and directional spacing.
