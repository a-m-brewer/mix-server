using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MixServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CurrentSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentPlaybackSessionId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CurrentPlaybackSessionId",
                table: "AspNetUsers",
                column: "CurrentPlaybackSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_PlaybackSessions_CurrentPlaybackSessionId",
                table: "AspNetUsers",
                column: "CurrentPlaybackSessionId",
                principalTable: "PlaybackSessions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_PlaybackSessions_CurrentPlaybackSessionId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CurrentPlaybackSessionId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CurrentPlaybackSessionId",
                table: "AspNetUsers");
        }
    }
}
