namespace MixServer.Infrastructure.Queueing.Models;

public abstract class QueueSortItem(Guid id, string absoluteFilePath)
{
    public Guid Id { get; } = id;

    public string AbsoluteFilePath { get; } = absoluteFilePath;
}