using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.Queueing.Commands.AddToQueue;

public class AddToQueueCommand
{
    public required NodePathDto NodePath { get; init; }
}