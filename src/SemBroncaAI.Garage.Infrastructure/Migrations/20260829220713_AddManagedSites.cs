using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SemBroncaAI.Garage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedSites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagedSites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TradeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Document = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    WhatsApp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ProjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SiteType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Scope = table.Column<string>(type: "text", nullable: true),
                    ContractedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    StartedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpectedDeliveryOn = table.Column<DateOnly>(type: "date", nullable: true),
                    PublishedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContractedRevisions = table.Column<int>(type: "integer", nullable: false),
                    UsedRevisions = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    DomainRegistrar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DomainHolder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DomainManagedByClient = table.Column<bool>(type: "boolean", nullable: false),
                    DomainRenewalOn = table.Column<DateOnly>(type: "date", nullable: true),
                    DomainCost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    DomainNotes = table.Column<string>(type: "text", nullable: true),
                    DnsProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DnsNotes = table.Column<string>(type: "text", nullable: true),
                    HostingProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HostingPlan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HostingAdminUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HostingCost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    HostingPeriodicity = table.Column<int>(type: "integer", nullable: false),
                    HostingRenewalOn = table.Column<DateOnly>(type: "date", nullable: true),
                    HostingStatus = table.Column<int>(type: "integer", nullable: false),
                    DeployPlatform = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProductionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StagingUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RepositoryUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProductionBranch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeployNotes = table.Column<string>(type: "text", nullable: true),
                    CredentialReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SslStatus = table.Column<int>(type: "integer", nullable: false),
                    SslExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    EmailProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmailPlan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmailIncludedCount = table.Column<int>(type: "integer", nullable: false),
                    EmailCost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    EmailPeriodicity = table.Column<int>(type: "integer", nullable: false),
                    EmailRenewalOn = table.Column<DateOnly>(type: "date", nullable: true),
                    EmailStatus = table.Column<int>(type: "integer", nullable: false),
                    DevelopmentContractValue = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    DevelopmentReceivedValue = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PaymentTerms = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DevelopmentPaymentStatus = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MonthlyFee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    EstimatedRecurringCost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    DueDay = table.Column<int>(type: "integer", nullable: false),
                    NextChargeOn = table.Column<DateOnly>(type: "date", nullable: true),
                    FinancialStatus = table.Column<int>(type: "integer", nullable: false),
                    HasContract = table.Column<bool>(type: "boolean", nullable: false),
                    ContractStatus = table.Column<int>(type: "integer", nullable: false),
                    ContractStartOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractSignedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ContractTermMonths = table.Column<int>(type: "integer", nullable: false),
                    CancellationNoticeDays = table.Column<int>(type: "integer", nullable: false),
                    IncludedRevisions = table.Column<int>(type: "integer", nullable: false),
                    MonthlySupportHours = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ContractNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedSites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManagedSiteCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Value = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Periodicity = table.Column<int>(type: "integer", nullable: false),
                    NextRenewalOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedSiteCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagedSiteCosts_ManagedSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "ManagedSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManagedSiteHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedSiteHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagedSiteHistory_ManagedSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "ManagedSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManagedSiteMailboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedSiteMailboxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagedSiteMailboxes_ManagedSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "ManagedSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManagedSiteSupportEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HoursSpent = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Billable = table.Column<bool>(type: "boolean", nullable: false),
                    AdditionalValue = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedSiteSupportEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagedSiteSupportEntries_ManagedSites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "ManagedSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedSiteCosts_SiteId",
                table: "ManagedSiteCosts",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedSiteHistory_SiteId_CreatedAt",
                table: "ManagedSiteHistory",
                columns: new[] { "SiteId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedSiteMailboxes_SiteId",
                table: "ManagedSiteMailboxes",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedSites_Active_Status",
                table: "ManagedSites",
                columns: new[] { "Active", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedSites_Domain",
                table: "ManagedSites",
                column: "Domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagedSiteSupportEntries_SiteId",
                table: "ManagedSiteSupportEntries",
                column: "SiteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagedSiteCosts");

            migrationBuilder.DropTable(
                name: "ManagedSiteHistory");

            migrationBuilder.DropTable(
                name: "ManagedSiteMailboxes");

            migrationBuilder.DropTable(
                name: "ManagedSiteSupportEntries");

            migrationBuilder.DropTable(
                name: "ManagedSites");
        }
    }
}
