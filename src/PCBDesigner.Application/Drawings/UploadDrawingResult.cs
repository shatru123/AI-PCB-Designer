using PCBDesigner.Application.Abstractions;

namespace PCBDesigner.Application.Drawings;

public sealed record UploadDrawingResult(
    Guid Id,
    string OriginalFileName,
    long SizeBytes,
    DrawingClassificationResult Analysis,
    string Status);
