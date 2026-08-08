using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemBroncaAI.Garage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGarageSettingsAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Garages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Complement",
                table: "Garages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Neighborhood",
                table: "Garages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "Garages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Garages",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Garages",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Garages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Garages");

            migrationBuilder.DropColumn(
                name: "Complement",
                table: "Garages");

            migrationBuilder.DropColumn(
                name: "Neighborhood",
                table: "Garages");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Garages");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Garages");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Garages");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Garages");
        }
    }
}
