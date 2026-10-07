using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RobotArm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Robots",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.Sql(
                "SET IDENTITY_INSERT [Users] ON; " +
                "INSERT INTO [Users] ([Id], [Username], [Email], [CreatedAt], [Role]) " +
                "VALUES (1, 'system', 'system@robotarm.local', '2026-01-01T00:00:00', 'User'); " +
                "SET IDENTITY_INSERT [Users] OFF;");

            migrationBuilder.CreateIndex(
                name: "IX_Robots_UserId",
                table: "Robots",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Users_UserId",
                table: "Robots",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Users_UserId",
                table: "Robots");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Robots_UserId",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Robots");
        }
    }
}
