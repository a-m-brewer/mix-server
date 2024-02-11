namespace MixServer.Domain.Sessions.Requests;

public interface IAddOrUpdateSessionRequest
{
    string AbsoluteFilePath { get; }
}

public class AddOrUpdateSessionRequest : IAddOrUpdateSessionRequest
{
    public string? ParentAbsoluteFilePath { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string AbsoluteFilePath => Path.Join(ParentAbsoluteFilePath, FileName);
}