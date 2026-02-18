using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedOrganizationAndExistDateToSupplyOrganizationAgreement : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_SupplyOrganization_Organization_OrganizationID",
            "SupplyOrganization");

        migrationBuilder.DropIndex(
            "IX_SupplyOrganization_OrganizationID",
            "SupplyOrganization");

        migrationBuilder.DropColumn(
            "ExistTo",
            "SupplyOrganization");

        migrationBuilder.DropColumn(
            "OrganizationID",
            "SupplyOrganization");

        migrationBuilder.AddColumn<DateTime>(
            "ExistTo",
            "SupplyOrganizationAgreement",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<long>(
            "OrganizationID",
            "SupplyOrganizationAgreement",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateIndex(
            "IX_SupplyOrganizationAgreement_OrganizationID",
            "SupplyOrganizationAgreement",
            "OrganizationID");

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrganizationAgreement_Organization_OrganizationID",
            "SupplyOrganizationAgreement",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_SupplyOrganizationAgreement_Organization_OrganizationID",
            "SupplyOrganizationAgreement");

        migrationBuilder.DropIndex(
            "IX_SupplyOrganizationAgreement_OrganizationID",
            "SupplyOrganizationAgreement");

        migrationBuilder.DropColumn(
            "ExistTo",
            "SupplyOrganizationAgreement");

        migrationBuilder.DropColumn(
            "OrganizationID",
            "SupplyOrganizationAgreement");

        migrationBuilder.AddColumn<DateTime>(
            "ExistTo",
            "SupplyOrganization",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<long>(
            "OrganizationID",
            "SupplyOrganization",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateIndex(
            "IX_SupplyOrganization_OrganizationID",
            "SupplyOrganization",
            "OrganizationID");

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrganization_Organization_OrganizationID",
            "SupplyOrganization",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }
}