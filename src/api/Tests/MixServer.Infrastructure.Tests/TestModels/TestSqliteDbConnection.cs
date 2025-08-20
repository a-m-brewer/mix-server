using Microsoft.Data.Sqlite;
using MixServer.Infrastructure.EF;

namespace MixServer.Infrastructure.Tests.TestModels;

public class TestSqliteDbConnection : IDisposable, IAsyncDisposable
{
    public required MixServerDbContext Context { get; init; }
    
    public required SqliteConnection Connection { get; init; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        
        Context.Dispose();
        Connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}