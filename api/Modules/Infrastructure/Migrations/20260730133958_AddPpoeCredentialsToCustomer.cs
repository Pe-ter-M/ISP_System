using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternetProvider.Api.Modules.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPpoeCredentialsToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_ppoe",
                table: "customers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "username_ppoe",
                table: "customers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_ppoe",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "username_ppoe",
                table: "customers");
        }
    }
}
