using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalChain.Marketplace.Migrations
{
    /// <inheritdoc />
    public partial class InitializeDashboardTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ConcurrencyToken = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardConfigCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CacheKey = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CachedValueJson = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HitCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardConfigCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardConfigAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PreviousConfigurationJson = table.Column<string>(type: "TEXT", nullable: true),
                    NewConfigurationJson = table.Column<string>(type: "TEXT", nullable: true),
                    ChangesSummary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RequestIpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardConfigAudits", x => x.Id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfig_TenantId",
                table: "DashboardConfigs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfig_TenantId_IsActive",
                table: "DashboardConfigs",
                columns: new[] { "TenantId", "IsActive" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfig_UpdatedAt",
                table: "DashboardConfigs",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfig_CreatedAt",
                table: "DashboardConfigs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfigCache_TenantId",
                table: "DashboardConfigCache",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfigCache_ExpiresAt",
                table: "DashboardConfigCache",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfigCache_CacheKey",
                table: "DashboardConfigCache",
                column: "CacheKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfigAudit_TenantId",
                table: "DashboardConfigAudits",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfigAudit_ConfigurationId",
                table: "DashboardConfigAudits",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfigAudit_TenantId_ChangedAt",
                table: "DashboardConfigAudits",
                columns: new[] { "TenantId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfigAudit_ChangedAt",
                table: "DashboardConfigAudits",
                column: "ChangedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardConfigAudits");

            migrationBuilder.DropTable(
                name: "DashboardConfigCache");

            migrationBuilder.DropTable(
                name: "DashboardConfigs");
        }
    }
}
