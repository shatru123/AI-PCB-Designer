using PCBDesigner.Application.Abstractions;
using PCBDesigner.Domain.Drawings;

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

        var kind = await classifier.ClassifyAsync(buffer, cancellationToken);
        buffer.Position = 0;

        await storage.SaveAsync(id, extension, buffer, cancellationToken);

        return new UploadDrawingResult(
            id,
            Path.GetFileName(request.OriginalFileName),
            request.SizeBytes,
            kind,
            $"Uploaded; classification adapter returned {kind}; recognition pending.");
    }
}
