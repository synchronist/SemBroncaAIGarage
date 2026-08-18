using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemBroncaAI.Garage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenServiceOrderConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceOrderEstimateApprovals_ServiceOrderId",
                table: "ServiceOrderEstimateApprovals");

            migrationBuilder.CreateTable(
                name: "ServiceOrderNumberSequences",
                columns: table => new
                {
                    GarageId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderNumberSequences", x => x.GarageId);
                    table.ForeignKey(
                        name: "FK_ServiceOrderNumberSequences_Garages_GarageId",
                        column: x => x.GarageId,
                        principalTable: "Garages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_GarageId_Number",
                table: "ServiceOrders",
                columns: new[] { "GarageId", "Number" },
                unique: true);

            migrationBuilder.Sql(
                """
                WITH ranked AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "ServiceOrderId", "EstimateUpdatedAt"
                               ORDER BY "CreatedAt" DESC, "Id" DESC) AS position
                    FROM "ServiceOrderEstimateApprovals"
                    WHERE "Status" = 1 AND "InvalidatedAt" IS NULL
                )
                UPDATE "ServiceOrderEstimateApprovals" AS approval
                SET "InvalidatedAt" = CURRENT_TIMESTAMP
                FROM ranked
                WHERE approval."Id" = ranked."Id" AND ranked.position > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderEstimateApprovals_ServiceOrderId_EstimateUpdatedAt_ActivePending",
                table: "ServiceOrderEstimateApprovals",
                columns: new[] { "ServiceOrderId", "EstimateUpdatedAt" },
                unique: true,
                filter: "\"Status\" = 1 AND \"InvalidatedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceOrderNumberSequences");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_GarageId_Number",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrderEstimateApprovals_ServiceOrderId_EstimateUpdatedAt_ActivePending",
                table: "ServiceOrderEstimateApprovals");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderEstimateApprovals_ServiceOrderId",
                table: "ServiceOrderEstimateApprovals",
                column: "ServiceOrderId");
        }
    }
}
