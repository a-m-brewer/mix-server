namespace MixServer.Infrastructure.Files.Services;

public interface IMimeTypeService
{
    string GetMimeType(string filePath);
}