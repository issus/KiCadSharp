# KiCad Board File Format (.kicad_pcb)

Source: https://dev-docs.kicad.org/en/file-formats/sexpr-pcb/index.html

## Overview

KiCad's PCB board files use the `.kicad_pcb` extension and employ an s-expression format. This file format was introduced with the launch of KiCad 4.0. The format supports all versions from KiCad 6.0 onward.

Saving an older board file to the latest file format will result in strings being quoted even though there is no functional change in the board itself.

## File Structure

A complete board file contains these sections in order:

1. **Header** (required) - Identifies the file as a KiCad PCB
2. **General** (required) - Board thickness and basic info
3. **Page** (required) - Page size settings
4. **Layers** (required) - Layer definitions
5. **Setup** (required) - Design rules and plot settings
6. **Properties** - Custom metadata (optional)
7. **Nets** (required) - Net definitions
8. **Footprints** - Component placements (optional)
9. **Graphic Items** - Lines, text, polygons (optional)
10. **Images** - Embedded images (optional)
11. **Tracks** - Segments, vias, arcs (optional)
12. **Zones** - Copper pours (optional)
13. **Groups** - Element groupings (optional)

The section order is not critical other than the header must be the first token.

## Header Section

```
(kicad_pcb
  (version VERSION)
  (generator GENERATOR)
  ;; remaining sections...
)
```

The version uses YYYYMMDD date format. Third party scripts should not use `pcbnew` as the generator identifier. Please use some other identifier so that bugs introduced by third party generators are not confused with the board file created by KiCad.

## General Section

```
(general
  (thickness THICKNESS)
)
```

The thickness token specifies overall board thickness. Most of the redundant information in the `general` section prior to version 6 has been removed.

## Layers Section

```
(layers
  (
    ORDINAL
    "CANONICAL_NAME"
    TYPE
    ["USER_NAME"]
  )
)
```

- **ORDINAL**: Integer for layer stack ordering
- **CANONICAL_NAME**: Internal layer identifier
- **TYPE**: `jumper`, `mixed`, `power`, `signal`, or `user`
- **USER_NAME**: Optional custom name

## Setup Section

```
(setup
  [(STACK_UP_SETTINGS)]
  (pad_to_mask_clearance CLEARANCE)
  [(solder_mask_min_width MINIMUM_WIDTH)]
  [(pad_to_paste_clearance CLEARANCE)]
  [(pad_to_paste_clearance_ratio RATIO)]
  [(aux_axis_origin X Y)]
  [(grid_origin X Y)]
  (PLOT_SETTINGS)
)
```

Key elements include manufacturing parameters and plotting configurations.

### Stack Up Settings

```
(stackup
  (LAYER_STACK_UP_DEFINITIONS)
  [(copper_finish "FINISH")]
  [(dielectric_constraints yes | no)]
  [(edge_connector yes | bevelled)]
  [(castellated_pads yes)]
  [(edge_plating yes)]
)
```

Individual layers within stack up:

```
(layer
  "NAME" | dielectric
  NUMBER
  (type "DESCRIPTION")
  [(color "COLOR")]
  [(thickness THICKNESS)]
  [(material "MATERIAL")]
  [(epsilon_r DIELECTRIC_RESISTANCE)]
  [(loss_tangent LOSS_TANGENT)]
)
```

### Plot Settings

The `pcbplotparams` section stores plotting and printing parameters:

```
(pcbplotparams
  (layerselection HEXADECIMAL_BIT_SET)
  (disableapertmacros true | false)
  (usegerberextensions true | false)
  (usegerberattributes true | false)
  (usegerberadvancedattributes true | false)
  (creategerberjobfile true | false)
  (svguseinch true | false)
  (svgprecision PRECISION)
  (excludeedgelayer true | false)
  (plotframeref true | false)
  (viasonmask true | false)
  (mode MODE)
  (useauxorigin true | false)
  (hpglpennumber NUMBER)
  (hpglpenspeed SPEED)
  (hpglpendiameter DIAMETER)
  (dxfpolygonmode true | false)
  (dxfimperialunits true | false)
  (dxfusepcbnewfont true | false)
  (psnegative true | false)
  (psa4output true | false)
  (plotreference true | false)
  (plotvalue true | false)
  (plotinvisibletext true | false)
  (sketchpadsonfab true | false)
  (subtractmaskfromsilk true | false)
  (outputformat FORMAT)
  (mirror true | false)
  (drillshape SHAPE)
  (scaleselection 1)
  (outputdirectory "PATH")
)
```

Output format values:
- 0 = Gerber
- 1 = PostScript
- 2 = SVG
- 3 = DXF
- 4 = HPGL
- 5 = PDF

## Nets Section

```
(net
  ORDINAL
  "NET_NAME"
)
```

Each net requires an ordinal number and name. The net class section has been moved out of the board file into the design rules file.

## Footprints Section

Footprints are defined using s-expression format. The section omits any footprints that aren't placed on the board. See `sexpr-intro.md` for the full footprint definition syntax.

## Graphic Items Section

This section includes drawn elements like lines, rectangles, and text. It's excluded if no graphics exist. See `sexpr-intro.md` for gr_text, gr_line, gr_rect, gr_circle, gr_arc, gr_poly, and bezier definitions.

## Images Section

Embedded images appear here using s-expression format. This section is absent if no images are present.

## Tracks Section

### Track Segment

```
(segment
  (start X Y)
  (end X Y)
  (width WIDTH)
  (layer LAYER_DEFINITION)
  [(locked)]
  (net NET_NUMBER)
  (tstamp UUID)
)
```

Defines a single track segment with coordinates, width, layer, and net assignment.

### Track Via

```
(via
  [TYPE]
  [(locked)]
  (at X Y)
  (size DIAMETER)
  (drill DIAMETER)
  (layers LAYER1 LAYER2)
  [(remove_unused_layers)]
  [(keep_end_layers)]
  [(free)]
  (net NET_NUMBER)
  (tstamp UUID)
)
```

Valid via types are `blind` and `micro`. Through-hole vias have no type specification. The `free` token indicates a via can move outside its assigned net.

### Track Arc

```
(arc
  (start X Y)
  (mid X Y)
  (end X Y)
  (width X Y)
  (layer LAYER_DEFINITION)
  [(locked)]
  (net NET_NUMBER)
  (tstamp UUID)
)
```

Track arcs require start, midpoint, and end coordinates to define the arc geometry.

## Zones Section

Copper zones are defined with pour parameters, layer assignment, and fill rules. This section doesn't appear if no zones exist. See `sexpr-intro.md` for the full zone definition syntax.

## Group Section

Groups allow organizing board elements. This section is absent if no groups are defined. See `sexpr-intro.md` for the group definition syntax.

## Example Board File

```
(kicad_pcb (version 3) (host pcbnew "(2013-02-20 BZR 3963)-testing")
  (general
    (links 2)
    (no_connects 0)
    (area 57.924999 28.924999 74.075001 42.075001)
    (thickness 1.6)
    (drawings 5)
    (tracks 5)
    (zones 0)
    (modules 2)
    (nets 3)
  )
  ;; ... remaining sections
)
```

## Key Principles

1. S-expressions use parentheses for nesting and organization
2. Tokens are whitespace-delimited
3. Strings requiring special characters must be quoted
4. UUIDs (tstamp) uniquely identify objects
5. Net numbers reference entries in the nets section
6. Layer names are canonical identifiers from the layers section
7. Coordinates are typically in millimeters
8. Boolean values use `true` or `false` keywords
