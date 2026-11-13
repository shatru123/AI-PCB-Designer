using PCBDesigner.Application.Abstractions;

namespace PCBDesigner.Application.Drawings;

public sealed class UploadDrawingService(
    IDrawingStorage storage,
    IDrawingClassifier classifier) : IUploadDrawingService
{
    public async Task<UploadDrawingResult> UploadAsync(
        UploadDrawingRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var extension = Path.GetExtension(request.OriginalFileName).ToLowerInvariant();

        await using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var analysis = await classifier.ClassifyAsync(buffer, extension, cancellationToken);
        buffer.Position = 0;

        await storage.SaveAsync(id, extension, buffer, cancellationToken);

        var status = analysis.Kind == PCBDesigner.Domain.Drawings.DrawingKind.Unknown
            ? "Uploaded; drawing type requires manual review."
            : $"Uploaded; probable drawing type: {analysis.Kind}. Human verification required.";

        return new UploadDrawingResult(
            id, Path.GetFileName(request.OriginalFileName), request.SizeBytes, analysis, status);
    }
}
