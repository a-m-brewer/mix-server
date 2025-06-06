﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MixServer.Infrastructure.EF;

#nullable disable

namespace MixServer.Infrastructure.Migrations
{
    [DbContext(typeof(MixServerDbContext))]
    partial class MixServerDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.1");

            modelBuilder.Entity("DbUserDevice", b =>
                {
                    b.Property<string>("DbUserId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("DevicesId")
                        .HasColumnType("TEXT");

                    b.HasKey("DbUserId", "DevicesId");

                    b.HasIndex("DevicesId");

                    b.ToTable("DbUserDevice");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClaimType")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClaimType")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleId")
                        .HasColumnType("TEXT");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("MixServer.Domain.FileExplorer.Entities.FileExplorerNodeEntityBase", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("NodeType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RelativePath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Nodes");

                    b.HasDiscriminator<int>("NodeType").HasValue(0);

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("MixServer.Domain.FileExplorer.Entities.FolderSort", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AbsoluteFolderPath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("Descending")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("NodeId")
                        .HasColumnType("TEXT");

                    b.Property<int>("SortMode")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NodeId");

                    b.HasIndex("UserId");

                    b.ToTable("FolderSorts");
                });

            modelBuilder.Entity("MixServer.Domain.Sessions.Entities.PlaybackSession", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AbsolutePath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan>("CurrentTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastPlayed")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("NodeId")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NodeId");

                    b.HasIndex("UserId");

                    b.ToTable("PlaybackSessions");
                });

            modelBuilder.Entity("MixServer.Domain.Streams.Entities.Transcode", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AbsolutePath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("NodeId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NodeId")
                        .IsUnique();

                    b.ToTable("Transcodes");
                });

            modelBuilder.Entity("MixServer.Domain.Users.Entities.Device", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Brand")
                        .HasColumnType("TEXT");

                    b.Property<string>("BrowserName")
                        .HasColumnType("TEXT");

                    b.Property<int>("ClientType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DeviceType")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastSeen")
                        .HasColumnType("TEXT");

                    b.Property<string>("Model")
                        .HasColumnType("TEXT");

                    b.Property<string>("OsName")
                        .HasColumnType("TEXT");

                    b.Property<string>("OsVersion")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("MixServer.Domain.Users.Entities.UserCredential", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AccessToken")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("DeviceId")
                        .HasColumnType("TEXT");

                    b.Property<string>("RefreshToken")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DeviceId");

                    b.HasIndex("UserId");

                    b.ToTable("UserCredentials");
                });

            modelBuilder.Entity("MixServer.Infrastructure.EF.Entities.DbUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("CurrentPlaybackSessionId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("TEXT");

                    b.Property<bool>("PasswordResetRequired")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("TEXT");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("TEXT");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CurrentPlaybackSessionId");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("MixServer.Domain.FileExplorer.Entities.FileExplorerNodeEntity", b =>
                {
                    b.HasBaseType("MixServer.Domain.FileExplorer.Entities.FileExplorerNodeEntityBase");

                    b.Property<Guid>("RootChildId")
                        .HasColumnType("TEXT");

                    b.HasIndex("RootChildId", "RelativePath")
                        .IsUnique();

                    b.HasDiscriminator().HasValue(1);
                });

            modelBuilder.Entity("MixServer.Domain.FileExplorer.Entities.FileExplorerRootChildNodeEntity", b =>
                {
                    b.HasBaseType("MixServer.Domain.FileExplorer.Entities.FileExplorerNodeEntityBase");

                    b.HasDiscriminator().HasValue(4);
                });

            modelBuilder.Entity("MixServer.Domain.FileExplorer.Entities.FileExplorerFileNodeEntity", b =>
                {
                    b.HasBaseType("MixServer.Domain.FileExplorer.Entities.FileExplorerNodeEntity");

                    b.HasDiscriminator().HasValue(2);
                });

            modelBuilder.Entity("MixServer.Domain.FileExplorer.Entities.FileExplorerFolderNodeEntity", b =>
                {
                    b.HasBaseType("MixServer.Domain.FileExplorer.Entities.FileExplorerNodeEntity");

                    b.HasDiscriminator().HasValue(3);
                });

            modelBuilder.Entity("DbUserDevice", b =>
                {
                    b.HasOne("MixServer.Infrastructure.EF.Entities.DbUser", null)
                        .WithMany()
                        .HasForeignKey("DbUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MixServer.Domain.Users.Entities.Device", null)
                        .WithMany()
                        .HasForeignKey("DevicesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("MixServer.Infrastructure.EF.Entities.DbUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("MixServer.Infrastructure.EF.Entities.DbUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MixServer.Infrastructure.EF.Entities.DbUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("MixServer.Infrastructure.EF.Entities.DbUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MixServer.Domain.FileExplorer.Entities.FolderSort", b =>
                {
                    b.HasOne("MixServer.Domain.FileExplorer.Entities.FileExplorerFolderNodeEntity", "Node")
                        .WithMany()
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MixServer.Infrastructure.EF.Entities.DbUser", null)
                        .WithMany("FolderSorts")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Node");
                });

            modelBuilder.Entity("MixServer.Domain.Sessions.Entities.PlaybackSession", b =>
                {
                    b.HasOne("MixServer.Domain.FileExplorer.Entities.FileExplorerFileNodeEntity", "Node")
                        .WithMany()
                        .HasForeignKey("NodeId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MixServer.Infrastructure.EF.Entities.DbUser", null)
                        .WithMany("PlaybackSessions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Node");
                });

            modelBuilder.Entity("MixServer.Domain.Streams.Entities.Transcode", b =>
                {
                    b.HasOne("MixServer.Domain.FileExplorer.Entities.FileExplorerFileNodeEntity", "Node")
                        .WithOne("Transcode")
                        .HasForeignKey("MixServer.Domain.Streams.Entities.Transcode", "NodeId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Node");
                });

            modelBuilder.Entity("MixServer.Domain.Users.Entities.UserCredential", b =>
                {
                    b.HasOne("MixServer.Domain.Users.Entities.Device", "Device")
                        .WithMany("UserCredentials")
                        .HasForeignKey("DeviceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MixServer.Infrastructure.EF.Entities.DbUser", null)
                        .WithMany("Credentials")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Device");
                });

            modelBuilder.Entity("MixServer.Infrastructure.EF.Entities.DbUser", b =>
                {
                    b.HasOne("MixServer.Domain.Sessions.Entities.PlaybackSession", "CurrentPlaybackSession")
                        .WithMany()
                        .HasForeignKey("CurrentPlaybackSessionId");

                    b.Navigation("CurrentPlaybackSession");
                });

            modelBuilder.Entity("MixServer.Domain.FileExplorer.Entities.FileExplorerNodeEntity", b =>
                {
                    b.HasOne("MixServer.Domain.FileExplorer.Entities.FileExplorerRootChildNodeEntity", "RootChild")
                        .WithMany()
                        .HasForeignKey("RootChildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("RootChild");
                });

            modelBuilder.Entity("MixServer.Domain.Users.Entities.Device", b =>
                {
                    b.Navigation("UserCredentials");
                });

            modelBuilder.Entity("MixServer.Infrastructure.EF.Entities.DbUser", b =>
                {
                    b.Navigation("Credentials");

                    b.Navigation("FolderSorts");

                    b.Navigation("PlaybackSessions");
                });

            modelBuilder.Entity("MixServer.Domain.FileExplorer.Entities.FileExplorerFileNodeEntity", b =>
                {
                    b.Navigation("Transcode");
                });
#pragma warning restore 612, 618
        }
    }
}
