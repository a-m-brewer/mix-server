using Microsoft.AspNetCore.StaticFiles;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Infrastructure.Files.Constants;

namespace MixServer.Infrastructure.Files.Services;

public class MimeTypeService : IMimeTypeService
{
    private readonly FileExtensionContentTypeProvider _fileExtensionContentTypeProvider;

    public MimeTypeService(FileExtensionContentTypeProvider fileExtensionContentTypeProvider)
    {
        _fileExtensionContentTypeProvider = fileExtensionContentTypeProvider;
    }
    
    public string GetMimeType(string filePath)
    {
        return _fileExtensionContentTypeProvider.TryGetContentType(filePath, out var contentType)
            ? contentType
            : MimeTypeConstants.DefaultMimeType;
    }
}