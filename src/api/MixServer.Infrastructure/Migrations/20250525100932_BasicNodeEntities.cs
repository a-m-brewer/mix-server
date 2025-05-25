using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MixServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BasicNodeEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transcodes_AbsolutePath",
                table: "Transcodes");

            migrationBuilder.AddColumn<Guid>(
                name: "NodeId",
                table: "Transcodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NodeId",
                table: "PlaybackSessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NodeId",
                table: "FolderSorts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    NodeType = table.Column<int>(type: "INTEGER", nullable: false),
                    RootChildId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nodes_Nodes_RootChildId",
                        column: x => x.RootChildId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transcodes_NodeId",
                table: "Transcodes",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackSessions_NodeId",
                table: "PlaybackSessions",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderSorts_NodeId",
                table: "FolderSorts",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_RootChildId_RelativePath",
                table: "Nodes",
                columns: new[] { "RootChildId", "RelativePath" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FolderSorts_Nodes_NodeId",
                table: "FolderSorts",
                column: "NodeId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaybackSessions_Nodes_NodeId",
                table: "PlaybackSessions",
                column: "NodeId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transcodes_Nodes_NodeId",
                table: "Transcodes",
                column: "NodeId",
                principalTable: "Nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FolderSorts_Nodes_NodeId",
                table: "FolderSorts");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaybackSessions_Nodes_NodeId",
                table: "PlaybackSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transcodes_Nodes_NodeId",
                table: "Transcodes");

            migrationBuilder.DropTable(
                name: "Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Transcodes_NodeId",
                table: "Transcodes");

            migrationBuilder.DropIndex(
                name: "IX_PlaybackSessions_NodeId",
                table: "PlaybackSessions");

            migrationBuilder.DropIndex(
                name: "IX_FolderSorts_NodeId",
                table: "FolderSorts");

            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "Transcodes");

            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "PlaybackSessions");

            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "FolderSorts");

            migrationBuilder.CreateIndex(
                name: "IX_Transcodes_AbsolutePath",
                table: "Transcodes",
                column: "AbsolutePath",
                unique: true);
        }
    }
}
