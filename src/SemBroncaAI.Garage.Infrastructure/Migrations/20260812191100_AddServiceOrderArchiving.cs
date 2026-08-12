using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemBroncaAI.Garage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOrderArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_GarageId",
                table: "ServiceOrders");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "ServiceOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_GarageId_ArchivedAt",
                table: "ServiceOrders",
                columns: new[] { "GarageId", "ArchivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_GarageId_ArchivedAt",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ServiceOrders");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_GarageId",
                table: "ServiceOrders",
                column: "GarageId");
        }
    }
}
