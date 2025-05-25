using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.FileExplorer.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class FolderSortEntityTypeConfiguration : IEntityTypeConfiguration<FolderSort>
{
    public void Configure(EntityTypeBuilder<FolderSort> builder)
    {
        builder.HasOne(o => o.Node)
            .WithMany()
            .HasForeignKey(o => o.NodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}