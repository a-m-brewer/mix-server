namespace MixServer.Domain.FileExplorer.Services;

public interface IMimeTypeService
{
    string GetMimeType(string filePath);
}