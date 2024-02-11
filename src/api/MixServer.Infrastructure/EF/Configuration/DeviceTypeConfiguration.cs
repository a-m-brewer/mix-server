using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Users.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class DeviceTypeConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder
            .HasMany(m => m.UserCredentials)
            .WithOne(o => o.Device)
            .HasForeignKey(f => f.DeviceId);
    }
}