namespace PCBDesigner.Application.Drawings;

public interface IUploadDrawingService
{
    Task<UploadDrawingResult> UploadAsync(
        UploadDrawingRequest request,
        CancellationToken cancellationToken);
}
