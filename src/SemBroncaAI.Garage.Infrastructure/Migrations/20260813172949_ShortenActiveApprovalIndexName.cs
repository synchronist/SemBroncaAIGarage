using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemBroncaAI.Garage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ShortenActiveApprovalIndexName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_ServiceOrderEstimateApprovals_ServiceOrderId_EstimateUpdatedAt_ActivePending",
                table: "ServiceOrderEstimateApprovals",
                newName: "UX_Approval_ActivePendingVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "UX_Approval_ActivePendingVersion",
                table: "ServiceOrderEstimateApprovals",
                newName: "IX_ServiceOrderEstimateApprovals_ServiceOrderId_EstimateUpdatedAt_ActivePending");
        }
    }
}
