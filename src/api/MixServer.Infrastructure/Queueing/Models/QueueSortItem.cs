namespace MixServer.Infrastructure.Queueing.Models;

public abstract class QueueSortItem
{
    protected QueueSortItem(Guid id, string absoluteFilePath)
    {
        Id = id;
        AbsoluteFilePath = absoluteFilePath;
    }

    public Guid Id { get; }

    public string AbsoluteFilePath { get; }
}