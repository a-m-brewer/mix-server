using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.FileExplorer.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class FileExplorerNodeEntityTypeConfiguration : IEntityTypeConfiguration<FileExplorerNodeEntity>
{
    public void Configure(EntityTypeBuilder<FileExplorerNodeEntity> builder)
    {
        builder
            .HasIndex(i => new { i.RootChildId, i.RelativePath })
            .IsUnique();
        
        builder.HasOne(o => o.RootChild)
            .WithMany()
            .HasForeignKey(f => f.RootChildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}