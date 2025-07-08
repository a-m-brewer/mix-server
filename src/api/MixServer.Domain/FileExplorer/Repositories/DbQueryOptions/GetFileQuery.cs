namespace MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;

public class GetFileQueryOptions
{
    public bool IncludeMetadata { get; set; }
    
    public bool IncludeTranscode { get; set; }
    
    public bool IncludeTracklist { get; set; }
    
    public static GetFileQueryOptions Full => new()
    {
        IncludeMetadata = true,
        IncludeTranscode = true,
        IncludeTracklist = true
    };
    
    public static GetFileQueryOptions MetadataOnly => new()
    {
        IncludeMetadata = true,
        IncludeTranscode = false,
        IncludeTracklist = false
    };
}