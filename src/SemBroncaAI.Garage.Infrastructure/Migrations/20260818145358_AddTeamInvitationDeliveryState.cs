using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemBroncaAI.Garage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamInvitationDeliveryState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryStatus",
                table: "TeamInvitations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Sent");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDeliveryAttemptAt",
                table: "TeamInvitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "TeamInvitations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "TeamInvitations");

            migrationBuilder.DropColumn(
                name: "LastDeliveryAttemptAt",
                table: "TeamInvitations");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "TeamInvitations");
        }
    }
}
