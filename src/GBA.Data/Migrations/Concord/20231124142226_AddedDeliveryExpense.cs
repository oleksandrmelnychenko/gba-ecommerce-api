using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedDeliveryExpense : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "DeliveryExpense",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                InvoiceNumber = table.Column<string>("nvarchar(50)", maxLength: 50, nullable: true),
                FromDate = table.Column<DateTime>("datetime2", nullable: false),
                GrossAmount = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<decimal>("money", nullable: false),
                AccountingGrossAmount = table.Column<decimal>("money", nullable: false),
                AccountingVatPercent = table.Column<decimal>("money", nullable: false),
                SupplyOrderUkraineID = table.Column<long>("bigint", nullable: false),
                SupplyOrganizationID = table.Column<long>("bigint", nullable: false),
                SupplyOrganizationAgreementID = table.Column<long>("bigint", nullable: false),
                ConsumableProductID = table.Column<long>("bigint", nullable: true),
                ActProvidingServiceDocumentID = table.Column<long>("bigint", nullable: true),
                ActProvidingServiceID = table.Column<long>("bigint", nullable: true),
                UserID = table.Column<long>("bigint", nullable: false),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_DeliveryExpense", x => x.ID);
                table.ForeignKey(
                    "FK_DeliveryExpense_ActProvidingService_ActProvidingServiceID",
                    x => x.ActProvidingServiceID,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DeliveryExpense_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DeliveryExpense_ConsumableProduct_ConsumableProductID",
                    x => x.ConsumableProductID,
                    "ConsumableProduct",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DeliveryExpense_SupplyOrderUkraine_SupplyOrderUkraineID",
                    x => x.SupplyOrderUkraineID,
                    "SupplyOrderUkraine",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DeliveryExpense_SupplyOrganization_SupplyOrganizationID",
                    x => x.SupplyOrganizationID,
                    "SupplyOrganization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DeliveryExpense_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
                    x => x.SupplyOrganizationAgreementID,
                    "SupplyOrganizationAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DeliveryExpense_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_DeliveryExpense_ActProvidingServiceDocumentID",
            "DeliveryExpense",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_DeliveryExpense_ActProvidingServiceID",
            "DeliveryExpense",
            "ActProvidingServiceID",
            unique: true,
            filter: "[ActProvidingServiceID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_DeliveryExpense_ConsumableProductID",
            "DeliveryExpense",
            "ConsumableProductID");

        migrationBuilder.CreateIndex(
            "IX_DeliveryExpense_SupplyOrderUkraineID",
            "DeliveryExpense",
            "SupplyOrderUkraineID");

        migrationBuilder.CreateIndex(
            "IX_DeliveryExpense_SupplyOrganizationAgreementID",
            "DeliveryExpense",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_DeliveryExpense_SupplyOrganizationID",
            "DeliveryExpense",
            "SupplyOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_DeliveryExpense_UserID",
            "DeliveryExpense",
            "UserID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "DeliveryExpense");
    }
}