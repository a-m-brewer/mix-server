namespace MixServer.Shared.Interfaces;

public interface IRepository {}

public interface IScopedRepository : IRepository {}

public interface ITransientRepository : IRepository {}

public interface ISingletonRepository : IRepository {}
