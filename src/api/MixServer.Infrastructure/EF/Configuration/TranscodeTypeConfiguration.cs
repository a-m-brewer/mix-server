﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Streams.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class TranscodeTypeConfiguration : IEntityTypeConfiguration<Transcode>
{
    public void Configure(EntityTypeBuilder<Transcode> builder)
    {
        builder.HasKey(k => k.Id);
        
        builder.HasIndex(i => i.AbsolutePath).IsUnique();
    }
}