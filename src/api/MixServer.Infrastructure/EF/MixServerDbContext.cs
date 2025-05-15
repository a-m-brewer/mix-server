#nullable disable

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Streams.Entities;
using MixServer.Domain.Users.Entities;
using MixServer.FolderIndexer.Data.EF.Configuration;
using MixServer.FolderIndexer.Domain.Entities;
using MixServer.Infrastructure.EF.Entities;

namespace MixServer.Infrastructure.EF;

public class MixServerDbContext(DbContextOptions<MixServerDbContext> options) : IdentityDbContext<DbUser>(options)
{
    public DbSet<PlaybackSession> PlaybackSessions { get; set; }
    
    public DbSet<FolderSort> FolderSorts { get; set; }

    public DbSet<Device> Devices { get; set; }
    
    public DbSet<Transcode> Transcodes { get; set; }

    public DbSet<UserCredential> UserCredentials { get; set; }

    #region FileIndexer

    public DbSet<FileSystemInfoEntity> FileSystemNodes { get; set; }

    #endregion
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(MixServerDbContext).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(FileSystemEntityConfiguration).Assembly);
    }
}