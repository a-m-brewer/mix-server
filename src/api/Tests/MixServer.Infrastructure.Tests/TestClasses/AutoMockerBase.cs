using Moq.AutoMock;
using NUnit.Framework;

namespace MixServer.Infrastructure.Tests.TestClasses;

public abstract class AutoMockerBase<T> where T : class
{
    protected AutoMocker Mocker { get; set; } = null!;
    
    protected T Subject { get; set; } = null!;
    
    [SetUp]
    public void BaseSetup()
    {
        Mocker = new AutoMocker();
        Setup();
        
        Subject = Mocker.CreateInstance<T>();
    }
    
    [TearDown]
    public void BaseTearDown()
    {
        Teardown();
        Mocker = null!;
        Subject = null!;
    }
    
    protected virtual void Setup() {}
    
    protected virtual void Teardown() {}
}