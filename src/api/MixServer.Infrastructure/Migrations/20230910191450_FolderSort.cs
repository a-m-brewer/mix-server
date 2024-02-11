using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MixServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FolderSort : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FolderSorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AbsoluteFolderPath = table.Column<string>(type: "TEXT", nullable: false),
                    Descending = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortMode = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderSorts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FolderSorts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FolderSorts_UserId",
                table: "FolderSorts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FolderSorts");
        }
    }
}
