#nullable disable

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Streams.Entities;
using MixServer.Domain.Tracklists.Entities;
using MixServer.Domain.Users.Entities;
using MixServer.Infrastructure.EF.Entities;

namespace MixServer.Infrastructure.EF;

public class MixServerDbContext(DbContextOptions<MixServerDbContext> options) : IdentityDbContext<DbUser>(options)
{
    public DbSet<PlaybackSession> PlaybackSessions { get; set; }
    
    public DbSet<FolderSort> FolderSorts { get; set; }

    public DbSet<Device> Devices { get; set; }
    
    public DbSet<Transcode> Transcodes { get; set; }

    public DbSet<UserCredential> UserCredentials { get; set; }
    
    public DbSet<FileExplorerNodeEntityBase> Nodes { get; set; }
    
    public DbSet<FileMetadataEntity> FileMetadata { get; set; }
    
    public DbSet<TracklistEntity> Tracklists { get; set; }
    
    public DbSet<CueEntity> Cues { get; set; }
    
    public DbSet<TrackEntity> Tracks { get; set; }
    
    public DbSet<TracklistPlayersEntity> TracklistPlayers { get; set; }
    
    public DbSet<QueueEntity> Queues { get; set; }
    
    public DbSet<QueueItemEntity> QueueItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(MixServerDbContext).Assembly);
    }
}