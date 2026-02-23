# Legacy Board Format (pre KiCad 4.0)

Source: https://dev-docs.kicad.org/en/file-formats/legacy-pcb/index.html

## Overview

This documentation covers KiCad's PCB file format prior to version 4.0, before the introduction of S-expression format. The documentation is preserved as reference material.

## V1 Version Information

**File Format Characteristics:**
- ASCII format for board files (*.brd)
- Dimensions in 1/10000 inch (except page size in 1/1000 inch)
- First line format: "PCBNEW-BOARD Version 1 date 02/04/2011 15:04:20"

**Structure:**
Data blocks follow the pattern:
```
$DESCRIPTION
some data
...
$endDESCRIPTION
```

**Example Header:**
```
$GENERAL
encoding utf-8
LayerCount 2
Ly 1FFF8001
Links 66
NoConn 0
Di 24940 20675 73708 40323
Ndraw 16
Ntrack 267
Nzone 1929
Nmodule 29
Nnets 26
$EndGENERAL

$SHEETDESCR
Sheet A4 11700 8267
Title ""
Date "23 feb 2004"
Rev ""
Comp ""
Comment1 ""
Comment2 ""
Comment3 ""
Comment4 ""
$EndSHEETDESCR
```

## V2 Version Information

**Key Differences from V1:**
- Identical file format structure, still uses .brd extension
- Dimensions in millimeters with floating-point notation (except page size)
- Internal unit changed to 1nm for higher precision
- Backward compatible for reading V1 files
- First line example: "PCBNEW-BOARD Version 2 date 22/02/2013 15:04:20"

**Example Structure:**
```
PCBNEW-BOARD Version 2 date 22/02/2013 10:33:30

# Created by Pcbnew(2013-02-20 BZR 3963)-testing

$GENERAL
encoding utf-8
Units mm
LayerCount 2
EnabledLayers 1FFF8001
Links 200
NoConn 0
Di 69.241669 24.89454 202.336401 196.2404
Ndraw 19
Ntrack 779
Nzone 0
BoardThickness 1.6002
Nmodule 25
Nnets 111
$EndGENERAL
```

## Layer Numbering System

**Copper Layers:**
- 0: Copper layer
- 1-14: Inner layers
- 15: Component layer

**Technical Layers:**
- 16: Copper side adhesive layer
- 17: Component side adhesive layer
- 18: Copper side solder paste layer
- 19: Component solder paste layer
- 20: Copper side silk screen layer
- 21: Component silk screen layer
- 22: Copper side solder mask layer
- 23: Component solder mask layer
- 24: Draw layer (general drawings)
- 25: Comment layer
- 26: ECO1 layer
- 27: ECO2 layer
- 28: Edge layer (visible on all layers)
- 29-31: Not yet used

**Mask Layer:**
A 32-bit hexadecimal mask indicates layer group usage (0-32 layers). Bit 0 represents copper layer, bit 1 represents inner layer 1, etc. Bit 27 is the edge layer.

## First Line Format

**Syntax:** `PCBNEW-BOARD Version <version number> date <date>-<time>`

Date and time are informational only and not used by Pcbnew.

## $GENERAL Block

This data is useful only when loading file. It is used by Pcbnew for displaying activity when loading data.

| Parameter | Meaning |
|-----------|---------|
| Ly | Obsolete (old Pcbnew compatibility) |
| Links | Total number of connections |
| NoConn | Remaining unconnected connections |
| Di | Bounding box: X_start Y_start X_end Y_end |
| Ndraw | Number of draw items (edge segments, texts, etc.) |
| Ntrack | Number of track segments |
| Nzone | Number of zone segments |
| Nmodule | Number of modules |
| Nnets | Number of nets |

## $SHEETDESCR Block

Contains page size and descriptive text.

