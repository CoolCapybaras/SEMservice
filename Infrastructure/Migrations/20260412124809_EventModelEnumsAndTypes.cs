using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EventModelEnumsAndTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Events",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Auditorium",
                table: "Events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LifecycleState",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "VenueFormat",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParticipantRole",
                table: "EventRoles",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.Sql("""
                UPDATE "Events" SET "LifecycleState" = CASE
                    WHEN UPPER(TRIM(COALESCE(status, ''))) IN ('FINISHED', 'COMPLETED', 'ЗАВЕРШЕНО') THEN 2
                    WHEN UPPER(TRIM(COALESCE(status, ''))) IN ('CANCELLED', 'ОТМЕНЕНО') THEN 3
                    WHEN LOWER(TRIM(COALESCE(status, ''))) IN ('draft', 'черновик') THEN 0
                    ELSE 1 END;
                """);

            migrationBuilder.Sql("""
                UPDATE "Events" SET "VenueFormat" = CASE
                    WHEN LOWER(TRIM(COALESCE("Format", ''))) LIKE '%онлайн%' OR LOWER(TRIM("Format")) = 'online' THEN 1
                    WHEN LOWER(TRIM(COALESCE("Format", ''))) LIKE '%гибрид%' OR LOWER(TRIM("Format")) = 'hybrid' THEN 2
                    ELSE 0 END;
                """);

            migrationBuilder.Sql("""
                UPDATE "Roles" SET "Name" = 'Наблюдатель' WHERE "Name" = 'Участник';
                """);

            migrationBuilder.Sql("""
                UPDATE "EventRoles" er SET "ParticipantRole" = CASE r."Name"
                    WHEN 'Организатор' THEN 0
                    WHEN 'Редактор' THEN 1
                    WHEN 'Помощник' THEN 2
                    WHEN 'Наблюдатель' THEN 3
                    ELSE 3 END
                FROM "Roles" r WHERE er."RoleId" = r."Id";
                """);

            migrationBuilder.CreateTable(
                name: "EventSelectedTypes",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeKind = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSelectedTypes", x => new { x.EventId, x.TypeKind });
                    table.ForeignKey(
                        name: "FK_EventSelectedTypes_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "EventSelectedTypes" ("EventId", "TypeKind")
                SELECT e."Id",
                    CASE
                        WHEN LOWER(TRIM(COALESCE(e."EventType", ''))) LIKE '%хакатон%' OR LOWER(e."EventType") LIKE '%hackathon%' THEN 0
                        WHEN LOWER(TRIM(COALESCE(e."EventType", ''))) LIKE '%лекци%' OR LOWER(e."EventType") LIKE '%lecture%' THEN 1
                        WHEN LOWER(TRIM(COALESCE(e."EventType", ''))) LIKE '%вебинар%' OR LOWER(e."EventType") LIKE '%webinar%' THEN 2
                        WHEN LOWER(TRIM(COALESCE(e."EventType", ''))) LIKE '%урфу%' THEN 3
                        WHEN LOWER(TRIM(COALESCE(e."EventType", ''))) IN ('пп', 'pp') THEN 4
                        WHEN LOWER(e."EventType") LIKE '%спецкурс%' OR LOWER(e."EventType") LIKE '%special%' THEN 5
                        WHEN LOWER(e."EventType") LIKE '%практик%' OR LOWER(e."EventType") LIKE '%practice%' THEN 6
                        ELSE 1
                    END
                FROM "Events" e
                ON CONFLICT ("EventId", "TypeKind") DO NOTHING;
                """);

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "status",
                table: "Events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Format",
                table: "Events",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Events",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Events" SET status = CASE "LifecycleState"
                    WHEN 0 THEN 'DRAFT'
                    WHEN 1 THEN 'ACTIVE'
                    WHEN 2 THEN 'FINISHED'
                    WHEN 3 THEN 'CANCELLED'
                    ELSE 'ACTIVE' END;
                """);

            migrationBuilder.Sql("""
                UPDATE "Events" SET "Format" = CASE "VenueFormat"
                    WHEN 1 THEN 'Онлайн'
                    WHEN 2 THEN 'Гибрид'
                    ELSE 'Очно' END;
                """);

            migrationBuilder.Sql("""
                UPDATE "Events" e SET "EventType" = COALESCE((
                    SELECT CASE est."TypeKind"
                        WHEN 0 THEN 'Хакатон'
                        WHEN 1 THEN 'Лекция'
                        WHEN 2 THEN 'Вебинар'
                        WHEN 3 THEN 'УрФУ'
                        WHEN 4 THEN 'ПП'
                        WHEN 5 THEN 'Спецкурс'
                        WHEN 6 THEN 'Практика'
                        ELSE 'Лекция' END
                    FROM "EventSelectedTypes" est WHERE est."EventId" = e."Id" ORDER BY est."TypeKind" LIMIT 1
                ), 'Лекция');
                """);

            migrationBuilder.DropTable(
                name: "EventSelectedTypes");

            migrationBuilder.DropColumn(
                name: "ParticipantRole",
                table: "EventRoles");

            migrationBuilder.DropColumn(
                name: "Auditorium",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LifecycleState",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "VenueFormat",
                table: "Events");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4096)",
                oldMaxLength: 4096,
                oldNullable: true);
        }
    }
}
