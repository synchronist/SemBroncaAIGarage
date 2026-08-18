using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemBroncaAI.Garage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGarageSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GarageSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GarageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Plan = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GarageSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GarageSubscriptions_Garages_GarageId",
                        column: x => x.GarageId,
                        principalTable: "Garages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Existing customers are not retroactively placed in trial. Their operational state
            // is mapped to the safest equivalent commercial state.
            migrationBuilder.Sql(
                """
                INSERT INTO "GarageSubscriptions"
                    ("Id", "GarageId", "Status", "Plan", "StartedAt", "TrialEndsAt",
                     "CurrentPeriodStart", "CurrentPeriodEnd", "SuspendedAt", "CancelledAt",
                     "CreatedAt", "UpdatedAt")
                SELECT "Id", "Id", CASE WHEN "Active" THEN 'Active' ELSE 'Suspended' END,
                       'Standard', "CreatedAt", NULL, NULL, NULL,
                       CASE WHEN "Active" THEN NULL ELSE CURRENT_TIMESTAMP END, NULL,
                       CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM "Garages";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_GarageSubscriptions_GarageId",
                table: "GarageSubscriptions",
                column: "GarageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GarageSubscriptions");
        }
    }
}
