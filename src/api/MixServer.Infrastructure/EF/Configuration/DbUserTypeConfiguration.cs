using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Sessions.Entities;
using MixServer.Infrastructure.EF.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class DbUserTypeConfiguration : IEntityTypeConfiguration<DbUser>
{
    public void Configure(EntityTypeBuilder<DbUser> builder)
    {
        builder
            .HasOne(o => o.CurrentPlaybackSession)
            .WithOne()
            .HasForeignKey<PlaybackSession>(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder
            .HasMany(m => m.PlaybackSessions)
            .WithOne()
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(m => m.FolderSorts)
            .WithOne()
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(m => m.Devices)
            .WithMany();

        builder
            .HasMany(m => m.Credentials)
            .WithOne()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}