using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class RemovedRecommendedBuyerFromSale : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_UpdateDataCarrie_Sale_SaleId",
            "UpdateDataCarrie");

        migrationBuilder.DropForeignKey(
            "FK_UpdateDataCarrie_Transporter_TransporterId",
            "UpdateDataCarrie");

        migrationBuilder.DropForeignKey(
            "FK_UpdateDataCarrie_User_UserId",
            "UpdateDataCarrie");

        migrationBuilder.DropPrimaryKey(
            "PK_UpdateDataCarrie",
            "UpdateDataCarrie");

        migrationBuilder.DropColumn(
            "RecommendedBuyer",
            "Sale");

        migrationBuilder.DropColumn(
            "RecommendedBuyer",
            "UpdateDataCarrie");

        migrationBuilder.RenameTable(
            "UpdateDataCarrie",
            newName: "UpdateDataCarrier");

        migrationBuilder.RenameIndex(
            "IX_UpdateDataCarrie_UserId",
            table: "UpdateDataCarrier",
            newName: "IX_UpdateDataCarrier_UserId");

        migrationBuilder.RenameIndex(
            "IX_UpdateDataCarrie_TransporterId",
            table: "UpdateDataCarrier",
            newName: "IX_UpdateDataCarrier_TransporterId");

        migrationBuilder.RenameIndex(
            "IX_UpdateDataCarrie_SaleId",
            table: "UpdateDataCarrier",
            newName: "IX_UpdateDataCarrier_SaleId");

        migrationBuilder.AddColumn<decimal>(
            "CashOnDeliveryAmount",
            "UpdateDataCarrier",
            "money",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddPrimaryKey(
            "PK_UpdateDataCarrier",
            "UpdateDataCarrier",
            "ID");

        migrationBuilder.AddForeignKey(
            "FK_UpdateDataCarrier_Sale_SaleId",
            "UpdateDataCarrier",
            "SaleId",
            "Sale",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            "FK_UpdateDataCarrier_Transporter_TransporterId",
            "UpdateDataCarrier",
            "TransporterId",
            "Transporter",
            principalColumn: "ID");

        migrationBuilder.AddForeignKey(
            "FK_UpdateDataCarrier_User_UserId",
            "UpdateDataCarrier",
            "UserId",
            "User",
            principalColumn: "ID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_UpdateDataCarrier_Sale_SaleId",
            "UpdateDataCarrier");

        migrationBuilder.DropForeignKey(
            "FK_UpdateDataCarrier_Transporter_TransporterId",
            "UpdateDataCarrier");

        migrationBuilder.DropForeignKey(
            "FK_UpdateDataCarrier_User_UserId",
            "UpdateDataCarrier");

        migrationBuilder.DropPrimaryKey(
            "PK_UpdateDataCarrier",
            "UpdateDataCarrier");

        migrationBuilder.DropColumn(
            "CashOnDeliveryAmount",
            "UpdateDataCarrier");

        migrationBuilder.RenameTable(
            "UpdateDataCarrier",
            newName: "UpdateDataCarrie");

        migrationBuilder.RenameIndex(
            "IX_UpdateDataCarrier_UserId",
            table: "UpdateDataCarrie",
            newName: "IX_UpdateDataCarrie_UserId");

        migrationBuilder.RenameIndex(
            "IX_UpdateDataCarrier_TransporterId",
            table: "UpdateDataCarrie",
            newName: "IX_UpdateDataCarrie_TransporterId");

        migrationBuilder.RenameIndex(
            "IX_UpdateDataCarrier_SaleId",
            table: "UpdateDataCarrie",
            newName: "IX_UpdateDataCarrie_SaleId");

        migrationBuilder.AddColumn<string>(
            "RecommendedBuyer",
            "Sale",
            "nvarchar(450)",
            maxLength: 450,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "RecommendedBuyer",
            "UpdateDataCarrie",
            "nvarchar(450)",
            maxLength: 450,
            nullable: true);

        migrationBuilder.AddPrimaryKey(
            "PK_UpdateDataCarrie",
            "UpdateDataCarrie",
            "ID");

        migrationBuilder.AddForeignKey(
            "FK_UpdateDataCarrie_Sale_SaleId",
            "UpdateDataCarrie",
            "SaleId",
            "Sale",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            "FK_UpdateDataCarrie_Transporter_TransporterId",
            "UpdateDataCarrie",
            "TransporterId",
            "Transporter",
            principalColumn: "ID");

        migrationBuilder.AddForeignKey(
            "FK_UpdateDataCarrie_User_UserId",
            "UpdateDataCarrie",
            "UserId",
            "User",
            principalColumn: "ID");
    }
}