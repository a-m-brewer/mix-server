using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MixServer.Infrastructure.EF.Factories;

public class MixServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<MixServerDbContext>
{
    public MixServerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MixServerDbContext>();
        optionsBuilder.UseSqlite("Data Source=mix-server.db");

        return new MixServerDbContext(optionsBuilder.Options);

    }
}