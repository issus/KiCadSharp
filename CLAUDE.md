# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Git Commit Strategy

Commit after completing each logical unit of work, each bug fix, each feature implementation, each test addition, and final documentation. Do NOT reference AI/Claude/Anthropic in commit messages. Do not add Co-Authored-By lines. Use the default system git user.

Obey the .gitignore, do not commit files from the docs directory. Do not commit markdown files from reviews, planning, or working files. The only markdown files that should be committed are documents that are clearly documentation of the end project itself, not files you have generated as you've worked.

## Build Commands

```bash
# Build the entire solution
dotnet build OriginalCircuit.KiCad.sln

# Build only the core library
dotnet build src/OriginalCircuit.KiCad/OriginalCircuit.KiCad.csproj

# Run tests
dotnet test tests/OriginalCircuit.KiCad.Tests/

# Create NuGet packages
dotnet pack OriginalCircuit.KiCad.sln -c Release
```

## Project Structure

```
src/
  OriginalCircuit.KiCad/                   Core library (readers, writers, data models)
  OriginalCircuit.KiCad.Rendering/         KiCad-specific rendering (raster + SVG)
shared/
  OriginalCircuit.Eda.Abstractions/        Shared EDA interfaces, primitives, enums (git submodule)
  OriginalCircuit.Eda.Rendering/           Shared rendering core, raster, SVG (git submodule)
tests/
  OriginalCircuit.KiCad.Tests/             Unit, integration, and round-trip tests
examples/
  CreateFiles/   LoadFiles/   ModifyFiles/   RenderFiles/
```

All projects target `net10.0`.

## Architecture Overview

OriginalCircuit.KiCad reads and writes KiCad 6.0+ files (.kicad_sym, .kicad_sch, .kicad_mod, .kicad_pcb) which use an **S-Expression** text format.

### Core Abstractions

**S-Expression Layer:**
- `SExpression` — Parsed tree representation of KiCad's S-expression format
- `SExpressionReader` / `SExpressionWriter` — Low-level S-expression parsing and serialization
- `SExpressionBuilder` — Programmatic construction of S-expression trees

**Reader/Writer Classes:**
- All readers/writers are fully async (`ReadAsync`/`WriteAsync`) with `CancellationToken` support
- Support both file path and `Stream`-based I/O

**Data Model Hierarchy (in shared Eda.Abstractions):**
- Common interfaces: `IContainer`, `IComponent` for objects containing primitives
- `Coord`, `CoordPoint`, `CoordRect` — Coordinate system primitives

**File Type Classes:**

| File Type | Data Class | Reader | Writer |
|-----------|------------|--------|--------|
| .kicad_sym | `KiCadSymLib` | `SymLibReader` | `SymLibWriter` |
| .kicad_sch | `KiCadSch` | `SchReader` | `SchWriter` |
| .kicad_mod | (Footprint) | `FootprintReader` | `FootprintWriter` |
| .kicad_pcb | `KiCadPcb` | `PcbReader` | `PcbWriter` |

### Key Namespaces

- `OriginalCircuit.KiCad` — Root namespace, exceptions, diagnostics
- `OriginalCircuit.KiCad.Models.Sch` — Schematic data models (symbols, wires, junctions, etc.)
- `OriginalCircuit.KiCad.Models.Pcb` — PCB data models (pads, tracks, vias, regions, etc.)
- `OriginalCircuit.KiCad.Serialization` — Readers and writers for all file formats
- `OriginalCircuit.KiCad.SExpression` — S-expression parsing infrastructure
- `OriginalCircuit.KiCad.Rendering` — Raster and SVG rendering implementations

### Rendering

Rendering is split between shared abstractions and KiCad-specific implementations:
- `OriginalCircuit.Eda.Rendering.Core` — Base rendering interfaces (`IRenderContext`, `IRenderer`)
- `OriginalCircuit.Eda.Rendering.Raster` — SkiaSharp-based PNG/JPG output (cross-platform)
- `OriginalCircuit.Eda.Rendering.Svg` — SVG output via XElement (no native dependencies)
- `OriginalCircuit.KiCad.Rendering` — KiCad-specific renderers, layer colors, coordinate mapping

### Error Handling

- `KiCadFileException` — Base exception for file parsing/writing errors
- `KiCadDiagnostic` with `DiagnosticSeverity` enum for non-fatal issues
- All models have `Diagnostics` property for parser warnings

## Dependencies

- **SkiaSharp** — Cross-platform raster rendering (Rendering package only)
- **xunit + FluentAssertions** — Test framework

## NuGet Packages

Shared metadata is centralized in `Directory.Build.props`. Version is `1.0.0-alpha.1`.
Centralized package versions in `Directory.Packages.props`.

## Shared Submodules

The `shared/` directory contains git submodules shared across EDA projects (AltiumSharp, KiCadSharp, etc.):
- `OriginalCircuit.Eda.Abstractions` — Common EDA interfaces, models, and primitives
- `OriginalCircuit.Eda.Rendering` — Common rendering infrastructure (Core, Raster, Svg)

## Test Data

Real-world KiCad projects for integration testing are in `tests/OriginalCircuit.KiCad.Tests/TestData/RealWorld/` including projects like KiCadDemo_JetsonBaseboard, SparkFun_Libraries, and KiCad_OfficialLibraries.

## Raw S-Expression Caching Policy

Do NOT cache raw S-expression subtrees from the original file as a shortcut for round-trip fidelity. Every feature that the library reads must be deserialized into typed model properties and re-serialized from those properties by the writer. Storing raw `SExpression` objects on model classes (e.g., `*Raw` properties, `SourceTree`, `RenderCache`, `RawNode`) is not permitted. Features that lack typed models should be tracked as gaps, not papered over with passthrough caching.

## File Format Documentation

KiCad S-expression file format documentation is in `docs/kicad-file-formats/` covering symbol libraries, schematics, PCBs, footprints, and legacy formats.
