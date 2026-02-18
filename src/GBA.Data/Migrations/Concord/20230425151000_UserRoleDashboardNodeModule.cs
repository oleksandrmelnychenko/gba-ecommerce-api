using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class UserRoleDashboardNodeModule : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "UserRoleDashboardNodeModule",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserRoleID = table.Column<long>("bigint", nullable: false),
                DashboardNodeModuleID = table.Column<long>("bigint", nullable: false),
                DashboardNodeID = table.Column<long>("bigint", nullable: false),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_UserRoleDashboardNodeModule", x => x.ID);
                table.ForeignKey(
                    "FK_UserRoleDashboardNodeModule_DashboardNode_DashboardNodeID",
                    x => x.DashboardNodeID,
                    "DashboardNode",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_UserRoleDashboardNodeModule_DashboardNodeModule_DashboardNodeModuleID",
                    x => x.DashboardNodeModuleID,
                    "DashboardNodeModule",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_UserRoleDashboardNodeModule_UserRole_UserRoleID",
                    x => x.UserRoleID,
                    "UserRole",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            "IX_UserRoleDashboardNodeModule_DashboardNodeID",
            "UserRoleDashboardNodeModule",
            "DashboardNodeID");

        migrationBuilder.CreateIndex(
            "IX_UserRoleDashboardNodeModule_DashboardNodeModuleID",
            "UserRoleDashboardNodeModule",
            "DashboardNodeModuleID");

        migrationBuilder.CreateIndex(
            "IX_UserRoleDashboardNodeModule_UserRoleID",
            "UserRoleDashboardNodeModule",
            "UserRoleID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "UserRoleDashboardNodeModule");
    }
}