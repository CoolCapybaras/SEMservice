using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropRolesUseEventRoleOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Один пользователь — одна строка на мероприятие; иначе PK (EventId, UserId) не применить.
            migrationBuilder.Sql("""
                DELETE FROM "EventRoles" er
                WHERE er.ctid NOT IN (
                    SELECT MAX(er_grp.ctid)
                    FROM "EventRoles" AS er_grp
                    GROUP BY er_grp."EventId", er_grp."UserId"
                );
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_EventRoles_Roles_RoleId",
                table: "EventRoles");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventRoles",
                table: "EventRoles");

            migrationBuilder.DropIndex(
                name: "IX_EventRoles_RoleId",
                table: "EventRoles");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "EventRoles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventRoles",
                table: "EventRoles",
                columns: new[] { "EventId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EventRoles",
                table: "EventRoles");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "EventRoles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventRoles",
                table: "EventRoles",
                columns: new[] { "EventId", "UserId", "RoleId" });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventRoles_RoleId",
                table: "EventRoles",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventRoles_Roles_RoleId",
                table: "EventRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
