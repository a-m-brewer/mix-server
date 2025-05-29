using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Tracklists.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class TracklistPlayersEntityConfiguration : IEntityTypeConfiguration<TracklistPlayersEntity>
{
    public void Configure(EntityTypeBuilder<TracklistPlayersEntity> builder)
    {
        builder.HasKey(k => k.Id);
        
        builder.HasOne(o => o.Track)
            .WithMany(m => m.Players)
            .HasForeignKey(f => f.TrackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}