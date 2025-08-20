using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class QueueEntityConfiguration : IEntityTypeConfiguration<QueueEntity>
{
    public void Configure(EntityTypeBuilder<QueueEntity> builder)
    {
        builder.HasKey(k => k.Id);
        
        builder.HasOne(o => o.CurrentPosition)
            .WithMany()
            .HasForeignKey(f => f.CurrentPositionId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(o => o.CurrentFolder)
            .WithMany()
            .HasForeignKey(f => f.CurrentFolderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}