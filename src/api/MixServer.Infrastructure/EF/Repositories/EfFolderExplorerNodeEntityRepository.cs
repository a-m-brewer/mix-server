using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;
using MixServer.Infrastructure.EF.Extensions;
using EEF = Microsoft.EntityFrameworkCore.EF;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfFileExplorerNodeRepository(MixServerDbContext context) : IFileExplorerNodeRepository
{
    public async Task<FileExplorerFileNodeEntity> GetFileNodeAsync(NodePath nodePath, GetFileQueryOptions options, CancellationToken cancellationToken)
    {
        var query = GetFileQuery(options);

        return await FirstAsync(query, nodePath, cancellationToken);
    }

    public async Task<ICollection<FileExplorerFileNodeEntity>> GetFileNodesAsync(IEnumerable<NodePath> nodePaths, GetFileQueryOptions options, CancellationToken cancellationToken)
    {
        var lookupPaths = nodePaths
            .Select(np => np.RootPath + ";" + np.RelativePath)
            .ToHashSet();
        
        if (lookupPaths.Count == 0)
        {
            return [];
        }

        var query = from file in GetFileQuery(options)
            let path = file.RootChild.RelativePath + ";" + file.RelativePath
            where lookupPaths.Contains(path)
            select file;

        return await query.ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<ICollection<FileExplorerFileNodeEntity>> GetFileNodesAsync(NodePath parentNodePath, GetFileQueryOptions options, CancellationToken cancellationToken)
    {
        var parentPath = parentNodePath.RelativePath;
        var hasPath = !string.IsNullOrWhiteSpace(parentPath);

        var childPattern = hasPath
            ? $"{parentPath}{Path.DirectorySeparatorChar}%"
            : "%";
        var descendantPattern = hasPath
            ? $"{parentPath}{Path.DirectorySeparatorChar}%{Path.DirectorySeparatorChar}%"
            : $"%{Path.DirectorySeparatorChar}%";
        
        var query = GetFileQuery(options)
            .Where(w =>
                w.RootChild.RelativePath == parentNodePath.RootPath &&
                EEF.Functions.Like(w.RelativePath, childPattern) &&
                !EEF.Functions.Like(w.RelativePath, descendantPattern));
        
        return await query.ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<FileExplorerFileNodeEntity?> GetFileNodeOrDefaultAsync(NodePath nodePath, GetFileQueryOptions options, CancellationToken cancellationToken)
    {
        var query = GetFileQuery(options);

        return await FirstOrDefaultAsync(query, nodePath, cancellationToken);
    }

    public Task<List<FileExplorerFileNodeEntity>> GetFileNodesAsync(string rootPath, IEnumerable<string> relativePaths, GetFileQueryOptions options, CancellationToken cancellationToken)
    {
        var query = GetFileQuery(options);

        return 
            query.Where(w => w.RootChild.RelativePath == rootPath && relativePaths.Contains(w.RelativePath))
                .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<List<FileExplorerFolderNodeEntity>> GetFolderNodesAsync(string rootPath, IEnumerable<Guid> folderIds, GetFolderQueryOptions options, CancellationToken cancellationToken)
    {
        var query = GetFolderQuery(options);

        var folders = await query
            .Where(w => w.RootChild.RelativePath == rootPath && folderIds.Contains(w.Id))
            .ToListAsync(cancellationToken: cancellationToken);
        
        await Task.WhenAll(folders.Select(f => LoadFolderQueryRelationshipsAsync(f, options, cancellationToken)));
        
        return folders;
    }

    public async Task<List<FileExplorerFolderNodeEntity>> GetFolderNodesAsync(IEnumerable<NodePath> nodePaths,
        CancellationToken cancellationToken)
    {
        var lookupPaths = nodePaths
            .Select(np => np.RootPath + ";" + np.RelativePath)
            .ToHashSet();
        
        if (lookupPaths.Count == 0)
        {
            return [];
        }
        
        var query = from folder in GetFolderQuery(GetFolderQueryOptions.FolderOnly)
            let path = folder.RootChild.RelativePath + ";" + folder.RelativePath
            where lookupPaths.Contains(path)
            select folder;
        
        
        var folders = await query.ToListAsync(cancellationToken: cancellationToken);

        return folders;
    }

    public Task<List<FileExplorerFolderNodeEntity>> GetFolderNodesAsync(NodePath parentNodePath,
        GetFolderQueryOptions options, CancellationToken cancellationToken)
    {
        var parentPath = parentNodePath.RelativePath;
        var hasPath = !string.IsNullOrWhiteSpace(parentPath);

        var childPattern = hasPath
            ? $"{parentPath}{Path.DirectorySeparatorChar}%"
            : "%";
        var descendantPattern = hasPath
            ? $"{parentPath}{Path.DirectorySeparatorChar}%{Path.DirectorySeparatorChar}%"
            : $"%{Path.DirectorySeparatorChar}%";
        
        var query = GetFolderQuery(options)
            .Where(w =>
                w.RootChild.RelativePath == parentNodePath.RootPath &&
                EEF.Functions.Like(w.RelativePath, childPattern) &&
                !EEF.Functions.Like(w.RelativePath, descendantPattern));
        
        return query.ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<FileExplorerFolderNodeEntity> GetFolderNodeAsync(NodePath nodePath, GetFolderQueryOptions options, CancellationToken cancellationToken)
    {
        var query = GetFolderQuery(options);
        
        var folder = await FirstAsync(query, nodePath, cancellationToken);
        
        await LoadFolderQueryRelationshipsAsync(folder, options, cancellationToken);
        
        return folder;
    }

    public async Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(NodePath nodePath, GetFolderQueryOptions options, CancellationToken cancellationToken)
    {
        var query = GetFolderQuery(options);

        var folder = await FirstOrDefaultAsync(query, nodePath, cancellationToken);
        
        await LoadFolderQueryRelationshipsAsync(folder, options, cancellationToken);
        
        return folder;
    }

    public async Task<FileExplorerRootChildNodeEntity?> GetRootChildFolderNodeOrDefaultAsync(NodePath nodePath, GetFolderQueryOptions options,
        CancellationToken cancellationToken)
    {
        var query = GetRootChildFolderQuery(options);
        
        var folder = await query.FirstOrDefaultAsync(f => f.RelativePath == nodePath.RootPath, cancellationToken);
        
        await LoadFolderQueryRelationshipsAsync(folder, options, cancellationToken);
        
        return folder;
    }

    public async Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(Guid nodeId, GetFolderQueryOptions options, CancellationToken cancellationToken)
    {
        var query = GetFolderQuery(options);

        var folder = await query.FirstOrDefaultAsync(f => f.Id == nodeId, cancellationToken);

        await LoadFolderQueryRelationshipsAsync(folder, options, cancellationToken);
        
        return folder;
    }

    public async Task<FileExplorerRootChildNodeEntity?> GetRootChildFolderNodeOrDefaultAsync(Guid nodeId, GetFolderQueryOptions options,
        CancellationToken cancellationToken)
    {
        var query = GetRootChildFolderQuery(options);
        
        var folder = await query.FirstOrDefaultAsync(f => f.Id == nodeId, cancellationToken);

        await LoadFolderQueryRelationshipsAsync(folder, options, cancellationToken);
        
        return folder;
    }

    private IQueryable<FileExplorerFolderNodeEntity> GetFolderQuery(GetFolderQueryOptions options)
    {
        var query = GetBaseQuery<FileExplorerFolderNodeEntity>();
        
        return GetFolderQuery(query, options);
    }
    
    private IQueryable<FileExplorerRootChildNodeEntity> GetRootChildFolderQuery(GetFolderQueryOptions options)
    {
        var query = context.Nodes
            .OfType<FileExplorerRootChildNodeEntity>();
        
        return GetRootChildFolderQuery(query, options);
    }
    
    private IQueryable<FileExplorerFolderNodeEntity> GetFolderQuery(IQueryable<FileExplorerFolderNodeEntity> query, GetFolderQueryOptions options)
    {
        return query;
    }
    
    private IQueryable<FileExplorerRootChildNodeEntity> GetRootChildFolderQuery(IQueryable<FileExplorerRootChildNodeEntity> query, GetFolderQueryOptions options)
    {
        return query;
    }
    
    private IQueryable<FileExplorerFolderNodeEntity> GetChildFolderQuery(IQueryable<FileExplorerFolderNodeEntity> query, GetChildFolderQueryOptions options)
    {
        return query;
    }
    
    private IQueryable<FileExplorerFileNodeEntity> GetFileQuery(GetFileQueryOptions options)
    {
        var query = GetBaseQuery<FileExplorerFileNodeEntity>();
        return query.IncludeGetFileQueryOptions(options);
    }
    
    private async Task LoadFolderQueryRelationshipsAsync(FileExplorerFolderNodeEntity? folder, GetFolderQueryOptions options, CancellationToken cancellationToken = default)
    {
        if (folder is null)
        {
            return;
        }

        if (options.ChildFolders is not null)
        {
            var folderQuery = context.Entry(folder)
                .Collection(c => c.Children)
                .Query()
                .OfType<FileExplorerFolderNodeEntity>();
            folderQuery = GetChildFolderQuery(folderQuery, options.ChildFolders);
            await folderQuery.LoadAsync(cancellationToken: cancellationToken);
        }

        if (options.ChildFiles is not null)
        {
            var fileQuery = context.Entry(folder)
                .Collection(c => c.Children)
                .Query()
                .OfType<FileExplorerFileNodeEntity>();
            fileQuery = fileQuery.IncludeGetFileQueryOptions(options.ChildFiles);
            await fileQuery.LoadAsync(cancellationToken: cancellationToken);
        }
    }
    
    private async Task LoadFolderQueryRelationshipsAsync(FileExplorerRootChildNodeEntity? folder, GetFolderQueryOptions options, CancellationToken cancellationToken = default)
    {
        if (folder is null)
        {
            return;
        }

        if (options.ChildFolders is not null)
        {
            var folderQuery = context.Entry(folder)
                .Collection(c => c.Children)
                .Query()
                .OfType<FileExplorerFolderNodeEntity>()
                // Everything is a child of the root child, so we only need to load files that are direct children
                .IncludeParents()
                .Where(w => w.Parent == null);
            folderQuery = GetChildFolderQuery(folderQuery, options.ChildFolders);
            await folderQuery.LoadAsync(cancellationToken: cancellationToken);
        }

        if (options.ChildFiles is not null)
        {
            var fileQuery = context.Entry(folder)
                .Collection(c => c.Children)
                .Query()
                .OfType<FileExplorerFileNodeEntity>()
                // Everything is a child of the root child, so we only need to load files that are direct children
                .IncludeParents()
                .Where(w => w.Parent == null);
            fileQuery = fileQuery.IncludeGetFileQueryOptions(options.ChildFiles);
            await fileQuery.LoadAsync(cancellationToken: cancellationToken);
        }
    }

    public async Task<IEnumerable<FileExplorerFileNodeEntity>> GetFileNodesAsync(List<Guid> fileIds, CancellationToken cancellationToken)
    {
        return await context.Nodes
            .OfType<FileExplorerFileNodeEntity>()
            .IncludeParents()
            .Include(i => i.Metadata)
            .Include(i => i.Tracklist)
            .Where(w => fileIds.Contains(w.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<List<FileExplorerFolderNodeEntity>> GetFolderNodesAsync(string rootPath, IEnumerable<Guid> folderIds,
        CancellationToken cancellationToken)
    {
        return context.Nodes
            .OfType<FileExplorerFolderNodeEntity>()
            .IncludeParents()
            .Where(w => w.RootChild.RelativePath == rootPath && folderIds.Contains(w.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<FolderHeader?> GetFolderHeaderOrDefaultAsync(
        FolderHeader header,
        CancellationToken cancellationToken)
    {
        if (header.NodePath.IsRoot)
        {
            return null; // Root folder has no hash
        }

        if (header.NodePath.IsRootChild)
        {
            return await context.Nodes.OfType<FileExplorerRootChildNodeEntity>()
                .Where(w => w.RelativePath == header.NodePath.RootPath || w.Hash == header.Hash)
                .Select(s => new FolderHeader
                {
                    NodePath = new NodePath(s.RelativePath, string.Empty),
                    Hash = s.Hash
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        
        return await context.Nodes.OfType<FileExplorerFolderNodeEntity>()
            .Include(i => i.RootChild)
            .Where(w => 
                (w.RelativePath == header.NodePath.RelativePath && w.RootChild.RelativePath == header.NodePath.RootPath) ||
                w.Hash == header.Hash)
            .Select(s => new FolderHeader
            {
                NodePath = new NodePath(s.RootChild.RelativePath, s.RelativePath),
                Hash = s.Hash
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<NodePath, FolderHeader>> GetFolderHeadersAsync(List<NodePath> childNodePaths,
        CancellationToken cancellationToken)
    {
        if (childNodePaths.Count == 0)
        {
            return [];
        }

        var lookupPaths = childNodePaths
            .Select(np => np.RootPath + ";" + np.RelativePath)
            .ToHashSet();

        var query = from folder in GetFolderQuery(GetFolderQueryOptions.FolderOnly)
            let path = folder.RootChild.RelativePath + ";" + folder.RelativePath
            where lookupPaths.Contains(path)
            select new FolderHeader
            {
                NodePath = folder.Path,
                Hash = folder.Hash
            };

        return await query
            .ToDictionaryAsync(k => k.NodePath, cancellationToken);
    }

    public async Task<FileExplorerRootChildNodeEntity> GetRootChildOrThrowAsync(string rootPath, CancellationToken cancellationToken)
    {
        var entity = await GetRootChildOrDefaultAsync(new NodePath(rootPath, string.Empty), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException(nameof(context.Nodes), rootPath);
        }
        
        return entity;
    }

    public async Task<FileExplorerRootChildNodeEntity?> GetRootChildOrDefaultAsync(NodePath rootChild, CancellationToken cancellationToken)
    {
        return await context.Nodes
            .OfType<FileExplorerRootChildNodeEntity>()
            .FirstOrDefaultAsync(f => f.RelativePath == rootChild.RootPath, cancellationToken);
    }

    public async Task<ICollection<FileExplorerRootChildNodeEntity>> GetRootChildrenAsync(IEnumerable<string> rootPaths, CancellationToken cancellationToken)
    {
        return await context.Nodes
            .OfType<FileExplorerRootChildNodeEntity>()
            .Where(w => rootPaths.Contains(w.RelativePath))
            .ToListAsync(cancellationToken);
    }

    public async Task<ICollection<FileExplorerRootChildNodeEntity>> GetAllRootChildrenAsync(CancellationToken cancellationToken)
    {
        return await context.Nodes
            .OfType<FileExplorerRootChildNodeEntity>()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FileExplorerNodeEntityBase nodeEntity, CancellationToken cancellationToken)
    {
        if (nodeEntity is FileExplorerFileNodeEntity { Metadata: not null } file)
        {
            await context.FileMetadata.AddAsync(file.Metadata, cancellationToken);
        }
        await context.Nodes.AddAsync(nodeEntity, cancellationToken);
    }

    public async Task AddRangeAsync(ICollection<FileExplorerNodeEntity> addedChildren, CancellationToken cancellationToken)
    {
        var metadata = addedChildren
            .OfType<FileExplorerFileNodeEntity>()
            .Select(s => s.Metadata)
            .Where(w => w is not null)
            .Cast<FileMetadataEntity>()
            .ToList();
        await context.FileMetadata.AddRangeAsync(metadata, cancellationToken);

        await context.Nodes.AddRangeAsync(addedChildren, cancellationToken);
    }

    public void RemoveRange(IEnumerable<FileExplorerNodeEntity> nodes)
    {
        context.Nodes.RemoveRange(nodes);
    }
    
    private IQueryable<TEntity> GetBaseQuery<TEntity>()
        where TEntity : FileExplorerNodeEntity
    {
        return context.Nodes
            .OfType<TEntity>()
            .IncludeParents();
    }

    private async Task<TEntity?> FirstOrDefaultAsync<TEntity>(IQueryable<TEntity> query, NodePath nodePath, CancellationToken cancellationToken)
        where TEntity : FileExplorerNodeEntity
    {
        return await query.FirstOrDefaultAsync(
            f => f.RelativePath == nodePath.RelativePath && f.RootChild.RelativePath == nodePath.RootPath,
            cancellationToken);
    }
    
    private async Task<TEntity> FirstAsync<TEntity>(IQueryable<TEntity> query, NodePath nodePath, CancellationToken cancellationToken)
        where TEntity : FileExplorerNodeEntity
    {
        return await FirstOrDefaultAsync(query, nodePath, cancellationToken) 
            ?? throw new NotFoundException(nameof(context.Nodes), nodePath.RelativePath);;
    }
}