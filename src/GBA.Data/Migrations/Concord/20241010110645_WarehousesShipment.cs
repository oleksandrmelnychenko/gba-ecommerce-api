using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class WarehousesShipment : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "WarehousesShipmentId",
            "Sale",
            "bigint",
            nullable: true);

        migrationBuilder.CreateTable(
            "WarehousesShipment",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                IsDevelopment = table.Column<bool>("bit", nullable: false, defaultValueSql: "0"),
                SaleId = table.Column<long>("bigint", nullable: true),
                TransporterId = table.Column<long>("bigint", nullable: true),
                UserId = table.Column<long>("bigint", nullable: true),
                IsCashOnDelivery = table.Column<bool>("bit", nullable: false),
                CashOnDeliveryAmount = table.Column<decimal>("money", nullable: false),
                HasDocument = table.Column<bool>("bit", nullable: false),
                ShipmentDate = table.Column<DateTime>("datetime2", nullable: true),
                TTN = table.Column<string>("nvarchar(max)", nullable: true),
                Comment = table.Column<string>("nvarchar(450)", maxLength: 450, nullable: true),
                Number = table.Column<string>("nvarchar(max)", nullable: true),
                MobilePhone = table.Column<string>("nvarchar(max)", nullable: true),
                FullName = table.Column<string>("nvarchar(max)", nullable: true),
                City = table.Column<string>("nvarchar(max)", nullable: true),
                Department = table.Column<string>("nvarchar(max)", nullable: true),
                TtnPDFPath = table.Column<string>("nvarchar(max)", nullable: true),
                ApproveUpdate = table.Column<bool>("bit", nullable: false),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_WarehousesShipment", x => x.ID);
                table.ForeignKey(
                    "FK_WarehousesShipment_Sale_SaleId",
                    x => x.SaleId,
                    "Sale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_WarehousesShipment_Transporter_TransporterId",
                    x => x.TransporterId,
                    "Transporter",
                    "ID");
                table.ForeignKey(
                    "FK_WarehousesShipment_User_UserId",
                    x => x.UserId,
                    "User",
                    "ID");
            });

        migrationBuilder.CreateIndex(
            "IX_WarehousesShipment_SaleId",
            "WarehousesShipment",
            "SaleId",
            unique: true,
            filter: "[SaleId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_WarehousesShipment_TransporterId",
            "WarehousesShipment",
            "TransporterId");

        migrationBuilder.CreateIndex(
            "IX_WarehousesShipment_UserId",
            "WarehousesShipment",
            "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "WarehousesShipment");

        migrationBuilder.DropColumn(
            "WarehousesShipmentId",
            "Sale");
    }
}