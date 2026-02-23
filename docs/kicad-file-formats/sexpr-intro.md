# KiCad S-Expression Format Documentation

Source: https://dev-docs.kicad.org/en/file-formats/sexpr-intro/index.html

## Introduction

KiCad employs an s-expression file format across symbol libraries, footprint libraries, schematics, PCBs, and worksheets. The format is based on the Specctra DSN specification.

### Syntax Fundamentals

The core syntax rules include:

- Tokens are delimited by parentheses `()` and use lowercase exclusively
- Tokens cannot contain whitespace or special characters except underscores
- All strings use double quotes and UTF-8 encoding
- Tokens may have zero or more attributes
- Human readability is a primary design goal

### Notation Conventions

Documentation uses specific conventions:

- Token attributes appear in uppercase (e.g., `(at X Y)`)
- Limited value options are separated by pipes: `(visible yes|no)`
- Optional attributes appear in square brackets: `(paper A0 [portrait])`

### Coordinates and Sizes

Values are expressed in millimeters with no exponential notation. All coordinates are relative to their containing object's origin.

## Common Syntax Elements

### Library Identifier

Libraries use the format `"LIBRARY_NICKNAME:ENTRY_NAME"`, though library files only store the entry name since the nickname is assigned via the library table.

### Position Identifier

The `at` token defines position and rotation:

```
(at X Y [ANGLE])
```

Symbol text angles use tenths of degrees; all others use degrees.

### Coordinate Point Lists

The `pts` token groups X/Y pairs:

```
(pts
  (xy X Y)
  ...
)
```

### Stroke Definition

Line appearance is controlled by:

```
(stroke
  (width WIDTH)
  (type TYPE)
  (color R G B A)
)
```

Valid line styles include `dash`, `dash_dot`, `dash_dot_dot`, `dot`, `default`, and `solid`.

### Text Effects

Text display is configured via:

```
(effects
  (font [(face FACE_NAME)] (size HEIGHT WIDTH) [(thickness THICKNESS)])
  [(justify [left|right] [top|bottom] [mirror])]
  [hide]
)
```

Features include optional boldface, italics, and line spacing control.

### Page Settings

The `paper` token specifies page dimensions:

```
(paper PAPER_SIZE | WIDTH HEIGHT [portrait])
```

Valid sizes are A0-A5, A, B, C, D, E, or custom dimensions.

### Title Block

The `title_block` token contains document metadata:

```
(title_block
  (title "TITLE")
  (date "DATE")
  (rev "REVISION")
  (company "COMPANY_NAME")
  (comment N "COMMENT")
)
```

Date format is YYYY-MM-DD; comments use numbers 1-9.

### Properties

Key-value pairs store custom information:

```
(property "KEY" "VALUE")
```

### Universally Unique Identifier

The `uuid` token holds Version 4 random UUIDs generated via the Mersenne Twister algorithm.

### Images

Embedded images use PNG format with Base64 encoding:

```
(image
  POSITION_IDENTIFIER
  [(scale SCALAR)]
  [(layer LAYER_DEFINITIONS)]
  UNIQUE_IDENTIFIER
  (data IMAGE_DATA)
)
```

## Board-Specific Syntax

### Board Coordinates

PCB and footprint files maintain nanometer resolution (six decimal places maximum; 0.000001 mm).

### Layers

Boards support 60 total layers including 32 copper layers, 8 technical layer pairs, 4 user layers, board outline, margins, and 9 optional user layers.

#### Canonical Layer Names

Copper layers:
- `F.Cu` (Front) and `B.Cu` (Back)
- `In1.Cu` through `In30.Cu` (Inner layers 1-30)

Technical layers:
- `F.Adhes`, `B.Adhes` (Adhesive)
- `F.Paste`, `B.Paste` (Solder paste)
- `F.SilkS`, `B.SilkS` (Silk screen)
- `F.Mask`, `B.Mask` (Solder mask)

Special layers:
- `Edge.Cuts` (Board outline)
- `F.CrtYd`, `B.CrtYd` (Courtyard)
- `F.Fab`, `B.Fab` (Fabrication)
- `Dwgs.User`, `Cmts.User` (User drawings/comments)
- `Eco1.User`, `Eco2.User` (ECO)
- `User.1` through `User.9` (Definable)

