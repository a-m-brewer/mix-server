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
                name: "FileSystemRoots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AbsolutePath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileSystemRoots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileSystemNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    AbsolutePath = table.Column<string>(type: "TEXT", nullable: false),
                    Exists = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreationTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    FileSystemRootId = table.Column<Guid>(type: "TEXT", nullable: true),
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
                        name: "FK_FileSystemNodes_FileSystemRoots_FileSystemRootId",
                        column: x => x.FileSystemRootId,
                        principalTable: "FileSystemRoots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileSystemNodes_AbsolutePath",
                table: "FileSystemNodes",
                column: "AbsolutePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileSystemNodes_FileSystemRootId",
                table: "FileSystemNodes",
                column: "FileSystemRootId");

            migrationBuilder.CreateIndex(
                name: "IX_FileSystemNodes_ParentId",
                table: "FileSystemNodes",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileSystemNodes");

            migrationBuilder.DropTable(
                name: "FileSystemRoots");
        }
    }
}
