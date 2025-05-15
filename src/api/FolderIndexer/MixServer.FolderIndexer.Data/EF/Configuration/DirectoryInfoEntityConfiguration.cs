using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Data.EF.Configuration;

public class DirectoryInfoEntityConfiguration : IEntityTypeConfiguration<DirectoryInfoEntity>
{
    public void Configure(EntityTypeBuilder<DirectoryInfoEntity> builder)
    {
    }
}