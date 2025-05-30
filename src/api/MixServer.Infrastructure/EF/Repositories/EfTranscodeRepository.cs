﻿using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Streams.Entities;
using MixServer.Domain.Streams.Repositories;
using MixServer.Infrastructure.EF.Extensions;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfTranscodeRepository(MixServerDbContext context) : ITranscodeRepository
{
    public async Task<Transcode> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Transcodes
                   .IncludeNode()
                   .SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
               ?? throw new NotFoundException(nameof(Transcode), id);
    }

    public Task<Transcode?> GetOrDefaultAsync(NodePath nodePath, CancellationToken cancellationToken)
    {
        return context.Transcodes
            .IncludeNode()
            .SingleOrDefaultAsync(s => 
                s.Node != null &&
                s.Node.RootChild.RelativePath == nodePath.RootPath && s.Node.RelativePath == nodePath.RelativePath, cancellationToken);
    }

    public async Task AddAsync(Transcode transcode, CancellationToken cancellationToken)
    {
        await context.Transcodes.AddAsync(transcode, cancellationToken);
    }

    public void Remove(Guid transcodeId)
    { 
        context.Transcodes.RemoveRange(context.Transcodes.Where(s => s.Id == transcodeId));
    }
}