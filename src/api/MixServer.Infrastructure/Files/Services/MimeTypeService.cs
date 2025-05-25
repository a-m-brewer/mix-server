using Microsoft.AspNetCore.StaticFiles;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Infrastructure.Files.Constants;

namespace MixServer.Infrastructure.Files.Services;

public class MimeTypeService(FileExtensionContentTypeProvider fileExtensionContentTypeProvider)
    : IMimeTypeService
{
    public string GetMimeType(NodePath nodePath)
    {
        if (fileExtensionContentTypeProvider.TryGetContentType(nodePath.AbsolutePath, out var contentType))
        {
            return contentType;
        }

        return nodePath.Extension switch
        {
            ".flac" => MimeTypeConstants.AudioFlac,
            ".ogx" => MimeTypeConstants.ApplicationOgg,
            ".ogv" => MimeTypeConstants.VideoOgg,
            ".oga" or ".ogg" or ".opus" or ".spx" => MimeTypeConstants.AudioOgg,
            _ => MimeTypeConstants.DefaultMimeType
        };
    }
}