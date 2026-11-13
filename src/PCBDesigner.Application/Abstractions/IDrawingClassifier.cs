using PCBDesigner.Domain.Drawings;

namespace PCBDesigner.Application.Abstractions;

public interface IDrawingClassifier
{
    Task<DrawingKind> ClassifyAsync(
        Stream image,
        CancellationToken cancellationToken);
}
