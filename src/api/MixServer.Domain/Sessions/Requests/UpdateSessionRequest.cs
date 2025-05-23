using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.Sessions.Requests;

public interface IAddOrUpdateSessionRequest
{
    NodePath NodePath { get; }
}

public class AddOrUpdateSessionRequest : IAddOrUpdateSessionRequest
{
    public required NodePath NodePath { get; set; }
}