### Footprint Definition

The `footprint` token describes component placements:

```
(footprint
  ["LIBRARY_LINK"]
  [locked]
  [placed]
  (layer LAYER_DEFINITIONS)
  (tedit TIME_STAMP)
  [(uuid UUID)]
  [POSITION_IDENTIFIER]
  [(descr "DESCRIPTION")]
  [(tags "NAME")]
  [(property "KEY" "VALUE") ...]
  (path "PATH")
  [(autoplace_cost90 COST)]
  [(autoplace_cost180 COST)]
  [(solder_mask_margin MARGIN)]
  [(solder_paste_margin MARGIN)]
  [(solder_paste_ratio RATIO)]
  [(clearance CLEARANCE)]
  [(zone_connect CONNECTION_TYPE)]
  [(thermal_width WIDTH)]
  [(thermal_gap DISTANCE)]
  [ATTRIBUTES]
  [(private_layers LAYER_DEFINITIONS)]
  [(net_tie_pad_groups PAD_GROUP_DEFINITIONS)]
  GRAPHIC_ITEMS...
  PADS...
  ZONES...
  GROUPS...
  3D_MODEL
)
```

#### Footprint Attributes

The `attr` token specifies footprint type and behavior:

```
(attr TYPE [board_only] [exclude_from_pos_files] [exclude_from_bom])
```

Valid types: `smd` and `through_hole`.

#### Footprint Graphics Items

Footprint drawing elements use `fp_` prefix (valid only within footprints):
- `fp_text` - Text elements with types `reference`, `value`, `user`
- `fp_text_box` - Wrapped text boxes
- `fp_line` - Lines
- `fp_rect` - Rectangles
- `fp_circle` - Circles
- `fp_arc` - Arcs
- `fp_poly` - Polygons
- `fp_curve` - Cubic Bezier curves

#### Footprint Text

```
(fp_text
  TYPE
  "TEXT"
  POSITION_IDENTIFIER
  [unlocked]
  (layer LAYER_DEFINITION)
  [hide]
  (effects TEXT_EFFECTS)
  (uuid UUID)
)
```

#### Footprint Text Box

```
(fp_text_box
  [locked]
  "TEXT"
  [(start X Y)]
  [(end X Y)]
  [(pts (xy X Y) (xy X Y) (xy X Y) (xy X Y))]
  [(angle ROTATION)]
  (layer LAYER_DEFINITION)
  (uuid UUID)
  TEXT_EFFECTS
  [STROKE_DEFINITION]
  [(render_cache RENDER_CACHE)]
)
```

Cardinals angles (0, 90, 180, 270) require `start`/`end`; non-cardinal angles require `pts` with 4 points.

#### Footprint Line

```
(fp_line
  (start X Y)
  (end X Y)
  (layer LAYER_DEFINITION)
  (width WIDTH)
  STROKE_DEFINITION
  [(locked)]
  (uuid UUID)
)
```

#### Footprint Rectangle

```
(fp_rect
  (start X Y)
  (end X Y)
  (layer LAYER_DEFINITION)
  (width WIDTH)
  STROKE_DEFINITION
  [(fill yes | no)]
  [(locked)]
  (uuid UUID)
)
```

#### Footprint Circle

```
(fp_circle
  (center X Y)
  (end X Y)
  (layer LAYER_DEFINITION)
  (width WIDTH)
  STROKE_DEFINITION
  [(fill yes | no)]
  [(locked)]
  (uuid UUID)
)
```

#### Footprint Arc

```
(fp_arc
  (start X Y)
  (mid X Y)
  (end X Y)
  (layer LAYER_DEFINITION)
  (width WIDTH)
  STROKE_DEFINITION
  [(locked)]
  (uuid UUID)
)
```

#### Footprint Polygon

```
(fp_poly
  COORDINATE_POINT_LIST
  (layer LAYER_DEFINITION)
  (width WIDTH)
  STROKE_DEFINITION
  [(fill yes | no)]
  [(locked)]
  (uuid UUID)
)
```

