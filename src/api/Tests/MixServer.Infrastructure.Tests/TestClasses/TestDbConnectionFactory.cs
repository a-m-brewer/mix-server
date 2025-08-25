using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MixServer.Infrastructure.EF;

namespace MixServer.Infrastructure.Tests.TestClasses;

public class TestDbConnectionFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    
    public TestDbConnectionFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        
        // Create initial schema
        using var initialContext = CreateContext();
        initialContext.Database.EnsureCreated();
    }
    
    public MixServerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MixServerDbContext>()
            .UseSqlite(_connection)
            .Options;
        
        return new MixServerDbContext(options);
    }
    
    public void Dispose()
    {
        _connection.Dispose();
    }
}