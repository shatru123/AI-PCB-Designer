namespace PCBDesigner.Application.Drawings;

public sealed record UploadDrawingRequest(
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    Stream Content);
