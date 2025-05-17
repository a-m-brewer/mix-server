using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Data.EF.Configuration;

public class FileSystemEntityConfiguration : IEntityTypeConfiguration<FileSystemInfoEntity>
{
    public void Configure(EntityTypeBuilder<FileSystemInfoEntity> builder)
    {
        builder.HasKey(k => k.Id);
        builder.HasIndex(i => new { i.RootId, i.RelativePath })
            .IsUnique();

        builder.UseTphMappingStrategy()
            .HasDiscriminator(t => t.Type)
            .HasValue<RootDirectoryInfoEntity>(nameof(RootDirectoryInfoEntity))
            .HasValue<DirectoryInfoEntity>(nameof(DirectoryInfoEntity))
            .HasValue<FileInfoEntity>(nameof(FileInfoEntity));

        builder.HasOne(o => o.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(f => f.ParentId);
        
        builder.HasOne(o => o.Root)
            .WithMany()
            .HasForeignKey(f => f.RootId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}