using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MixServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FileSystemInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileSystemNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    Exists = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreationTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 34, nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RootId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Extension = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileSystemNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileSystemNodes_FileSystemNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "FileSystemNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileSystemNodes_FileSystemNodes_RootId",
                        column: x => x.RootId,
                        principalTable: "FileSystemNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileSystemNodes_ParentId",
                table: "FileSystemNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_FileSystemNodes_RootId_RelativePath",
                table: "FileSystemNodes",
                columns: new[] { "RootId", "RelativePath" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileSystemNodes");
        }
    }
}
