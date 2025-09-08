using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MixServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Queueing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Transcodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "QueueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Rank = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    QueueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueItems_Nodes_FileId",
                        column: x => x.FileId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QueueItems_QueueItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "QueueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Queues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentPositionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CurrentFolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CurrentRootChildId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Queues_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Queues_Nodes_CurrentFolderId",
                        column: x => x.CurrentFolderId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Queues_Nodes_CurrentRootChildId",
                        column: x => x.CurrentRootChildId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Queues_QueueItems_CurrentPositionId",
                        column: x => x.CurrentPositionId,
                        principalTable: "QueueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueueItems_FileId",
                table: "QueueItems",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueItems_ParentId",
                table: "QueueItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueItems_QueueId_Rank",
                table: "QueueItems",
                columns: new[] { "QueueId", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_Queues_CurrentFolderId",
                table: "Queues",
                column: "CurrentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_CurrentPositionId",
                table: "Queues",
                column: "CurrentPositionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Queues_CurrentRootChildId",
                table: "Queues",
                column: "CurrentRootChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_UserId",
                table: "Queues",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueItems_Queues_QueueId",
                table: "QueueItems",
                column: "QueueId",
                principalTable: "Queues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QueueItems_Queues_QueueId",
                table: "QueueItems");

            migrationBuilder.DropTable(
                name: "Queues");

            migrationBuilder.DropTable(
                name: "QueueItems");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Transcodes");
        }
    }
}