| Parameter | Units | Description |
|-----------|-------|-------------|
| Sheet | mils (1/1000") | Page size with X_size and Y_size |
| Title | text | Title text |
| Date | text | Date text |
| Rev | text | Revision text |
| Comp | text | Company name |
| Comment1-4 | text | Comment lines |

## $SETUP Block

Design settings useful for board edition.

**Example:**
```
$SETUP
InternalUnit 0.000100 INCH
Layers 2
Layer[0] Cuivre signal
Layer[15] Composant signal
TrackWidth 250
TrackWidthHistory 25
TrackWidthHistory 170
TrackWidthHistory 250
TrackClearence 110
ZoneClearence 150
DrawSegmWidth 150
EdgeSegmWidth 50
ViaSize 600
ViaDrill 250
ViaSizeHistory 600
MicroViaSize 200
MicroViaDrill 80
MicroViasAllowed 0
TextPcbWidth 170
TextPcbSize 600 800
EdgeModWidth 150
TextModSize 600 600
TextModWidth 120
PadSize 1500 2500
PadDrill 1200
AuxiliaryAxisOrg 29500 55500
$EndSETUP
```

| Parameter | Meaning |
|-----------|---------|
| InternalUnit | Internal Pcbnew unit; all coordinates use this unit |
| Layers | Number of layers (2 = double-sided board; 1-16 valid) |
| Layer[n] | Layer name and type (signal type not currently used) |
| TrackWidth | Current track width |
| TrackWidthHistory | Last used track widths |
| TrackClearence | DRC isolation distance |
| ZoneClearence | Isolation used in zone filling |
| DrawSegmWidth | Current segment width for technical layer drawings |
| EdgeSegmWidth | Current segment width for edge layer drawings |
| ViaSize | Current via size |
| ViaDrill | Via drill size for board |
| ViaSizeHistory | Last used via sizes |
| MicroViaSize | Micro via size |
| MicroViaDrill | Micro via drill |
| MicroViasAllowed | Enable/disable micro vias |
| TextPcbWidth | Current text width for copper/technical layers |
| TextPcbSize | Current text X Y size |
| EdgeModWidth | Current segment width for footprint edition |
| TextModSize | Current text XY size for footprint edition |
| TextModWidth | Current text width for footprint edition |
| PadSize | Current X Y pad size (footprint edition) |
| PadDrill | Current pad drill |
| AuxiliaryAxisOrg | Auxiliary axis position (reference for EXCELLON files) |

## $EQUIPOT Block

Describes a net name.

**Format:**
```
$EQUIPOT
Na <internal net number> "net name"
St ~
$EndEQUIPOT
```

**Notes:**
- Internal net number is arbitrary, computed by Pcbnew
- Net 0 is reserved for unconnected pads (not a real net)

**Example:**
```
$EQUIPOT
Na 0 ""
St ~
$EndEQUIPOT

$EQUIPOT
Na 1 "DONE"
St ~
$EndEQUIPOT

$EQUIPOT
Na 2 "N-000026"
St ~
$EndEQUIPOT

$EQUIPOT
Na 3 "TD0/PROG"
St ~
$EndEQUIPOT
```

## $MODULE Block

Describes footprint/module definitions.

**Format:** `$MODULE <module name>` ... `$EndMODULE <module name>`

All coordinates are relative to the module position. This means the coordinates of segments, pads, texts are given for a module in position 0, rotation 0. If a module is rotated or mirrored, real coordinates must be computed according to the real position and rotation.

### Module General Description

**Example:**
```
$MODULE bornier6
Po 62000 30500 2700 15 3EC0C28A 3EBF830C ~~
Li bornier6
Cd Bornier d'alimentation 4 pins
Kw DEV
Sc 3EBF830C
Op 0 0 0
```

| Parameter | Format | Meaning |
|-----------|--------|---------|
| Po | Xpos Ypos Orientation Layer TimeStamp Attribut1 Attribut2 | Position, rotation (0.1 deg), layer, timestamp, attributes (F=fixed/~=moveable, P=autoplaced/~=manual) |
| Li | module lib name | Module library name |
| Cd | description text | Comment description (shown when browsing libraries) |
| Kw | keyword list | Keywords for footprint selection |
| Sc | TimeStampOp | Timestamp |
| Op | rot90 rot180 | Rotation costs for autoplace (0-10, where 0=no rotation, 10=no cost) |

**Note:** Components are typically on layer 15 (component layer) or 0 (copper layer). Layer 0 indicates a mirrored component with X-axis as mirror axis.

### Field Description

Fields range from 2-12 items:
- Field 0: Component reference (U1, R5, etc.) - required
- Field 1: Component value (10K, 74LS02, etc.) - required
- Other fields: Optional comments

**Format:** `T<field number> <Xpos> <Ypos> <Xsize> <Ysize> <rotation> <penWidth> N <visible> <layer> "text"`

| Parameter | Units | Meaning |
|-----------|-------|---------|
| field number | enumeration | 0=reference, 1=value, etc. |
| Xpos | tenths of mils | Horizontal offset from module position |
| Ypos | tenths of mils | Vertical offset from module position |
| Xsize | tenths of mils | Horizontal size of character 'M' |
| Ysize | tenths of mils | Vertical size of character 'M' |
| rotation | tenths of degrees | Counterclockwise rotation from horizontal |
| penWidth | tenths of mils | Character drawing pen width |
| N | flag | Parser flag |
| visible | boolean | I=invisible, V=visible |
| layer | enumeration | See layer numbers |

**Examples:**
```
T0 500 -3000 1030 629 2700 120 N V 21 "P1"
T1 0 3000 1201 825 2700 120 N V 21 "CONN_6"
```

### Module Drawings

Cannot be on copper layers (DRC ignores them). Types include segments, circles, arcs, and polygons.

#### Draw Segment

**Format:** `DS Xstart Ystart Xend Yend Width Layer`

```
DS -6000 -1500 -6000 1500 120 21
DS 6000 1500 6000 -1500 120 21
```

#### Circle

**Format:** `DC Xcentre Ycentre Xpoint Ypoint Width Layer`

Xpoint and Ypoint define a point on the circle.

#### Arc

**Format:** `DA Xcentre Ycentre Xstart_point Ystart_point angle width layer`

- angle: Arc angle in 0.1 degree units
- Currently only 90-degree arcs supported (angle = 900)
- Center coordinates computed from start point, end point, and angle

#### Polygon

**Format:**
```
DP 0 0 0 0 corners_count width layer
Dl corner_posx corner_posy
```

First line initiates polygon. Polygon should be closed (otherwise polyline). Width is outline thickness.

### Module Pads ($PAD)

Pads have different shapes and attributes.

**Shapes:**
- Circle
- Oblong (oval)
- Rectangular (square is a rectangle variant)
- Trapeze

**Attributes:**
- Normal (usually has hole)
- SMD (Surface Mounted Devices, no hole)
- Connector (like PC board bus connectors)
- Mechanical (holes for mechanical use)

**Format:**
```
$PAD
Sh "<pad name>" <shape> Xsize Ysize Xdelta Ydelta Orientation
Dr <Pad drill> Xoffset Yoffset
At <Pad type> N <layer mask>
Ne <netnumber> "net name"
Po X_pos Y_pos
$EndPAD
```

For oblong holes:
```
Dr <Pad drill.x> Xoffset Yoffset O <Pad drill.x> <Pad drill.y>
```

| Parameter | Options | Meaning |
|-----------|---------|---------|
| shape | C, R, O, T | Circle, Rectangular, Oblong, Trapeze |
| Pad type | STD, SMD, CONN, HOLE, MECA | Pad attribute |
| layer mask | hexadecimal | Layers where pad appears |
| Hole shape | O | O for Oblong |

**Example:**
```
$PAD
Sh "3" C 1500 1500 0 0 2700
Dr 600 0 0
At STD N 00E0FFFF
Ne 10 "TD0_1"
Po -1000 0
$EndPAD
```

### Module 3D Shapes ($SHAPE3D)

Describes 3D representations using VRML files built with Wings3D.

**Format:**
```
$SHAPE3D
Na "device/bornier_6.wrl"
Sc 1.000000 1.000000 1.000000
Of 0.000000 0.000000 0.000000
Ro 0.000000 0.000000 0.000000
$EndSHAPE3D
```

| Parameter | Meaning |
|-----------|---------|
| Na | Filename (default path: kicad/modules/packages3d/) |
| Sc | X Y Z scale factor |
| Of | X Y Z offset (move vector in 3D units of 0.1 inch) |
| Ro | X Y Z rotation in degrees |

**Important Notes:**
- Real shape unit is 0.1 inch (1 VRML unit = 0.1 inch = 2.54 mm)
- Coordinates relative to footprint
- Scale applied first, then move, then rotate
- If footprint is inverted (on copper side), 3D shape must also invert
- Multiple 3D shapes supported per footprint

## Graphic Items

### $DRAWSEGMENT

Draw segments include lines, circles, and arcs.

#### Line

**Format:**
```
$DRAWSEGMENT
Po 0 67500 39000 65500 39000 120
De 28 0 900 0 0
$EndDRAWSEGMENT
```

- shape = 0
- Po: Xstart Ystart Xend Yend width
- De: layer type angle timestamp status

#### Circle

**Format:**
```
$DRAWSEGMENT
Po 1 67500 39000 65500 39000 120
De 28 0 900 0 0
$EndDRAWSEGMENT
```

- shape = 1
- End is a point on the circle (if Xend or Yend is 0, other coordinate is radius)

#### Arc

**Format:**
```
$DRAWSEGMENT
Po 2 67500 39000 65500 39000 120
De 28 0 900 0 0
$EndDRAWSEGMENT
```

- shape = 2
- Start and end are arc endpoints
- angle is arc angle in 0.1 degree units
- Center coordinates computed from start, end, and angle
- Currently only 90-degree arcs supported

### $TEXTPCB

Text on copper or technical layers.

**Format:**
```
$TEXTPCB
Te "string"
Po 57250 35750 600 600 150 0
De 15 1 B98C Normal
$EndTEXTPCB
```

| Parameter | Meaning |
|-----------|---------|
| Te | Text content |
| Po | Xpos Ypos Xsize Ysize Width rotation |
| De | layer normal timestamp style |
| normal | 0=mirrored, 1=normal |
| style | Normal or Italic |

### $MIREPCB (Target/Cross)

**Format:** `Po 0 28 28000 51000 5000 150 00000000`

- shape: 0 or 1
- Xpos Ypos: Position
- size, width, timestamp

### $COTATION (Dimension)

Dimension annotations.

**Format:**
```
$COTATION
Ge 0 24 0
Te "4,5500''"
Po 50250 5791 600 800 170 0 1
Sb 0 27500 6501 73000 6501 150
Sd 0 73000 9000 73000 5081 150
Sg 0 27500 9000 27500 5081 150
S1 0 73000 6501 72557 6731 150
S2 0 73000 6501 72557 6271 150
S3 0 27500 6501 27943 6731 150
S4 0 27500 6501 27943 6271 150
$EndCOTATION
```

| Parameter | Meaning |
|-----------|---------|
| Ge | General shape layer timestamp (shape=0 currently) |
| Te | Dimension value text in inches or mm |
| Po | Text position: Xpos Ypos Xsize Ysize width orient normal |
| Sb | Segment coordinates (axis, arrows) |
| Sd | Segment coordinates |
| Sg | Segment coordinates |
| S1-S4 | Arrow coordinates |

## Track, Vias, and Zones Section

### $TRACK

Describes track segments and vias on copper layers. Each track/via has two-line description.

#### Track Segment

**Format:**
```
Po 0 Xstart Ystart Xend Yend width
De layer 0 netcode timestamp status
```

- shape = 0
- type = 0 for track segment

#### Via

**Format:**
```
Po 3 Xstart Ystart Xend Yend diameter
De layer 1 netcode timestamp status
```

| Component | Meaning |
|-----------|---------|
| shape | 0=reserved for future |
| X/Ystart, X/Yend | Position coordinates |
| width/diameter | Track width or via diameter |
| layer | For via: 4 LSBs=start layer, next 4 bits=end layer (e.g., F0 hex=layer 0 to 15) |
| type | 0=track, 1=via |
| Via type | 3=through, 2=blind, 1=buried |
| timestamp | Reserved (set to 0) |
| status | 0 for routing information |

**Example:**
```
$TRACK
Po 0 36750 37000 36550 37000 250
De 15 0 1 0 400
Po 0 39000 36750 38750 37000 250
De 15 0 1 0 0
Po 3 53500 27000 53500 27000 650
De 15 1 14 0 0
$EndTRACK
```

### $ZONE

Zone filling segments (similar to track section, no vias included).

**Format:**
```
$ZONE
Po 0 67100 33700 67100 38600 100
De 0 0 2 3EDDB09D 0
$EndZONE
```

### $CZONE_OUTLINE

Describes zone main outlines and filled area outlines (polygons). Includes pads-in-zone options and thermal relief parameters.

**Format:**
```
$CZONE_OUTLINE
ZInfo <timestamp> <netcode> "net name"
ZLayer <layer>
ZAux <corners count> <hatching option>
ZClearance <clearance> <pads option>
ZMinThickness <thickness>
ZOptions <fill mode> <arc approx> <antipad> <thermal stubs>
ZCorner <x> <y> <flag>
...
$POLYSCORNERS
<x> <y> <unk1> <unk2>
...
$endPOLYSCORNERS
$endCZONE_OUTLINE
```

| Parameter | Options | Meaning |
|-----------|---------|---------|
| ZLayer | 0=copper, 15=component, 1-14=inner | Layer number |
| ZAux | N, E, F | Hatching: N=none, E=edge, F=full |
| ZClearance | I, T, X | Pads option: I=in zone, T=thermal relief, X=not in zone |
| ZMinThickness | value | Zone minimum copper thickness |
| fill mode | 0, 1 | 0=solid polygons, 1=segments |
| arc approx | 16, 32 | Segments to approximate 360 deg arc |
| ZCorner | x y flag | Flag=1 marks outline end |

**Example:**
```
$CZONE_OUTLINE
ZInfo 478E3FC8 1 "/aux_sheet/INPUT"
ZLayer 0
ZAux 4 E
ZClearance 150 T
ZMinThickness 190
ZOptions 0 32 F 200 200
ZCorner 74750 51750 0
ZCorner 74750 13250 0
ZCorner 29750 13250 0
ZCorner 29750 51750 1
$POLYSCORNERS
74655 51655 0 0
74655 13345 0 0
$endPOLYSCORNERS
$endCZONE_OUTLINE
```

## File Termination

**$EndBOARD**

This tag terminates the entire board description and must be the last line of the file.
