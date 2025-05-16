using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Data.EF.Configuration;

public class RootDirectoryInfoEntityConfiguration : IEntityTypeConfiguration<RootDirectoryInfoEntity>
{
    public void Configure(EntityTypeBuilder<RootDirectoryInfoEntity> builder)
    {
    }
}