using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Streams.Entities;
using MixServer.Domain.Streams.Repositories;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfTranscodeRepository(MixServerDbContext context) : ITranscodeRepository
{
    public async Task<Transcode> GetAsync(Guid id)
    {
        return await context.Transcodes.SingleOrDefaultAsync(s => s.Id == id)
               ?? throw new NotFoundException(nameof(Transcode), id);
    }

    public async Task<Transcode> GetAsync(NodePath nodePath)
    {
        return await GetOrDefaultAsync(nodePath) ?? throw new NotFoundException(nameof(Transcode), nodePath.AbsolutePath);
    }

    public Task<Transcode?> GetOrDefaultAsync(NodePath nodePath)
    {
        return context.Transcodes.SingleOrDefaultAsync(s => s.AbsolutePath == nodePath.AbsolutePath);
    }

    public async Task<Transcode> GetOrAddAsync(NodePath nodePath)
    {
        var existing = await GetOrDefaultAsync(nodePath);
        
        if (existing is not null)
        {
            return existing;
        }

        var transcode = new Transcode
        {
            Id = Guid.NewGuid(),
            AbsolutePath = nodePath.AbsolutePath,
        };
        await context.Transcodes.AddAsync(transcode);
        
        return transcode;
    }
}