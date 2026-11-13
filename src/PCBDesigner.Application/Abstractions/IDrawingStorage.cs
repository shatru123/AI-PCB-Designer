namespace PCBDesigner.Application.Abstractions;

public interface IDrawingStorage
{
    Task<string> SaveAsync(
        Guid drawingId,
        string safeExtension,
        Stream content,
        CancellationToken cancellationToken);
}
