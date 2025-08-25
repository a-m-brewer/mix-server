using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.Tests.TestModels;

namespace MixServer.Infrastructure.Tests.TestExtensions;

public static class SqliteInMemoryContextExtensions
{
    public static TestSqliteDbConnection CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        
        return CreateContextFromConnection(connection, ensureCreated: true);
    }
    
    public static TestSqliteDbConnection CreateContextFromConnection(SqliteConnection connection, bool ensureCreated = false)
    {
        var options = new DbContextOptionsBuilder<MixServerDbContext>()
            .UseSqlite(connection)
            .Options;
        
        var context = new MixServerDbContext(options);
        
        if (ensureCreated)
        {
            context.Database.EnsureCreated();
        }
        
        return new TestSqliteDbConnection
        {
            Context = context,
            Connection = connection
        };
    }
}