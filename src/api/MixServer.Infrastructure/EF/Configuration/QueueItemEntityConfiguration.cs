using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Queueing.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class QueueItemEntityConfiguration : IEntityTypeConfiguration<QueueItemEntity>
{
    public void Configure(EntityTypeBuilder<QueueItemEntity> builder)
    {
        builder
            .HasKey(k => k.Id);
        
        builder
            .HasOne(o => o.File)
            .WithMany()
            .HasForeignKey(f => f.FileId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasIndex(i => new { i.QueueId, i.Rank }).IsUnique();
    }
}