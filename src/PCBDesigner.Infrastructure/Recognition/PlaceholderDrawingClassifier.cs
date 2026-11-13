using PCBDesigner.Application.Abstractions;
using PCBDesigner.Domain.Drawings;

namespace PCBDesigner.Infrastructure.Recognition;

/// <summary>
/// Deliberate placeholder. Replace with preprocessing and vision classification.
/// It must not guess drawing type from a filename or claim electrical recognition.
/// </summary>
public sealed class PlaceholderDrawingClassifier : IDrawingClassifier
{
    public Task<DrawingKind> ClassifyAsync(
        Stream image,
        CancellationToken cancellationToken) =>
        Task.FromResult(DrawingKind.Unknown);
}
