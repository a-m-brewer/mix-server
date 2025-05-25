using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Infrastructure.EF.Configuration;

public class FileExplorerNodeEntityBaseEntityConfiguration : IEntityTypeConfiguration<FileExplorerNodeEntityBase>
{
    public void Configure(EntityTypeBuilder<FileExplorerNodeEntityBase> builder)
    {
        builder.HasKey(k => k.Id);
        
        builder.UseTphMappingStrategy()
            .HasDiscriminator(e => e.NodeType)
            .HasValue<FileExplorerNodeEntityBase>(FileExplorerEntityNodeType.Base)
            .HasValue<FileExplorerRootChildNodeEntity>(FileExplorerEntityNodeType.RootChild)
            .HasValue<FileExplorerNodeEntity>(FileExplorerEntityNodeType.Node)
            .HasValue<FileExplorerFolderNodeEntity>(FileExplorerEntityNodeType.Folder)
            .HasValue<FileExplorerFileNodeEntity>(FileExplorerEntityNodeType.File);
    }
}