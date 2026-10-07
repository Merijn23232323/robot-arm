using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RobotArm.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('Users', 'Role') IS NULL " +
                "ALTER TABLE [Users] ADD [Role] nvarchar(max) NOT NULL CONSTRAINT [DF_Users_Role] DEFAULT N'User'; " +
                "UPDATE [Users] SET [Role] = N'User' WHERE [Role] IS NULL OR [Role] = N'';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('Users', 'Role') IS NOT NULL " +
                "ALTER TABLE [Users] DROP CONSTRAINT [DF_Users_Role]; " +
                "IF COL_LENGTH('Users', 'Role') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [Role];");
        }
    }
}
