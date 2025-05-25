using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Services;

public interface IMimeTypeService
{
    string GetMimeType(NodePath path);
}