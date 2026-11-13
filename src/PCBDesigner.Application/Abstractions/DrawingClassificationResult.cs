using PCBDesigner.Domain.Drawings;

namespace PCBDesigner.Application.Abstractions;

public sealed record DrawingClassificationResult(
    DrawingKind Kind,
    double Confidence,
    string Method,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> Warnings,
    int? WidthPixels = null,
    int? HeightPixels = null);
