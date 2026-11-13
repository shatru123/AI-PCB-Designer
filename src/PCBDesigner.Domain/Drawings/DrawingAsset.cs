namespace PCBDesigner.Domain.Drawings;

public sealed record DrawingAsset(
    Guid Id,
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAtUtc,
    DrawingKind Kind = DrawingKind.Unknown);
