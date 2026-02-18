using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class UpdateDataCarrier : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "UpdateDataCarrie",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SaleId = table.Column<long>("bigint", nullable: false),
                TransporterId = table.Column<long>("bigint", nullable: true),
                UserId = table.Column<long>("bigint", nullable: true),
                IsCashOnDelivery = table.Column<bool>("bit", nullable: false),
                HasDocument = table.Column<bool>("bit", nullable: false),
                IsPrintedPaymentInvoice = table.Column<bool>("bit", nullable: false),
                RecommendedBuyer = table.Column<string>("nvarchar(450)", maxLength: 450, nullable: true),
                Comment = table.Column<string>("nvarchar(450)", maxLength: 450, nullable: true),
                Number = table.Column<string>("nvarchar(max)", nullable: true),
                MobilePhone = table.Column<string>("nvarchar(max)", nullable: true),
                FullName = table.Column<string>("nvarchar(max)", nullable: true),
                City = table.Column<string>("nvarchar(max)", nullable: true),
                Department = table.Column<string>("nvarchar(max)", nullable: true),
                TtnPDFPath = table.Column<string>("nvarchar(max)", nullable: true),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_UpdateDataCarrie", x => x.ID);
                table.ForeignKey(
                    "FK_UpdateDataCarrie_Sale_SaleId",
                    x => x.SaleId,
                    "Sale",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_UpdateDataCarrie_Transporter_TransporterId",
                    x => x.TransporterId,
                    "Transporter",
                    "ID");
                table.ForeignKey(
                    "FK_UpdateDataCarrie_User_UserId",
                    x => x.UserId,
                    "User",
                    "ID");
            });

        migrationBuilder.CreateIndex(
            "IX_UpdateDataCarrie_SaleId",
            "UpdateDataCarrie",
            "SaleId");

        migrationBuilder.CreateIndex(
            "IX_UpdateDataCarrie_TransporterId",
            "UpdateDataCarrie",
            "TransporterId");

        migrationBuilder.CreateIndex(
            "IX_UpdateDataCarrie_UserId",
            "UpdateDataCarrie",
            "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "UpdateDataCarrie");
    }
}