using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.Tests.TestExtensions;
using MixServer.Infrastructure.Tests.TestModels;
using NUnit.Framework;

namespace MixServer.Infrastructure.Tests.TestClasses;

public abstract class SqliteTestBase<T> : AutoMockerBase<T>
    where T : class
{
    private TestDbConnectionFactory _connectionFactory = null!;

    protected MixServerDbContext Context => Mocker.Get<MixServerDbContext>();
    
    protected override void Setup()
    {
        _connectionFactory = new TestDbConnectionFactory();
        
        using var setupContext = _connectionFactory.CreateContext();
        Setup(setupContext);

        Mocker.Use(_connectionFactory.CreateContext());
    }

    protected virtual void Setup(MixServerDbContext setupContext) {}
    
    protected override void Teardown()
    {
        Context.Dispose();
        _connectionFactory.Dispose();
        _connectionFactory = null!;
    }
}