using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Infrastructure.EF.Configuration;

public class FileMetadataEntityConfiguration : IEntityTypeConfiguration<FileMetadataEntity>
{
    public void Configure(EntityTypeBuilder<FileMetadataEntity> builder)
    {
        builder.HasKey(k => k.Id);
        
        builder.HasOne(o => o.Node)
            .WithOne(o => o.Metadata)
            .HasForeignKey<FileMetadataEntity>(f => f.NodeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.UseTphMappingStrategy()
            .HasDiscriminator(e => e.Type)
            .HasValue<FileMetadataEntity>(FileMetadataType.File)
            .HasValue<MediaMetadataEntity>(FileMetadataType.Media);
    }
}