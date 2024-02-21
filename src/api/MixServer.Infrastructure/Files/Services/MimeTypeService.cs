using Microsoft.AspNetCore.StaticFiles;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Infrastructure.Files.Constants;

namespace MixServer.Infrastructure.Files.Services;

public class MimeTypeService(FileExtensionContentTypeProvider fileExtensionContentTypeProvider)
    : IMimeTypeService
{
    public string GetMimeType(string filePath)
    {
        return fileExtensionContentTypeProvider.TryGetContentType(filePath, out var contentType)
            ? contentType
            : MimeTypeConstants.DefaultMimeType;
    }
}