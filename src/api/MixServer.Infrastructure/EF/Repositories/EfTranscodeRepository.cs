using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
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

    public async Task<Transcode> GetAsync(string fileAbsolutePath)
    {
        return await GetOrDefaultAsync(fileAbsolutePath) ?? throw new NotFoundException(nameof(Transcode), fileAbsolutePath);
    }

    public Task<Transcode?> GetOrDefaultAsync(string fileAbsolutePath)
    {
        return context.Transcodes.SingleOrDefaultAsync(s => s.AbsolutePath == fileAbsolutePath);
    }

    public async Task<Transcode> GetOrAddAsync(string fileAbsolutePath)
    {
        var existing = await GetOrDefaultAsync(fileAbsolutePath);
        
        if (existing is not null)
        {
            return existing;
        }

        var transcode = new Transcode
        {
            Id = Guid.NewGuid(),
            AbsolutePath = fileAbsolutePath,
        };
        await context.Transcodes.AddAsync(transcode);
        
        return transcode;
    }
}