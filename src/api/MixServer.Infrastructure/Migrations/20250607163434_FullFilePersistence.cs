using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MixServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FullFilePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreationTimeUtc",
                table: "Nodes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "Exists",
                table: "Nodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "Nodes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Hidden",
                table: "Nodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "Nodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FileMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", nullable: false),
                    IsMedia = table.Column<bool>(type: "INTEGER", nullable: false),
                    NodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Bitrate = table.Column<int>(type: "INTEGER", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileMetadata_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tracklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NodeId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tracklists_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cue = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    TracklistId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cues_Tracklists_TracklistId",
                        column: x => x.TracklistId,
                        principalTable: "Tracklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Artist = table.Column<string>(type: "TEXT", nullable: false),
                    CueId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tracks_Cues_CueId",
                        column: x => x.CueId,
                        principalTable: "Cues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TracklistPlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    TrackId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TracklistPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TracklistPlayers_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_ParentId",
                table: "Nodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Cues_TracklistId",
                table: "Cues",
                column: "TracklistId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_NodeId",
                table: "FileMetadata",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TracklistPlayers_TrackId",
                table: "TracklistPlayers",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracklists_NodeId",
                table: "Tracklists",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_CueId",
                table: "Tracks",
                column: "CueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_Nodes_ParentId",
                table: "Nodes",
                column: "ParentId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_Nodes_ParentId",
                table: "Nodes");

            migrationBuilder.DropTable(
                name: "FileMetadata");

            migrationBuilder.DropTable(
                name: "TracklistPlayers");

            migrationBuilder.DropTable(
                name: "Tracks");

            migrationBuilder.DropTable(
                name: "Cues");

            migrationBuilder.DropTable(
                name: "Tracklists");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_ParentId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "CreationTimeUtc",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "Exists",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "Hash",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "Hidden",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Nodes");
        }
    }
}
