using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddRetailClientPaymentImageItem : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "ImgUrl",
            "RetailClientPaymentImage");

        migrationBuilder.CreateTable(
            "RetailClientPaymentImageItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ImgUrl = table.Column<string>(maxLength: 1000, nullable: true),
                Amount = table.Column<decimal>("money", nullable: false),
                IsCorrectPayment = table.Column<bool>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                RetailClientPaymentImageID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_RetailClientPaymentImageItem", x => x.ID);
                table.ForeignKey(
                    "FK_RetailClientPaymentImageItem_RetailClientPaymentImage_RetailClientPaymentImageID",
                    x => x.RetailClientPaymentImageID,
                    "RetailClientPaymentImage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_RetailClientPaymentImageItem_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_RetailClientPaymentImageItem_RetailClientPaymentImageID",
            "RetailClientPaymentImageItem",
            "RetailClientPaymentImageID");

        migrationBuilder.CreateIndex(
            "IX_RetailClientPaymentImageItem_UserID",
            "RetailClientPaymentImageItem",
            "UserID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "RetailClientPaymentImageItem");

        migrationBuilder.AddColumn<string>(
            "ImgUrl",
            "RetailClientPaymentImage",
            maxLength: 1000,
            nullable: true);
    }
}