namespace MixServer.Domain.FileExplorer.Models;

public record UpdateMediaMetadataRequest(List<Guid> FileIds);