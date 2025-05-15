using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Data.EF.Configuration;

public class FileSystemRootEntityConfiguration : IEntityTypeConfiguration<FileSystemRootEntity>
{
    public void Configure(EntityTypeBuilder<FileSystemRootEntity> builder)
    {
        builder.HasMany(m => m.Directories)
            .WithOne(m => m.FileSystemRoot)
            .HasForeignKey(m => m.FileSystemRootId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}