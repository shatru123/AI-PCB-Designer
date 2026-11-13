# Product Requirements (MVP)

## Personas
- DIY electronics maker converting paper schematics into a PCB.
- Electronics student preparing a simple board.
- Engineer digitizing an existing PCB sketch.

## Input modes
1. Circuit schematic: symbols, values, pins, wires, junctions, power rails.
2. Existing PCB drawing: board outline, component locations, pads, tracks and dimensions.

These modes require separate recognition pipelines and must not be conflated.

## Workflow
Upload -> classify -> preprocess -> recognize -> review -> schematic/netlist -> footprint assignment -> PCB placement/routing -> ERC/DRC -> export.

## Required outputs
- Printable copper artwork at exact 1:1 scale with explicit mirror setting.
- PDF, SVG and PNG.
- Gerber copper, mask, silkscreen and edge layers as applicable.
- Excellon drill.
- BOM and manufacturing report.

## Safety gates
- Uncertain nets require human confirmation.
- Footprints require package/pinout verification.
- No manufacturing-ready status until applicable ERC, DRC and fabrication checks pass.
- Users explicitly choose board stackup, copper side, units and print mirroring.
