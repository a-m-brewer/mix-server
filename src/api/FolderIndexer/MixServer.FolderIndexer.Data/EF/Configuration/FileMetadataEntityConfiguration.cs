using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Data.EF.Configuration;

public class FileMetadataEntityConfiguration : IEntityTypeConfiguration<FileMetadataEntity>
{
    public void Configure(EntityTypeBuilder<FileMetadataEntity> builder)
    {
        builder.HasKey(k => k.Id);

        builder.HasOne(o => o.File)
            .WithOne(o => o.Metadata)
            .HasForeignKey<FileMetadataEntity>(f => f.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UseTphMappingStrategy()
            .HasDiscriminator(t => t.Type)
            .HasValue<FileMetadataEntity>(nameof(FileMetadataEntity))
            .HasValue<MediaMetadataEntity>(nameof(MediaMetadataEntity));
    }
}