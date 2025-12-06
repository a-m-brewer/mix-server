using MixServer.Domain.FileExplorer.Models;
using Range = MixServer.Domain.FileExplorer.Models.Range;

namespace MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;

public class GetFolderQueryOptions
{
    private GetFolderQueryOptions(
        Page? page,
        IFolderSort? sort)
    {
        Page = page;
        Sort = sort ?? FolderSortModel.Default;
    }
    
    private GetFolderQueryOptions(
        Range? range,
        IFolderSort? sort)
    {
        Range = range;
        Sort = sort ?? FolderSortModel.Default;
    }
    
    private GetFolderQueryOptions()
    {
        Sort = FolderSortModel.Default;
    }

    public Page? Page { get; set; }

    public Range? Range { get; set; }
    
    public IFolderSort Sort { get; set; }

    public GetChildFolderQueryOptions? ChildFolders { get; init; }
    
    public GetFileQueryOptions? ChildFiles { get; init; }
    
    public static GetFolderQueryOptions FolderOnly => new()
    {
        ChildFolders = null,
        ChildFiles = null
    };
    
    public static GetFolderQueryOptions Full(Page page, IFolderSort sort) => new(page, sort)
    {
        ChildFolders = GetChildFolderQueryOptions.Full,
        ChildFiles = GetFileQueryOptions.Full
    };
    
    public static GetFolderQueryOptions Full(Range range, IFolderSort sort) => new(range, sort)
    {
        ChildFolders = GetChildFolderQueryOptions.Full,
        ChildFiles = GetFileQueryOptions.Full
    };
}

public class GetChildFolderQueryOptions
{
    public static GetChildFolderQueryOptions Full => new();
}