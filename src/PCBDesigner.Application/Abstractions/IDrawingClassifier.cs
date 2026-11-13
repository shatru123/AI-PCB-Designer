namespace PCBDesigner.Application.Abstractions;

public interface IDrawingClassifier
{
    Task<DrawingClassificationResult> ClassifyAsync(
        Stream drawing,
        string fileExtension,
        CancellationToken cancellationToken);
}