#### Footprint Curve

```
(fp_curve
  COORDINATE_POINT_LIST
  (layer LAYER_DEFINITION)
  (width WIDTH)
  STROKE_DEFINITION
  [(locked)]
  (uuid UUID)
)
```

#### Footprint Pad

```
(pad
  "NUMBER"
  TYPE
  SHAPE
  POSITION_IDENTIFIER
  [(locked)]
  (size X Y)
  [(drill DRILL_DEFINITION)]
  (layers "CANONICAL_LAYER_LIST")
  [(property PROPERTY)]
  [(remove_unused_layer)]
  [(keep_end_layers)]
  [(roundrect_rratio RATIO)]
  [(chamfer_ratio RATIO)]
  [(chamfer CORNER_LIST)]
  (net NUMBER "NAME")
  (uuid UUID)
  [(pinfunction "PIN_FUNCTION")]
  [(pintype "PIN_TYPE")]
  [(die_length LENGTH)]
  [(solder_mask_margin MARGIN)]
  [(solder_paste_margin MARGIN)]
  [(solder_paste_margin_ratio RATIO)]
  [(clearance CLEARANCE)]
  [(zone_connect ZONE)]
  [(thermal_width WIDTH)]
  [(thermal_gap DISTANCE)]
  [CUSTOM_PAD_OPTIONS]
  [CUSTOM_PAD_PRIMITIVES]
)
```

Pad types: `thru_hole`, `smd`, `connect`, `np_thru_hole`
Pad shapes: `circle`, `rect`, `oval`, `trapezoid`, `roundrect`, `custom`

Properties: `pad_prop_bga`, `pad_prop_fiducial_glob`, `pad_prop_fiducial_loc`, `pad_prop_testpoint`, `pad_prop_heatsink`, `pad_prop_castellated`

Zone connection types (0-3):
- 0: Not connected
- 1: Thermal relief
- 2: Solid fill

##### Pad Drill Definition

```
(drill
  [oval]
  DIAMETER
  [WIDTH]
  [(offset X Y)]
)
```

##### Custom Pad Options

```
(options
  (clearance CLEARANCE_TYPE)
  (anchor PAD_SHAPE)
)
```

Clearance types: `outline`, `convexhull`
Anchor shapes: `rect`, `circle`

##### Custom Pad Primitives

```
(primitives
  GRAPHIC_ITEMS...
  (width WIDTH)
  [(fill yes)]
)
```

#### Footprint 3D Model

```
(model
  "3D_MODEL_FILE"
  (at (xyz X Y Z))
  (scale (xyz X Y Z))
  (rotate (xyz X Y Z))
)
```

### Graphic Items (General Board Items)

#### Graphical Text

```
(gr_text
  "TEXT"
  POSITION_INDENTIFIER
  (layer LAYER_DEFINITION [knockout])
  (uuid UUID)
  (effects TEXT_EFFECTS)
)
```

#### Graphical Text Box

```
(gr_text_box
  [locked]
  "TEXT"
  [(start X Y)]
  [(end X Y)]
  [(pts (xy X Y) (xy X Y) (xy X Y) (xy X Y))]
  [(angle ROTATION)]
  (layer LAYER_DEFINITION)
  (uuid UUID)
  TEXT_EFFECTS
  [STROKE_DEFINITION]
  [(render_cache RENDER_CACHE)]
)
```

Same cardinal angle rules as footprint text box apply.

#### Graphical Line

```
(gr_line
  (start X Y)
  (end X Y)
  [(angle ANGLE)]
  (layer LAYER_DEFINITION)
  (width WIDTH)
  (uuid UUID)
)
```

#### Graphical Rectangle

```
(gr_rect
  (start X Y)
  (end X Y)
  (layer LAYER_DEFINITION)
  (width WIDTH)
  [(fill yes | no)]
  (uuid UUID)
)
```

#### Graphical Circle

```
(gr_circle
  (center X Y)
  (end X Y)
  (layer LAYER_DEFINITION)
  (width WIDTH)
  [(fill yes | no)]
  (uuid UUID)
)
```

#### Graphical Arc

