using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternetProvider.Api.Modules.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationContactFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "organization",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "support_email",
                table: "organization",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "support_phone",
                table: "organization",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "address",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "support_email",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "support_phone",
                table: "organization");
        }
    }
}
