using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class UserQueueItemEntityConfiguration : IEntityTypeConfiguration<UserQueueItem>
{
    public void Configure(EntityTypeBuilder<UserQueueItem> builder)
    {
        builder.HasKey(q => q.Id);
        
        builder.HasOne(o => o.Queue)
            .WithMany(m => m.UserQueueItems)
            .HasForeignKey(f => f.QueueId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(o => o.File)
            .WithMany()
            .HasForeignKey(f => f.FileId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(o => o.PreviousFolderItem)
            .WithMany()
            .HasForeignKey(f => f.PreviousFolderItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}