```
(gr_arc
  (start X Y)
  (mid X Y)
  (end X Y)
  (layer LAYER_DEFINITION)
  (width WIDTH)
  (uuid UUID)
)
```

#### Graphical Polygon

```
(gr_poly
  COORDINATE_POINT_LIST
  (layer LAYER_DEFINITION)
  (width WIDTH)
  [(fill yes | no)]
  (uuid UUID)
)
```

#### Graphical Curve (Bezier)

```
(bezier
  COORDINATE_POINT_LIST
  (layer LAYER_DEFINITION)
  (width WIDTH)
  (uuid UUID)
)
```

#### Annotation Bounding Box

```
(gr_bbox
  (start X Y)
  (end X Y)
)
```

### Dimensions

The `dimension` token defines measurement annotations:

```
(dimension
  [locked]
  (type DIMENSION_TYPE)
  (layer LAYER_DEFINITION)
  (uuid UUID)
  (pts (xy X Y) (xy X Y))
  [(height HEIGHT)]
  [(orientation ORIENTATION)]
  [(leader_length LEADER_LENGTH)]
  [(gr_text GRAPHICAL_TEXT)]
  [(format DIMENSION_FORMAT)]
  (style DIMENSION_STYLE)
)
```

Dimension types: `aligned`, `leader`, `center`, `orthogonal`, `radial`

#### Dimension Format

```
(format
  [(prefix "PREFIX")]
  [(suffix "SUFFIX")]
  (units UNITS)
  (units_format UNITS_FORMAT)
  (precision PRECISION)
  [(override_value "VALUE")]
  [(suppress_zeros yes | no)]
)
```

Units (0-3): Inches, Mils, Millimeters, Automatic
Units format (0-2): No suffix, Bare, Parenthesized
Precision 6+: Units-scaled precision values

#### Dimension Style

```
(style
  (thickness THICKNESS)
  (arrow_length LENGTH)
  (text_position_mode MODE)
  [(arrow_direction DIRECTION)]
  [(extension_height HEIGHT)]
  [(text_frame TEXT_FRAME_TYPE)]
  [(extension_offset OFFSET)]
  [(keep_text_aligned yes | no)]
)
```

Position modes (0-2): Outside, In-line, Manual
Arrow directions: `outward`, `inward`
Text frames (0-3): None, Rectangle, Circle, Rounded rectangle

### Zones

The `zone` token defines filled copper or keep-out areas:

```
(zone
  (net NET_NUMBER)
  (net_name "NET_NAME")
  (layer LAYER_DEFINITION)
  (uuid UUID)
  [(name "NAME")]
  (hatch STYLE PITCH)
  [(priority PRIORITY)]
  (connect_pads [CONNECTION_TYPE] (clearance CLEARANCE))
  (min_thickness THICKNESS)
  [(filled_areas_thickness no)]
  [ZONE_KEEPOUT_SETTINGS]
  ZONE_FILL_SETTINGS
  (polygon COORDINATE_POINT_LIST)
  [ZONE_FILL_POLYGONS...]
  [ZONE_FILL_SEGMENTS...]
)
```

Hatch styles: `none`, `edge`, `full`
Pad connection types: `thru_hole_only`, `full`, `no` (default: thermal relief)

#### Zone Keep Out Settings

```
(keepout
  (tracks KEEPOUT)
  (vias KEEPOUT)
  (pads KEEPOUT)
  (copperpour KEEPOUT)
  (footprints KEEPOUT)
)
```

All values: `allowed` or `not_allowed`

#### Zone Fill Settings

```
(fill
  [yes]
  [(mode FILL_MODE)]
  (thermal_gap GAP)
  (thermal_bridge_width WIDTH)
  [(smoothing STYLE)]
  [(radius RADIUS)]
  [(island_removal_mode MODE)]
  [(island_area_min AREA)]
  [(hatch_thickness THICKNESS)]
  [(hatch_gap GAP)]
  [(hatch_orientation ORIENTATION)]
  [(hatch_smoothing_level LEVEL)]
  [(hatch_smoothing_value VALUE)]
  [(hatch_border_algorithm TYPE)]
  [(hatch_min_hole_area AREA)]
)
```

