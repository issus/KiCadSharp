# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-alpha.1] - Unreleased

### Added

- S-expression parser and writer for KiCad's text-based file format
- Read and write support for symbol libraries (`.kicad_sym`)
- Read and write support for schematic documents (`.kicad_sch`)
- Read and write support for footprints (`.kicad_mod`)
- Read and write support for PCB layouts (`.kicad_pcb`)
- Cross-platform raster rendering (PNG/JPG) via SkiaSharp
- SVG rendering with no native dependencies
- Fully async API with `CancellationToken` support
- Stream-based and file path-based I/O
- Shared `Eda.Abstractions` for common EDA interfaces and coordinate primitives
- Diagnostic system for non-fatal parser warnings
- Example projects for creating, loading, modifying, and rendering KiCad files
