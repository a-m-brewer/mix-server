namespace MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;

public class GetFolderQueryOptions
{
    public GetFolderQueryOptions()
    {
    }

    public GetFolderQueryOptions(GetChildFolderQueryOptions childFolder, GetFileQueryOptions childFiles)
    {
        ChildFolders = childFolder;
        ChildFiles = childFiles;
    }
    
    public GetChildFolderQueryOptions? ChildFolders { get; init; }
    
    public GetFileQueryOptions? ChildFiles { get; init; }
    
    public static GetFolderQueryOptions FolderOnly => new()
    {
        ChildFolders = null,
        ChildFiles = null
    };
    
    public static GetFolderQueryOptions FolderAndChildrenWithBasicMetadata => new()
    {
        ChildFolders = new GetChildFolderQueryOptions(),
        ChildFiles = GetFileQueryOptions.MetadataOnly
    };
    
    public static GetFolderQueryOptions Full => new()
    {
        ChildFolders = GetChildFolderQueryOptions.Full,
        ChildFiles = GetFileQueryOptions.Full
    };
}

public class GetChildFolderQueryOptions
{
    public static GetChildFolderQueryOptions Full => new();
}