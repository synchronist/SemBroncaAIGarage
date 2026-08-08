using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemBroncaAI.Garage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOrderEstimate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceOrderEstimates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderEstimates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderEstimates_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOrderEstimateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderEstimateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderEstimateItems_ServiceOrderEstimates_EstimateId",
                        column: x => x.EstimateId,
                        principalTable: "ServiceOrderEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderEstimateItems_EstimateId",
                table: "ServiceOrderEstimateItems",
                column: "EstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderEstimates_ServiceOrderId",
                table: "ServiceOrderEstimates",
                column: "ServiceOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceOrderEstimateItems");

            migrationBuilder.DropTable(
                name: "ServiceOrderEstimates");
        }
    }
}
