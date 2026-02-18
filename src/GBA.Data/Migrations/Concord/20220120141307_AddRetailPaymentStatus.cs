using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddRetailPaymentStatus : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "PaymentImageStatusId",
            "RetailClientPaymentImage",
            nullable: true);

        migrationBuilder.CreateTable(
            "RetailPaymentStatus",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                RetailPaymentStatusType = table.Column<int>(nullable: false),
                Amount = table.Column<decimal>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_RetailPaymentStatus", x => x.ID);
            });

        migrationBuilder.CreateIndex(
            "IX_RetailClientPaymentImage_PaymentImageStatusId",
            "RetailClientPaymentImage",
            "PaymentImageStatusId");

        migrationBuilder.AddForeignKey(
            "FK_RetailClientPaymentImage_RetailPaymentStatus_PaymentImageStatusId",
            "RetailClientPaymentImage",
            "PaymentImageStatusId",
            "RetailPaymentStatus",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_RetailClientPaymentImage_RetailPaymentStatus_PaymentImageStatusId",
            "RetailClientPaymentImage");

        migrationBuilder.DropTable(
            "RetailPaymentStatus");

        migrationBuilder.DropIndex(
            "IX_RetailClientPaymentImage_PaymentImageStatusId",
            "RetailClientPaymentImage");

        migrationBuilder.DropColumn(
            "PaymentImageStatusId",
            "RetailClientPaymentImage");
    }
}