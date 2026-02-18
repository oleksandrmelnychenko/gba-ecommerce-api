using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedPermissions : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "Permission",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ControlId = table.Column<string>("nvarchar(max)", nullable: true),
                Name = table.Column<string>("nvarchar(500)", maxLength: 500, nullable: true),
                ImageUrl = table.Column<string>("nvarchar(max)", nullable: true),
                Description = table.Column<string>("nvarchar(500)", maxLength: 500, nullable: true),
                DashboardNodeID = table.Column<long>("bigint", nullable: false),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_Permission", x => x.ID);
                table.ForeignKey(
                    "FK_Permission_DashboardNode_DashboardNodeID",
                    x => x.DashboardNodeID,
                    "DashboardNode",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "RolePermission",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserRoleID = table.Column<long>("bigint", nullable: false),
                PermissionID = table.Column<long>("bigint", nullable: false),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_RolePermission", x => x.ID);
                table.ForeignKey(
                    "FK_RolePermission_Permission_PermissionID",
                    x => x.PermissionID,
                    "Permission",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_RolePermission_UserRole_UserRoleID",
                    x => x.UserRoleID,
                    "UserRole",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            "IX_Permission_DashboardNodeID",
            "Permission",
            "DashboardNodeID");

        migrationBuilder.CreateIndex(
            "IX_RolePermission_PermissionID",
            "RolePermission",
            "PermissionID");

        migrationBuilder.CreateIndex(
            "IX_RolePermission_UserRoleID",
            "RolePermission",
            "UserRoleID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "RolePermission");

        migrationBuilder.DropTable(
            "Permission");
    }
}