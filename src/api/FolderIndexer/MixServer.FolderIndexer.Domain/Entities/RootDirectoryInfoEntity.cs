using MixServer.FolderIndexer.Interface.Models;

namespace MixServer.FolderIndexer.Domain.Entities;

public class RootDirectoryInfoEntity : DirectoryInfoEntity, IRootDirectoryInfo
{
    public override bool IsRoot => true;
}