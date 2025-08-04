using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;

public class GetFolderQueryOptions
{
    public GetFolderQueryOptions(
        Page? page,
        IFolderSort? sort)
    {
        Page = page;
        Sort = sort ?? FolderSortModel.Default;
    }

    public Page? Page { get; set; }
    
    public IFolderSort Sort { get; set; }

    public GetChildFolderQueryOptions? ChildFolders { get; init; }
    
    public GetFileQueryOptions? ChildFiles { get; init; }
    
    public static GetFolderQueryOptions FolderOnly => new(null, null)
    {
        ChildFolders = null,
        ChildFiles = null
    };
    
    public static GetFolderQueryOptions Full(Page page, IFolderSort sort) => new(page, sort)
    {
        ChildFolders = GetChildFolderQueryOptions.Full,
        ChildFiles = GetFileQueryOptions.Full
    };
}

public class GetChildFolderQueryOptions
{
    public static GetChildFolderQueryOptions Full => new();
}