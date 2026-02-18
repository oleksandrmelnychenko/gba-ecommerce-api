using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ReconfiguredRelationsForPermissions : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_UserRoleDashboardNodeModule_DashboardNodeModule_DashboardNodeModuleID",
            "UserRoleDashboardNodeModule");

        migrationBuilder.DropIndex(
            "IX_UserRoleDashboardNodeModule_DashboardNodeModuleID",
            "UserRoleDashboardNodeModule");

        migrationBuilder.DropColumn(
            "DashboardNodeModuleID",
            "UserRoleDashboardNodeModule");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "DashboardNodeModuleID",
            "UserRoleDashboardNodeModule",
            "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateIndex(
            "IX_UserRoleDashboardNodeModule_DashboardNodeModuleID",
            "UserRoleDashboardNodeModule",
            "DashboardNodeModuleID");

        migrationBuilder.AddForeignKey(
            "FK_UserRoleDashboardNodeModule_DashboardNodeModule_DashboardNodeModuleID",
            "UserRoleDashboardNodeModule",
            "DashboardNodeModuleID",
            "DashboardNodeModule",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);
    }
}