using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BoardTaskPriorityAndAssigneeNav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "BoardTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BoardTasks_AssignedUserId",
                table: "BoardTasks",
                column: "AssignedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardTasks_Users_AssignedUserId",
                table: "BoardTasks",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardTasks_Users_AssignedUserId",
                table: "BoardTasks");

            migrationBuilder.DropIndex(
                name: "IX_BoardTasks_AssignedUserId",
                table: "BoardTasks");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "BoardTasks");
        }
    }
}
