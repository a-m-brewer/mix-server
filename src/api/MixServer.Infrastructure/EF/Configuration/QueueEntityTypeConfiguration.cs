using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Queueing.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class QueueEntityTypeConfiguration : IEntityTypeConfiguration<QueueEntity>
{
    public void Configure(EntityTypeBuilder<QueueEntity> builder)
    {
        builder
            .HasKey(k => k.Id);
        
        builder
            .HasMany(m => m.Items)
            .WithOne(o => o.Queue)
            .HasForeignKey(f => f.QueueId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder
            .HasOne(o => o.CurrentPosition)
            .WithOne()
            .HasForeignKey<QueueEntity>(f => f.CurrentPositionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}