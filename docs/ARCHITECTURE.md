# Architecture Decisions

## ADR-001: KiCad is the engineering source of truth
AI output is an intermediate interpretation. KiCad-native schematic and board files plus CLI validation/export are authoritative.

## ADR-002: Separate recognition paths
Schematic interpretation extracts symbols, pins and nets. PCB drawing interpretation extracts board geometry, pads, tracks and component placement. A PCB photo cannot be treated as a circuit schematic.

## ADR-003: Human-in-the-loop
Expose confidence and provenance for recognized entities. Ambiguous junctions, pin numbering, polarity and footprints block fabrication until reviewed.

## ADR-004: Export gating
Printable artwork and Gerber generation consume a versioned, validated board revision. Exports record tool version, board revision, units, layers and mirror settings.

## Current implementation status
Only the API upload foundation exists. Classification is a placeholder; AI recognition, KiCad generation, routing, DRC and Gerber export are not yet implemented.
