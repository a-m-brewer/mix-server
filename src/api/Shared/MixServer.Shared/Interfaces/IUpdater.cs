namespace MixServer.Shared.Interfaces;

public interface IUpdater;

public interface IUpdater<in TValue, in TUpdate> : IUpdater
{
    void Update(TValue value, TUpdate update);
}
