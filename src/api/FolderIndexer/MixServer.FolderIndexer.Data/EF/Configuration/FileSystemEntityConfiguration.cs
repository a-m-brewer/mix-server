using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Data.EF.Configuration;

public class FileSystemEntityConfiguration : IEntityTypeConfiguration<FileSystemInfoEntity>
{
    public void Configure(EntityTypeBuilder<FileSystemInfoEntity> builder)
    {
        builder.HasKey(k => k.Id);
        builder.HasIndex(i => i.AbsolutePath)
            .IsUnique();

        builder.UseTphMappingStrategy()
            .HasDiscriminator(t => t.Type);
    }
}