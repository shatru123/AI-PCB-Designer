using PCBDesigner.Domain.Drawings;

namespace PCBDesigner.Application.Drawings;

public sealed record UploadDrawingResult(
    Guid Id,
    string OriginalFileName,
    long SizeBytes,
    DrawingKind DetectedKind,
    string Status);
