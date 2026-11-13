using PCBDesigner.Application.Abstractions;

namespace PCBDesigner.Infrastructure.Storage;

public sealed class LocalDrawingStorage(string rootPath) : IDrawingStorage
{
    public async Task<string> SaveAsync(
        Guid drawingId,
        string safeExtension,
        Stream content,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(rootPath);
        var fileName = $"{drawingId:N}{safeExtension}";
        var fullPath = Path.Combine(rootPath, fileName);

        await using var output = new FileStream(
            fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true);

        await content.CopyToAsync(output, cancellationToken);
        return fileName;
    }
}
