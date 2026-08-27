using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemBroncaAI.Garage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeBillingIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingCustomerId",
                table: "GarageSubscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingPriceId",
                table: "GarageSubscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingSubscriptionId",
                table: "GarageSubscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CancelAtPeriodEnd",
                table: "GarageSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ProcessedBillingEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedBillingEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GarageSubscriptions_BillingCustomerId",
                table: "GarageSubscriptions",
                column: "BillingCustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GarageSubscriptions_BillingSubscriptionId",
                table: "GarageSubscriptions",
                column: "BillingSubscriptionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedBillingEvents");

            migrationBuilder.DropIndex(
                name: "IX_GarageSubscriptions_BillingCustomerId",
                table: "GarageSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_GarageSubscriptions_BillingSubscriptionId",
                table: "GarageSubscriptions");

            migrationBuilder.DropColumn(
                name: "BillingCustomerId",
                table: "GarageSubscriptions");

            migrationBuilder.DropColumn(
                name: "BillingPriceId",
                table: "GarageSubscriptions");

            migrationBuilder.DropColumn(
                name: "BillingSubscriptionId",
                table: "GarageSubscriptions");

            migrationBuilder.DropColumn(
                name: "CancelAtPeriodEnd",
                table: "GarageSubscriptions");
        }
    }
}
