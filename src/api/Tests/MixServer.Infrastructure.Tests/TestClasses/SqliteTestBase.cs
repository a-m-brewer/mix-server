using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.Tests.TestExtensions;
using MixServer.Infrastructure.Tests.TestModels;
using NUnit.Framework;

namespace MixServer.Infrastructure.Tests.TestClasses;

public abstract class SqliteTestBase
{
    private TestSqliteDbConnection _testConnection = null!;
    
    [SetUp]
    public void BaseSetup()
    {
        _testConnection = SqliteInMemoryContextExtensions.CreateContext();
        
        Setup();
    }
    
    [TearDown]
    public void BaseTearDown()
    {
        Teardown();
        
        _testConnection.Dispose();
        _testConnection = null!;
    }
    
    protected MixServerDbContext Context => _testConnection.Context;

    protected virtual void Setup() {}
    
    protected virtual void Teardown() {}
}