Fill mode: `hatched` (default: solid)
Smoothing: `chamfer`, `fillet`
Island removal modes (0-2): Always remove, Never remove, Minimum area
Hatch smoothing (0-3): None, Fillet, Arc min, Arc max
Border algorithm (0-1): Zone thickness, Hatch thickness

#### Zone Fill Polygons

```
(filled_polygon
  (layer LAYER_DEFINITION)
  COORDINATE_POINT_LIST
)
```

#### Zone Fill Segments

```
(fill_segments
  (layer LAYER_DEFINITION)
  COORDINATED_POINT_LIST
)
```

Used only for legacy board imports; modern boards use polygons.

### Groups

The `group` token organizes related objects:

```
(group
  "NAME"
  (id UUID)
  (members UUID1 ... UUIDN)
)
```

## Schematic and Symbol Library Syntax

### Schematic Coordinates

Schematic and symbol library files maintain nanometer resolution (four decimal places maximum; 0.0001 mm).

### Symbol Unit Identifier

Units use format `"NAME_UNIT_STYLE"` where UNIT is an integer (0 = all units) and STYLE is 1 or 2.

### Fill Definition

The `fill` token controls graphical object filling:

```
(fill (type none | outline | background))
```

Fill modes:
- `none` - No fill
- `outline` - Filled with line color
- `background` - Filled with theme background

### Symbols

The `symbol` token defines components:

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

Extended symbols derive from parent symbols and modify only properties.

#### Symbol Properties

```
(property
  "KEY"
  "VALUE"
  (id N)
  POSITION_IDENTIFIER
  TEXT_EFFECTS
)
```

Mandatory properties for parent symbols:
- `Reference` - Designator
- `Value` - Component value
- `Footprint` - Library identifier
- `Datasheet` - Link

Reserved keys cannot be user-defined: `ki_keywords`, `ki_description`, `ki_locked`, `ki_fp_filters`

#### Symbol Graphic Items

##### Symbol Arc

```
(arc
  (start X Y)
  (mid X Y)
  (end X Y)
  STROKE_DEFINITION
  FILL_DEFINITION
)
```

##### Symbol Circle

```
(circle
  (center X Y)
  (radius RADIUS)
  STROKE_DEFINITION
  FILL_DEFINITION
)
```

##### Symbol Curve (Bezier)

```
(bezier
  COORDINATE_POINT_LIST
  STROKE_DEFINITION
  FILL_DEFINITION
)
```

##### Symbol Line (Polyline)

```
(polyline
  COORDINATE_POINT_LIST
  STROKE_DEFINITION
  FILL_DEFINITION
)
```

Minimum two points required.

##### Symbol Rectangle

```
(rectangle
  (start X Y)
  (end X Y)
  STROKE_DEFINITION
  FILL_DEFINITION
)
```

##### Symbol Text

```
(text
  "TEXT"
  POSITION_IDENTIFIER
  (effects TEXT_EFFECTS)
)
```

#### Symbol Pin

```
(pin
  PIN_ELECTRICAL_TYPE
  PIN_GRAPHIC_STYLE
  POSITION_IDENTIFIER
  (length LENGTH)
  (name "NAME" TEXT_EFFECTS)
  (number "NUMBER" TEXT_EFFECTS)
)
```

Supported pin angles: 0, 90, 180, 270 degrees

##### Pin Electrical Types

- `input` - Input pin
- `output` - Output pin
- `bidirectional` - Bidirectional
- `tri_state` - Tri-state output
- `passive` - Electrically passive
- `free` - Not internally connected
- `unspecified` - No specified type
- `power_in` - Power input
- `power_out` - Power output
- `open_collector` - Open collector output
- `open_emitter` - Open emitter output
- `no_connect` - No electrical connection

##### Pin Graphic Styles

- `line` - Simple line
- `inverted` - Inverted bubble
- `clock` - Clock signal
- `inverted_clock` - Inverted clock
- `input_low` - Active-low input
- `clock_low` - Active-low clock
- `output_low` - Active-low output
- `edge_clock_high` - Falling-edge clock
- `non_logic` - Non-logic pin
