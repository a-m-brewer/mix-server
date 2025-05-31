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
                name: "FileMetadataEntity",
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
                    table.PrimaryKey("PK_FileMetadataEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileMetadataEntity_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TracklistEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NodeId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TracklistEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TracklistEntity_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CueEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cue = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    TracklistId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CueEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CueEntity_TracklistEntity_TracklistId",
                        column: x => x.TracklistId,
                        principalTable: "TracklistEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Artist = table.Column<string>(type: "TEXT", nullable: false),
                    CueId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackEntity_CueEntity_CueId",
                        column: x => x.CueId,
                        principalTable: "CueEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TracklistPlayersEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    TrackId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TracklistPlayersEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TracklistPlayersEntity_TrackEntity_TrackId",
                        column: x => x.TrackId,
                        principalTable: "TrackEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_ParentId",
                table: "Nodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_CueEntity_TracklistId",
                table: "CueEntity",
                column: "TracklistId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadataEntity_NodeId",
                table: "FileMetadataEntity",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackEntity_CueId",
                table: "TrackEntity",
                column: "CueId");

            migrationBuilder.CreateIndex(
                name: "IX_TracklistEntity_NodeId",
                table: "TracklistEntity",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TracklistPlayersEntity_TrackId",
                table: "TracklistPlayersEntity",
                column: "TrackId");

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
                name: "FileMetadataEntity");

            migrationBuilder.DropTable(
                name: "TracklistPlayersEntity");

            migrationBuilder.DropTable(
                name: "TrackEntity");

            migrationBuilder.DropTable(
                name: "CueEntity");

            migrationBuilder.DropTable(
                name: "TracklistEntity");

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
