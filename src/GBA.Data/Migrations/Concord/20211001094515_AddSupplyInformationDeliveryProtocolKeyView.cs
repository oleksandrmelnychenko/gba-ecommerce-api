using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddSupplyInformationDeliveryProtocolKeyView : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(@"
                IF NOT EXISTS(select * FROM sys.views where name = 'SupplyInformationDeliveryProtocolKeyView')
                BEGIN
                    EXEC('
                        CREATE VIEW views.SupplyInformationDeliveryProtocolKeyView
                            WITH SCHEMABINDING
                        AS
                        SELECT [ProtocolKey].ID
                             , [ProtocolKey].Deleted
                             , [ProtocolKey].Created
                             , [ProtocolKey].IsDefault
                             , [ProtocolKey].KeyAssignedTo
                             , [ProtocolKey].NetUID
                             , [ProtocolKey].TransportationType
                             , [ProtocolKey].Updated
                             , [ProtocolKeyTranslation].[Key]
                             , [ProtocolKeyTranslation].CultureCode
                        FROM [dbo].[SupplyInformationDeliveryProtocolKey] AS [ProtocolKey]
                             INNER JOIN [dbo].[SupplyInformationDeliveryProtocolKeyTranslation] AS [ProtocolKeyTranslation]
                                ON [ProtocolKeyTranslation].SupplyInformationDeliveryProtocolKeyID = [ProtocolKey].ID
                                AND [ProtocolKeyTranslation].Deleted = 0
                    ')
                END
            ");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("DROP VIEW views.SupplyInformationDeliveryProtocolKeyView");
    }
}