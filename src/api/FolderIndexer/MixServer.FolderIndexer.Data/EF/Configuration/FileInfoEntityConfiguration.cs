using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Data.EF.Configuration;

public class FileInfoEntityConfiguration : IEntityTypeConfiguration<FileInfoEntity>
{
    public void Configure(EntityTypeBuilder<FileInfoEntity> builder)
    {
    }
}