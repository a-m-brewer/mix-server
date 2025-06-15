using MixServer.Domain.FileExplorer.Entities;

namespace MixServer.Domain.FileExplorer.Models;

public record AddMediaMetadataRequest(
    MediaMetadataEntity AddedMetadata,
    FileMetadataEntity? RemovedMetadata);