using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChatNewFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReplyToMessageId",
                table: "EventChatMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "EventChatMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventChatMessages_ReplyToMessageId",
                table: "EventChatMessages",
                column: "ReplyToMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventChatMessages_EventChatMessages_ReplyToMessageId",
                table: "EventChatMessages",
                column: "ReplyToMessageId",
                principalTable: "EventChatMessages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventChatMessages_EventChatMessages_ReplyToMessageId",
                table: "EventChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_EventChatMessages_ReplyToMessageId",
                table: "EventChatMessages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                table: "EventChatMessages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "EventChatMessages");
        }
    }
}
