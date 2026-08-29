using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemBroncaAI.Garage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PilotReadyApprovalEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_GarageId_Document",
                table: "Customers");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedItemIdsJson",
                table: "ServiceOrderEstimateApprovals",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedTotal",
                table: "ServiceOrderEstimateApprovals",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientIp",
                table: "ServiceOrderEstimateApprovals",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerDocument",
                table: "ServiceOrderEstimateApprovals",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "ServiceOrderEstimateApprovals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimateSnapshotJson",
                table: "ServiceOrderEstimateApprovals",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "ServiceOrderEstimateApprovals",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorizationStatus",
                table: "ServiceOrderEstimateItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "ServiceOrderEstimateItems" AS item
                SET "AuthorizationStatus" = CASE
                    WHEN service_order."DigitalApprovalWaivedAt" IS NOT NULL THEN 3
                    WHEN EXISTS (
                        SELECT 1 FROM "ServiceOrderEstimateApprovals" AS approval
                        WHERE approval."ServiceOrderId" = service_order."Id"
                          AND approval."InvalidatedAt" IS NULL
                          AND approval."Status" = 2) THEN 1
                    WHEN EXISTS (
                        SELECT 1 FROM "ServiceOrderEstimateApprovals" AS approval
                        WHERE approval."ServiceOrderId" = service_order."Id"
                          AND approval."InvalidatedAt" IS NULL
                          AND approval."Status" = 3) THEN 2
                    ELSE 0
                END
                FROM "ServiceOrderEstimates" AS estimate
                INNER JOIN "ServiceOrders" AS service_order ON service_order."Id" = estimate."ServiceOrderId"
                WHERE item."EstimateId" = estimate."Id";
                """);

            migrationBuilder.Sql(
                "ALTER TABLE \"ServiceOrderEstimateApprovals\" ALTER COLUMN \"EstimateSnapshotJson\" DROP DEFAULT;");

            migrationBuilder.Sql(
                "ALTER TABLE \"ServiceOrderEstimateItems\" ALTER COLUMN \"AuthorizationStatus\" DROP DEFAULT;");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_GarageId_Document",
                table: "Customers",
                columns: new[] { "GarageId", "Document" },
                unique: true,
                filter: "\"Document\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_GarageId_Document",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ApprovedItemIdsJson",
                table: "ServiceOrderEstimateApprovals");

            migrationBuilder.DropColumn(
                name: "AuthorizationStatus",
                table: "ServiceOrderEstimateItems");

            migrationBuilder.DropColumn(
                name: "ApprovedTotal",
                table: "ServiceOrderEstimateApprovals");

            migrationBuilder.DropColumn(
                name: "ClientIp",
                table: "ServiceOrderEstimateApprovals");

            migrationBuilder.DropColumn(
                name: "CustomerDocument",
                table: "ServiceOrderEstimateApprovals");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "ServiceOrderEstimateApprovals");

            migrationBuilder.DropColumn(
                name: "EstimateSnapshotJson",
                table: "ServiceOrderEstimateApprovals");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "ServiceOrderEstimateApprovals");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_GarageId_Document",
                table: "Customers",
                columns: new[] { "GarageId", "Document" },
                unique: true);
        }
    }
}
