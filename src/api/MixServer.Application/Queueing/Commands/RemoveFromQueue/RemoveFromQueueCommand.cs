namespace MixServer.Application.Queueing.Commands.RemoveFromQueue;

public class RemoveFromQueueCommand
{
    public List<Guid> QueueItems { get; set; } = [];
}