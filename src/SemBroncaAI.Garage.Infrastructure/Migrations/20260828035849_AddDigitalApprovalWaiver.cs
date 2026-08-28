using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemBroncaAI.Garage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDigitalApprovalWaiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DigitalApprovalWaivedAt",
                table: "ServiceOrders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DigitalApprovalWaivedAt",
                table: "ServiceOrders");
        }
    }
}
