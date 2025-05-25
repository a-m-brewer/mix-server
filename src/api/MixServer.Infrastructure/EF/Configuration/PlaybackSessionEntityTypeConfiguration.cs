using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class PlaybackSessionEntityTypeConfiguration : IEntityTypeConfiguration<PlaybackSession>
{
    public void Configure(EntityTypeBuilder<PlaybackSession> builder)
    {
        builder
            .HasOne(o => o.Node)
            .WithMany()
            .HasForeignKey(o => o.NodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}