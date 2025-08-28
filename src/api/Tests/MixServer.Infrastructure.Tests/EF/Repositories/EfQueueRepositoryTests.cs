using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.Streams.Entities;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Users.Models;
using MixServer.Infrastructure.EF;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.EF.Repositories;
using MixServer.Infrastructure.Tests.TestClasses;
using NUnit.Framework;

namespace MixServer.Infrastructure.Tests.EF.Repositories;

public class EfQueueRepositoryTests : SqliteTestBase<EfQueueRepository>
{
    private DbUser _user = null!;
    private FileExplorerRootChildNodeEntity _root = null!;

    protected override void Setup(MixServerDbContext setupContext)
    {
        _user = new DbUser();
        setupContext.Users.Add(_user);

        _root = new FileExplorerRootChildNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = "/media",
            Exists = true,
            CreationTimeUtc = DateTime.UtcNow
        };
        setupContext.Nodes.Add(_root);

        setupContext.SaveChanges();
    }

    [Test]
    public async Task SetFolderAsync_UserHasNoQueue_CreatesQueue()
    {
        // Act
        await Subject.SetFolderAsync(_user.Id, Guid.NewGuid(), CancellationToken.None);
        
        // Assert
        var queue = await Context.Queues.
            SingleOrDefaultAsync(s => s.UserId == _user.Id);

        queue.Should()
            .NotBeNull();
        
        queue.UserId
            .Should()
            .Be(_user.Id);
    }

    [Test]
    public async Task SetFolderAsync_DefaultSortAppliedIfUserHasNotSort_Sorted()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);
        
        // Act
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        // Assert
        var items = await Context.QueueItems
            .Include(i => i.Queue)
            .Where(w => w.Queue.UserId == _user.Id)
            .OrderBy(o => o.Rank)
            .ToListAsync();
        
        items
            .Should()
            .HaveCount(3);
        
        items[0].FileId
            .Should()
            .Be(childA.Id);
        
        items[1].FileId
            .Should()
            .Be(childB.Id);
        
        items[2].FileId
            .Should()
            .Be(childC.Id);
    }
    
    [Test]
    public async Task SetFolderAsync_NameDeccending_Sorted()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);
        
        await SetUserSortAsync(parentNode, FolderSortMode.Name, true);
        
        // Act
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        // Assert
        var items = await Context.QueueItems
            .Include(i => i.Queue)
            .Where(w => w.Queue.UserId == _user.Id)
            .OrderBy(o => o.Rank)
            .ToListAsync();
        
        items
            .Should()
            .HaveCount(3);
        
        items[0].FileId
            .Should()
            .Be(childC.Id);
        
        items[1].FileId
            .Should()
            .Be(childB.Id);
        
        items[2].FileId
            .Should()
            .Be(childA.Id);
    }
    
    [Test]
    public async Task SetFolderAsync_CreationTimeAccending_Sorted()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A", new DateTime(2025, 8, 15));
        var childB = CreateChildNode(parentNode, "B", new DateTime(2025, 8, 14));
        var childC = CreateChildNode(parentNode, "C", new DateTime(2025, 8, 13));
        await AddNodesAsync(childC, childA, childB);
        
        await SetUserSortAsync(parentNode, FolderSortMode.Created, false);
        
        // Act
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        // Assert
        var items = await Context.QueueItems
            .Include(i => i.Queue)
            .Where(w => w.Queue.UserId == _user.Id)
            .OrderBy(o => o.Rank)
            .ToListAsync();
        
        items
            .Should()
            .HaveCount(3);
        
        items[0].FileId
            .Should()
            .Be(childC.Id);
        
        items[1].FileId
            .Should()
            .Be(childB.Id);
        
        items[2].FileId
            .Should()
            .Be(childA.Id);
    }

    [Test]
    public async Task SetFolderAsync_CreationTimeDeccending_Sorted()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A", new DateTime(2025, 8, 15));
        var childB = CreateChildNode(parentNode, "B", new DateTime(2025, 8, 14));
        var childC = CreateChildNode(parentNode, "C", new DateTime(2025, 8, 13));
        await AddNodesAsync(childC, childA, childB);
        
        await SetUserSortAsync(parentNode, FolderSortMode.Created, true);
        
        // Act
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        // Assert
        var items = await Context.QueueItems
            .Include(i => i.Queue)
            .Where(w => w.Queue.UserId == _user.Id)
            .OrderBy(o => o.Rank)
            .ToListAsync();
        
        items
            .Should()
            .HaveCount(3);
        
        items[0].FileId
            .Should()
            .Be(childA.Id);
        
        items[1].FileId
            .Should()
            .Be(childB.Id);
        
        items[2].FileId
            .Should()
            .Be(childC.Id);
    }
    
    [Test]
    public async Task SetFolderAsync_PreviousQueueItemsDeleted_OnlyNewItemsExist()
    {
        // Arrange
        var oldParentNode = await CreateParentNodeAsync("old");
        await AddNodesAsync(CreateChildNode(oldParentNode));
        
        var newParentNode = await CreateParentNodeAsync("new");
        var childA = CreateChildNode(newParentNode, "A");
        var childB = CreateChildNode(newParentNode, "B");
        var childC = CreateChildNode(newParentNode, "C");
        await AddNodesAsync(childC, childA, childB);
        
        // Act
        await Subject.SetFolderAsync(_user.Id, newParentNode.Id, CancellationToken.None);
        
        // Assert
        var items = await Context.QueueItems
            .Include(i => i.Queue)
            .Where(w => w.Queue.UserId == _user.Id)
            .OrderBy(o => o.Rank)
            .ToListAsync();
        
        items
            .Should()
            .HaveCount(3);
    }

    [Test]
    public async Task SetFolderAsync_SetSameFolderAgain_ExistingQueueItemsRemain()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var child = CreateChildNode(parentNode);
        await AddNodesAsync(child);
        
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        var initialQueueItem = await Context.QueueItems
            .Include(i => i.Queue)
            .Where(w => w.Queue.UserId == _user.Id)
            .FirstAsync();
        
        // Act
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        // Assert
        var postSetQueueItems = await Context.QueueItems
            .Include(i => i.Queue)
            .Where(w => w.Queue.UserId == _user.Id)
            .OrderBy(o => o.Rank)
            .ToListAsync();

        postSetQueueItems.Should()
            .HaveCount(1);
        
        postSetQueueItems[0].Id
            .Should()
            .Be(initialQueueItem.Id);
    }
    
    [Test]
    public async Task SetFolderAsync_SetSameDifferentFolder_OldFolderQueueItemsRemoved()
    {
        // Arrange
        var parentNodeA = await CreateParentNodeAsync(relativePath: "A");
        var childA = CreateChildNode(parentNodeA);
        await AddNodesAsync(childA);
        
        await Subject.SetFolderAsync(_user.Id, parentNodeA.Id, CancellationToken.None);
        
        var parentNodeB = await CreateParentNodeAsync(relativePath: "B");
        var childB = CreateChildNode(parentNodeB);
        await AddNodesAsync(childB);
        
        // Act
        await Subject.SetFolderAsync(_user.Id, parentNodeB.Id, CancellationToken.None);
        
        // Assert
        var items = await Context.QueueItems
            .Include(i => i.Queue)
            .Where(w => w.Queue.UserId == _user.Id)
            .OrderBy(o => o.Rank)
            .ToListAsync();

        items.Should()
            .HaveCount(1);
        
        items[0].FileId
            .Should()
            .Be(childB.Id);
    }
    
    [Test]
    public async Task SetFolderAsync_FolderSortChanged_SortedWithLatest()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A", new DateTime(2025, 8, 15));
        var childB = CreateChildNode(parentNode, "B", new DateTime(2025, 8, 14));
        var childC = CreateChildNode(parentNode, "C", new DateTime(2025, 8, 13));
        await AddNodesAsync(childC, childA, childB);
        
        var sort = await SetUserSortAsync(parentNode, FolderSortMode.Name, false);
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        // Act
        sort.SortMode = FolderSortMode.Created;
        await Context.SaveChangesAsync();
        
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        // Assert
        var items = await Context.QueueItems
            .Include(i => i.Queue)
            .Where(w => w.Queue.UserId == _user.Id)
            .OrderBy(o => o.Rank)
            .ToListAsync();
        
        items
            .Should()
            .HaveCount(3);
        
        items[0].FileId
            .Should()
            .Be(childC.Id);
        
        items[1].FileId
            .Should()
            .Be(childB.Id);
        
        items[2].FileId
            .Should()
            .Be(childA.Id);
    }

    [Test]
    public async Task SetQueuePositionAsync_ValidItem_SetsPosition()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);
        
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        var queueItem = await Context.QueueItems
            .Include(i => i.Queue)
            .Where(w => w.Queue.UserId == _user.Id)
            .FirstAsync(f => f.FileId == childB.Id);
        
        // Act
        await Subject.SetQueuePositionAsync(_user.Id, childB.Id, CancellationToken.None);
        
        // Assert
        var queue = await Context.Queues
            .Include(i => i.CurrentPosition)
            .SingleAsync(s => s.UserId == _user.Id);
        
        queue.CurrentPositionId
            .Should()
            .Be(queueItem.Id);
        
        queue.CurrentPosition!.FileId
            .Should()
            .Be(childB.Id);
    }
    
    [Test]
    public async Task SetQueuePositionAsync_InvalidItem_Throws()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);
        
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        // Act
        var act = async () => await Subject.SetQueuePositionAsync(_user.Id, Guid.NewGuid(), CancellationToken.None);
        
        // Assert
        await act
            .Should()
            .ThrowAsync<NotFoundException>();
    }
    
    [Test]
    public async Task GetNextPositionAsync_ValidNextItem_ReturnsNextItem()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);
        
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        await Subject.SetQueuePositionAsync(_user.Id, childA.Id, CancellationToken.None);
        
        // Act
        var nextItem = await Subject.GetNextPositionAsync(_user.Id, cancellationToken: CancellationToken.None);
        
        // Assert
        nextItem
            .Should()
            .NotBeNull();
        
        nextItem.FileId
            .Should()
            .Be(childB.Id);
    }
    
    [Test]
    public async Task GetNextPositionAsync_NoCurrentPosition_GetsFirstItem()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);
        
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        // Act
        var nextItem = await Subject.GetNextPositionAsync(_user.Id, cancellationToken: CancellationToken.None);
        
        // Assert
        nextItem
            .Should()
            .NotBeNull();
        
        nextItem.FileId
            .Should()
            .Be(childA.Id);
    }
    
    [Test]
    public async Task GetNextPositionAsync_NoQueue_Throws()
    {
        // Act
        var act = async () => await Subject.GetNextPositionAsync(_user.Id, cancellationToken: CancellationToken.None);
        
        // Assert
        await act
            .Should()
            .ThrowAsync<NotFoundException>();
    }
    
    [Test]
    public async Task GetNextPositionAsync_NoNextItem_ReturnsNull()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);
        
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        await Subject.SetQueuePositionAsync(_user.Id, childC.Id, CancellationToken.None);
        
        // Act
        var nextItem = await Subject.GetNextPositionAsync(_user.Id, cancellationToken: CancellationToken.None);
        
        // Assert
        nextItem
            .Should()
            .BeNull();
    }
    
    [Test]
    public async Task GetNextPositionAsync_ImmediateNextItemDoesNotExist_ReturnsNextExistingItem()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);
        
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        await Subject.SetQueuePositionAsync(_user.Id, childA.Id, CancellationToken.None);
        
        childB.Exists = false;
        await Context.SaveChangesAsync();
        
        // Act
        var nextItem = await Subject.GetNextPositionAsync(_user.Id, cancellationToken: CancellationToken.None);
        
        // Assert
        nextItem
            .Should()
            .NotBeNull();
        
        nextItem.FileId
            .Should()
            .Be(childC.Id);
    }
    
    [Test]
    public async Task GetNextPositionAsync_ImmediateNextIsNotMedia_ReturnsNextMediaItem()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);
        
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        await Subject.SetQueuePositionAsync(_user.Id, childA.Id, CancellationToken.None);
        
        childB.Metadata!.IsMedia = false;
        await Context.SaveChangesAsync();
        
        // Act
        var nextItem = await Subject.GetNextPositionAsync(_user.Id, cancellationToken: CancellationToken.None);
        
        // Assert
        nextItem
            .Should()
            .NotBeNull();
        
        nextItem.FileId
            .Should()
            .Be(childC.Id);
    }
    
    [Test]
    public async Task GetNextPositionAsync_DeviceStatePassed_NextItemInvalidMimeType_ReturnsNextValidMimeType()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);
        
        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);
        
        await Subject.SetQueuePositionAsync(_user.Id, childA.Id, CancellationToken.None);

        var deviceState = new DeviceState(Guid.NewGuid());
        deviceState.UpdateCapabilities(new Dictionary<string, bool>
        {
            { "audio/mp3", true }
        });
        
        childB.Metadata!.MimeType = "audio/wav";
        await Context.SaveChangesAsync();
        
        // Act
        var nextItem = await Subject.GetNextPositionAsync(_user.Id, deviceState, CancellationToken.None);
        
        // Assert
        nextItem
            .Should()
            .NotBeNull();
        
        nextItem.FileId
            .Should()
            .Be(childC.Id);
    }

    [Test]
    public async Task GetNextPositionAsync_MimeTypeNotSupported_HasCompletedTranscode_ReturnsItem()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B", mimeType: "audio/wav");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);

        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);

        await Subject.SetQueuePositionAsync(_user.Id, childA.Id, CancellationToken.None);

        var deviceState = new DeviceState(Guid.NewGuid());
        deviceState.UpdateCapabilities(new Dictionary<string, bool>
        {
            { "audio/mp3", true }
        });

        var transcode = new Transcode
        {
            Id = Guid.NewGuid(),
            NodeEntity = childB,
            NodeIdEntity = childB.Id,
            State = TranscodeState.Completed
        };
        await Context.Transcodes.AddAsync(transcode);
        await Context.SaveChangesAsync();

        // Act
        var nextItem = await Subject.GetNextPositionAsync(_user.Id, deviceState, CancellationToken.None);
        
        // Assert
        nextItem
            .Should()
            .NotBeNull();
        
        nextItem.FileId
            .Should()
            .Be(childB.Id);
    }
    
    [Test]
    public async Task GetNextPositionAsync_MimeTypeNotSupported_TranscodeNotCompleted_SkipsItem()
    {
        // Arrange
        var parentNode = await CreateParentNodeAsync();
        var childA = CreateChildNode(parentNode, "A");
        var childB = CreateChildNode(parentNode, "B", mimeType: "audio/wav");
        var childC = CreateChildNode(parentNode, "C");
        await AddNodesAsync(childC, childA, childB);

        await Subject.SetFolderAsync(_user.Id, parentNode.Id, CancellationToken.None);

        await Subject.SetQueuePositionAsync(_user.Id, childA.Id, CancellationToken.None);

        var deviceState = new DeviceState(Guid.NewGuid());
        deviceState.UpdateCapabilities(new Dictionary<string, bool>
        {
            { "audio/mp3", true }
        });

        var transcode = new Transcode
        {
            Id = Guid.NewGuid(),
            NodeEntity = childB,
            NodeIdEntity = childB.Id,
            State = TranscodeState.InProgress,
        };
        await Context.Transcodes.AddAsync(transcode);
        await Context.SaveChangesAsync();
        
        // Act
        var nextItem = await Subject.GetNextPositionAsync(_user.Id, deviceState, CancellationToken.None);
        
        // Assert
        nextItem
            .Should()
            .NotBeNull();
        
        nextItem.FileId
            .Should()
            .Be(childC.Id);
    }

    private async Task<FileExplorerFolderNodeEntity> CreateParentNodeAsync(string? relativePath = null)
    {
        var parentNode = new FileExplorerFolderNodeEntity
        {
            Id = Guid.NewGuid(),
            RelativePath = relativePath ?? "test",
            Exists = true,
            CreationTimeUtc = DateTime.UtcNow,
            RootChild = await Context.Nodes.OfType<FileExplorerRootChildNodeEntity>().SingleAsync(s => s.Id == _root.Id),
            Parent = null
        };
        await Context.SaveChangesAsync();
        return parentNode;
    }
    
    private FileExplorerFileNodeEntity CreateChildNode(
        FileExplorerFolderNodeEntity parent,
        string? relativePath = null,
        DateTime? creationTime = null,
        string mimeType = "audio/mp3")
    {
        var id = Guid.NewGuid();
        var childNode = new FileExplorerFileNodeEntity
        {
            Id = id,
            RelativePath = relativePath ?? id.ToString(),
            Exists = true,
            CreationTimeUtc = creationTime ?? DateTime.UtcNow,
            RootChild = parent.RootChild,
            Parent = parent,
            Metadata = null
        };

        var metadata = new MediaMetadataEntity
        {
            Bitrate = 128,
            Duration = TimeSpan.FromMinutes(3),
            Id = Guid.NewGuid(),
            MimeType = mimeType,
            IsMedia = true,
            Node = childNode
        };
        
        childNode.Metadata = metadata;

        return childNode;
    }
    
    private async Task AddNodesAsync(params FileExplorerNodeEntityBase[] nodes)
    {
        await Context.FileMetadata.AddRangeAsync(nodes.OfType<FileExplorerFileNodeEntity>()
            .Select(s => s.Metadata)
            .Where(w => w != null)!);
        await Context.Nodes.AddRangeAsync(nodes);
        await Context.SaveChangesAsync();
    }
    
    private async Task<FolderSort> SetUserSortAsync(FileExplorerFolderNodeEntity node, FolderSortMode sortMode, bool descending)
    {
        var sort = new FolderSort
        {
            Id = Guid.NewGuid(),
            UserId = _user.Id,
            SortMode = sortMode,
            Descending = descending,
            NodeEntity = node,
            NodeIdEntity = node.Id
        };
        await Context.FolderSorts.AddAsync(sort);
        await Context.SaveChangesAsync();
        
        return sort;
    }
}