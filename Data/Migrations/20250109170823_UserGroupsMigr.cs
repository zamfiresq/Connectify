using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Connectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserGroupsMigr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAccepted",
                table: "UserGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAccepted",
                table: "UserGroups");
        }
    }
}
