using System;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class BillOfLadingServiceRepository : IBillOfLadingServiceRepository {
    private readonly IDbConnection _connection;

    public BillOfLadingServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(BillOfLadingService billOfLadingService) {
        return _connection.Query<long>(
            "INSERT INTO [BillOfLadingService]([IsActive], [FromDate], [GrossPrice], [NetPrice], [Vat], [AccountingGrossPrice], [AccountingNetPrice], [AccountingVat], " +
            "[VatPercent], [AccountingVatPercent], [Number], [ServiceNumber], [Name], [UserID], [SupplyPaymentTaskID], [AccountingPaymentTaskID], " +
            "[SupplyOrganizationAgreementID], [LoadDate], [BillOfLadingNumber], [TermDeliveryInDays], [SupplyOrganizationID], " +
            "[SupplyExtraChargeType], [TypeBillOfLadingService], [DeliveryProductProtocolID], [Updated], [IsAutoCalculatedValue], [IsShipped], " +
            "[AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], [ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], " +
            "[ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID], [ActProvidingServiceID], [AccountingActProvidingServiceID]) " +
            "VALUES (@IsActive, @FromDate, @GrossPrice, @NetPrice, @Vat, @AccountingGrossPrice, @AccountingNetPrice, @AccountingVat, " +
            "@VatPercent, @AccountingVatPercent, @Number, @ServiceNumber, @Name, @UserID, @SupplyPaymentTaskID, @AccountingPaymentTaskID, " +
            "@SupplyOrganizationAgreementID, @LoadDate, @BillOfLadingNumber, @TermDeliveryInDays, @SupplyOrganizationID, " +
            "@SupplyExtraChargeType, @TypeBillOfLadingService, @DeliveryProductProtocolID, getutcdate(), @IsAutoCalculatedValue, @IsShipped, " +
            "@AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, @ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, " +
            "@ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId, @ActProvidingServiceId, @AccountingActProvidingServiceId) " +
            "SELECT SCOPE_IDENTITY(); ",
            billOfLadingService).Single();
    }

    public void Update(BillOfLadingService billOfLadingService) {
        _connection.Execute(
            "UPDATE [dbo].[BillOfLadingService] " +
            "SET [Updated] = GETUTCDATE() " +
            ",[IsActive] = @IsActive " +
            ",[FromDate] = @FromDate " +
            ",[GrossPrice] = @GrossPrice " +
            ",[NetPrice] = @NetPrice " +
            ",[Vat] = @Vat " +
            ",[AccountingGrossPrice] = @AccountingGrossPrice " +
            ",[AccountingNetPrice] = @AccountingNetPrice " +
            ",[AccountingVat] = @AccountingVat " +
            ",[VatPercent] = @VatPercent " +
            ",[AccountingVatPercent] = @AccountingVatPercent " +
            ",[Number] = @Number " +
            ",[ServiceNumber] = @ServiceNumber " +
            ",[Name] = @Name " +
            ",[UserID] = @UserID " +
            ",[SupplyPaymentTaskID] = @SupplyPaymentTaskID " +
            ",[AccountingPaymentTaskID] = @AccountingPaymentTaskID " +
            ",[SupplyOrganizationAgreementID] = @SupplyOrganizationAgreementID " +
            ",[LoadDate] = @LoadDate " +
            ",[BillOfLadingNumber] = @BillOfLadingNumber " +
            ",[TermDeliveryInDays] = @TermDeliveryInDays " +
            ",[SupplyOrganizationID] = @SupplyOrganizationID " +
            ",[SupplyExtraChargeType] = @SupplyExtraChargeType " +
            ",[TypeBillOfLadingService] = @TypeBillOfLadingService " +
            ",[DeliveryProductProtocolID] = @DeliveryProductProtocolID " +
            ",[IsCalculatedValue] = @IsCalculatedValue " +
            ",[IsAutoCalculatedValue] = @IsAutoCalculatedValue " +
            ",[IsShipped] = @IsShipped " +
            ",[AccountingSupplyCostsWithinCountry] = @AccountingSupplyCostsWithinCountry " +
            ",[SupplyInformationTaskID] = @SupplyInformationTaskId " +
            ",[ExchangeRate] = @ExchangeRate " +
            ",[AccountingExchangeRate] = @AccountingExchangeRate " +
            ",[IsIncludeAccountingValue] = @IsIncludeAccountingValue " +
            ",[ActProvidingServiceDocumentID] = @ActProvidingServiceDocumentId " +
            ",[SupplyServiceAccountDocumentID] = @SupplyServiceAccountDocumentId " +
            ",[ActProvidingServiceID] = @ActProvidingServiceId " +
            ",[AccountingActProvidingServiceID] = @AccountingActProvidingServiceId " +
            "WHERE [BillOfLadingService].[ID] = @Id; ",
            billOfLadingService);
    }

    public void RemoveById(long id) {
        _connection.Execute(
            "UPDATE [BillOfLadingService] " +
            "SET [Deleted] = 1 " +
            ",[Updated] = getutcdate() " +
            "WHERE [BillOfLadingService].[ID] = @Id; ",
            new { Id = id });
    }

    public BillOfLadingService GetByIdWithoutIncludes(long id) {
        return _connection.Query<BillOfLadingService>(
            "SELECT * FROM [BillOfLadingService] " +
            "WHERE [BillOfLadingService].[ID] = @Id; ",
            new { ID = id }).FirstOrDefault();
    }

    public DeliveryProductProtocol GetDeliveryProductProtocolByNetId(Guid netId) {
        return _connection.Query<DeliveryProductProtocol>(
            "SELECT * FROM [BillOfLadingService] " +
            "LEFT JOIN [DeliveryProductProtocol] " +
            "ON [DeliveryProductProtocol].[ID] = [BillOfLadingService].[DeliveryProductProtocolID] " +
            "WHERE [BillOfLadingService].[NetUID] = @NetId; ",
            new { NetId = netId }).FirstOrDefault();
    }

    public BillOfLadingService GetWithoutIncludesByNetId(Guid netId) {
        return _connection.Query<BillOfLadingService, SupplyOrganizationAgreement, BillOfLadingService>(
            "SELECT * FROM [BillOfLadingService] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [BillOfLadingService].[SupplyOrganizationAgreementID] " +
            "WHERE [BillOfLadingService].[NetUID] = @NetId; ",
            (service, agreement) => {
                service.SupplyOrganizationAgreement = agreement;
                return service;
            },
            new { NetId = netId }).FirstOrDefault();
    }

    public long GetDeliveryProductProtocolIdByNetId(Guid netUid) {
        return _connection.Query<long>(
            "SELECT [BillOfLadingService].[DeliveryProductProtocolID] " +
            "FROM [BillOfLadingService] " +
            "WHERE [BillOfLadingService].[NetUID] = @NetId; ",
            new { NetId = netUid }).Single();
    }

    public void UpdateIsCalculatedValueById(long id, bool isAuto) {
        _connection.Execute(
            "UPDATE [BillOfLadingService] " +
            "SET [IsCalculatedValue] = 1 " +
            ", [IsAutoCalculatedValue] = @IsAuto " +
            "WHERE [ID] = @Id; ",
            new { Id = id, IsAuto = isAuto });
    }

    public void UpdateSupplyExtraChargeTypeById(long id, SupplyExtraChargeType type) {
        _connection.Execute(
            "UPDATE [BillOfLadingService] " +
            "SET SupplyExtraChargeType = @ExtraChargeType " +
            "WHERE [ID] = @Id; ",
            new { Id = id, ExtraChargeType = type });
    }

    public void UpdateIsShippedByDeliveryProductProtocolId(long id) {
        _connection.Execute(
            "UPDATE [BillOfLadingService] " +
            "SET [IsShipped] = 1 " +
            ",[Updated] = getutcdate() " +
            "WHERE [BillOfLadingService].[DeliveryProductProtocolID] = @Id; ",
            new { Id = id });
    }

    public void ResetIsCalculatedValueById(long id) {
        _connection.Execute(
            "UPDATE [BillOfLadingService] " +
            "SET [IsCalculatedValue] = 0 " +
            "WHERE [ID] = @Id; ",
            new { Id = id });
    }
}