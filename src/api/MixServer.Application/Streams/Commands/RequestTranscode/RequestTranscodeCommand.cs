using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.Streams.Commands.RequestTranscode;

public class RequestTranscodeCommand
{
    public required NodePathRequestDto NodePath { get; init; }
}