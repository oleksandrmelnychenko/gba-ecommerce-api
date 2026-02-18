using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Consumables.Orders;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.ActProvidingServices;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.EntityHelpers.Supplies;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyOrganizationCashFlowRepository : ISupplyOrganizationCashFlowRepository {
    private readonly IDbConnection _connection;

    public SupplyOrganizationCashFlowRepository(IDbConnection connection) {
        _connection = connection;
    }

    public AccountingCashFlow GetRangedBySupplyOrganization(SupplyOrganization supplyOrganization, DateTime from, DateTime to, TypePaymentTask typePaymentTask) {
        string beforeRangeInAmountSqlQuery;

        if (typePaymentTask.Equals(TypePaymentTask.PaymentTask))
            beforeRangeInAmountSqlQuery = "SELECT " +
                                          "ROUND( " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].NetPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [ContainerService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [ContainerService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [BillOfLadingDocument] " +
                                          "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                                          "WHERE [ContainerService].ContainerOrganizationID = @Id " +
                                          "AND [BillOfLadingDocument].[Date] < @From " +
                                          "AND [ContainerService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].NetPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [VehicleService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [VehicleService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [BillOfLadingDocument] " +
                                          "ON [BillOfLadingDocument].ID = [VehicleService].BillOfLadingDocumentID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                                          "WHERE [VehicleService].VehicleOrganizationID = @Id " +
                                          "AND [BillOfLadingDocument].[Date] < @From " +
                                          "AND [VehicleService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].NetPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [BillOfLadingService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [BillOfLadingService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [BillOfLadingDocument] " +
                                          "ON [BillOfLadingDocument].BillOfLadingServiceID = [BillOfLadingService].ID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [BillOfLadingService].SupplyOrganizationAgreementID " +
                                          "WHERE [BillOfLadingService].SupplyOrganizationID = @Id " +
                                          "AND [BillOfLadingDocument].[Date] < @From " +
                                          "AND [BillOfLadingService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [CustomService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                                          "WHERE " +
                                          "( " +
                                          "[CustomService].CustomOrganizationID = @Id " +
                                          "OR [CustomService].ExciseDutyOrganizationID = @Id " +
                                          ") " +
                                          "AND [CustomService].FromDate < @From " +
                                          "AND [CustomService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [PortWorkService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                                          "WHERE [PortWorkService].PortWorkOrganizationID = @Id " +
                                          "AND [PortWorkService].FromDate < @From " +
                                          "AND [PortWorkService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [TransportationService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                                          "WHERE [TransportationService].TransportationOrganizationID = @Id " +
                                          "AND [TransportationService].FromDate < @From " +
                                          "AND [TransportationService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [PortCustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                                          "WHERE [PortCustomAgencyService].PortCustomAgencyOrganizationID = @Id " +
                                          "AND [PortCustomAgencyService].FromDate < @From " +
                                          "AND [PortCustomAgencyService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [CustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                                          "WHERE [CustomAgencyService].CustomAgencyOrganizationID = @Id " +
                                          "AND [CustomAgencyService].FromDate < @From " +
                                          "AND [CustomAgencyService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [PlaneDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                                          "WHERE [PlaneDeliveryService].PlaneDeliveryOrganizationID = @Id " +
                                          "AND [PlaneDeliveryService].FromDate < @From " +
                                          "AND [PlaneDeliveryService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [VehicleDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                                          "WHERE [VehicleDeliveryService].VehicleDeliveryOrganizationID = @Id " +
                                          "AND [VehicleDeliveryService].FromDate < @From " +
                                          "AND [VehicleDeliveryService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[MergedService].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [MergedService] " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                                          "WHERE [MergedService].SupplyOrganizationID = @Id " +
                                          "AND [MergedService].FromDate < @From " +
                                          "AND [MergedService].Deleted = 0 " +
                                          "AND [MergedService].[ActProvidingServiceID] IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[DeliveryExpense].GrossAmount, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [DeliveryExpense] " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                          "LEFT JOIN [ActProvidingService] " +
                                          "ON [ActProvidingService].ID = [DeliveryExpense].ActProvidingServiceID " +
                                          "WHERE [DeliveryExpense].SupplyOrganizationID = @Id " +
                                          "AND [DeliveryExpense].FromDate < @From " +
                                          "AND [DeliveryExpense].Deleted = 0 " +
                                          "AND [DeliveryExpense].ActProvidingServiceID IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "([ConsumablesOrderItem].TotalPriceWithVAT), " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [ConsumablesOrder] " +
                                          "LEFT JOIN [ConsumablesOrderItem] " +
                                          "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                                          "AND [ConsumablesOrderItem].Deleted = 0 " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                                          "WHERE [ConsumablesOrderItem].ConsumableProductOrganizationID = @Id " +
                                          "AND [ConsumablesOrder].OrganizationFromDate < @From " +
                                          "AND [ConsumablesOrder].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([IncomePaymentOrder].EuroAmount), 0) " +
                                          "FROM [IncomePaymentOrder] " +
                                          "WHERE [IncomePaymentOrder].SupplyOrganizationID = @Id " +
                                          "AND [IncomePaymentOrder].FromDate < @From " +
                                          "AND [IncomePaymentOrder].Deleted = 0 " +
                                          "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                          "AND [IncomePaymentOrder].IsManagementAccounting = 1 " +
                                          "AND [IncomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
                                          ") " +
                                          ", 2) AS [BeforeRangeInAmount]";
        else if (typePaymentTask.Equals(TypePaymentTask.AccountingPaymentTask))
            beforeRangeInAmountSqlQuery = "SELECT " +
                                          "ROUND( " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].NetPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [ContainerService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [ContainerService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [BillOfLadingDocument] " +
                                          "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                                          "WHERE [ContainerService].ContainerOrganizationID = @Id " +
                                          "AND [BillOfLadingDocument].[Date] < @From " +
                                          "AND [ContainerService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].NetPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [VehicleService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [VehicleService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [BillOfLadingDocument] " +
                                          "ON [BillOfLadingDocument].ID = [VehicleService].BillOfLadingDocumentID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                                          "WHERE [VehicleService].VehicleOrganizationID = @Id " +
                                          "AND [BillOfLadingDocument].[Date] < @From " +
                                          "AND [VehicleService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].NetPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [BillOfLadingService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [BillOfLadingService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [BillOfLadingDocument] " +
                                          "ON [BillOfLadingDocument].BillOfLadingServiceID = [BillOfLadingService].ID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [BillOfLadingService].SupplyOrganizationAgreementID " +
                                          "WHERE [BillOfLadingService].SupplyOrganizationID = @Id " +
                                          "AND [BillOfLadingDocument].[Date] < @From " +
                                          "AND [BillOfLadingService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [CustomService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [CustomService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                                          "WHERE " +
                                          "( " +
                                          "[CustomService].CustomOrganizationID = @Id " +
                                          "OR [CustomService].ExciseDutyOrganizationID = @Id " +
                                          ") " +
                                          "AND [CustomService].FromDate < @From " +
                                          "AND [CustomService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [PortWorkService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PortWorkService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                                          "WHERE [PortWorkService].PortWorkOrganizationID = @Id " +
                                          "AND [PortWorkService].FromDate < @From " +
                                          "AND [PortWorkService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [TransportationService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [TransportationService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                                          "WHERE [TransportationService].TransportationOrganizationID = @Id " +
                                          "AND [TransportationService].FromDate < @From " +
                                          "AND [TransportationService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [PortCustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                                          "WHERE [PortCustomAgencyService].PortCustomAgencyOrganizationID = @Id " +
                                          "AND [PortCustomAgencyService].FromDate < @From " +
                                          "AND [PortCustomAgencyService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [CustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [CustomAgencyService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                                          "WHERE [CustomAgencyService].CustomAgencyOrganizationID = @Id " +
                                          "AND [CustomAgencyService].FromDate < @From " +
                                          "AND [CustomAgencyService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [PlaneDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                                          "WHERE [PlaneDeliveryService].PlaneDeliveryOrganizationID = @Id " +
                                          "AND [PlaneDeliveryService].FromDate < @From " +
                                          "AND [PlaneDeliveryService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [VehicleDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                                          "WHERE [VehicleDeliveryService].VehicleDeliveryOrganizationID = @Id " +
                                          "AND [VehicleDeliveryService].FromDate < @From " +
                                          "AND [VehicleDeliveryService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[MergedService].AccountingGrossPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [MergedService] " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                                          "WHERE [MergedService].SupplyOrganizationID = @Id " +
                                          "AND [MergedService].FromDate < @From " +
                                          "AND [MergedService].Deleted = 0 " +
                                          "AND [MergedService].[AccountingActProvidingServiceID] IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[DeliveryExpense].AccountingGrossAmount, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [DeliveryExpense] " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                          "LEFT JOIN [ActProvidingService] " +
                                          "ON [ActProvidingService].ID = [DeliveryExpense].AccountingActProvidingServiceID " +
                                          "WHERE [DeliveryExpense].SupplyOrganizationID = @Id " +
                                          "AND [DeliveryExpense].FromDate < @From " +
                                          "AND [DeliveryExpense].Deleted = 0 " +
                                          "AND [DeliveryExpense].AccountingActProvidingServiceID IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "([ConsumablesOrderItem].TotalPriceWithVAT), " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [ConsumablesOrder] " +
                                          "LEFT JOIN [ConsumablesOrderItem] " +
                                          "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                                          "AND [ConsumablesOrderItem].Deleted = 0 " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                                          "WHERE [ConsumablesOrderItem].ConsumableProductOrganizationID = @Id " +
                                          "AND [ConsumablesOrder].OrganizationFromDate < @From " +
                                          "AND [ConsumablesOrder].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([IncomePaymentOrder].EuroAmount), 0) " +
                                          "FROM [IncomePaymentOrder] " +
                                          "WHERE [IncomePaymentOrder].SupplyOrganizationID = @Id " +
                                          "AND [IncomePaymentOrder].FromDate < @From " +
                                          "AND [IncomePaymentOrder].Deleted = 0 " +
                                          "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                          "AND [IncomePaymentOrder].IsAccounting = 1 " +
                                          "AND [IncomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
                                          ") " +
                                          ", 2) AS [BeforeRangeInAmount]";
        else
            beforeRangeInAmountSqlQuery = "SELECT " +
                                          "ROUND( " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].NetPrice + [AccountingPaymentTask].NetPrice, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [ContainerService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [ContainerService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].ID = [ContainerService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [BillOfLadingDocument] " +
                                          "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                                          "WHERE [ContainerService].ContainerOrganizationID = @Id " +
                                          "AND [BillOfLadingDocument].[Date] < @From " +
                                          "AND [ContainerService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].NetPrice + [AccountingPaymentTask].[NetPrice], " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [VehicleService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [VehicleService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].ID = [VehicleService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [BillOfLadingDocument] " +
                                          "ON [BillOfLadingDocument].ID = [VehicleService].BillOfLadingDocumentID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                                          "WHERE [VehicleService].VehicleOrganizationID = @Id " +
                                          "AND [BillOfLadingDocument].[Date] < @From " +
                                          "AND [VehicleService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].NetPrice + [AccountingPaymentTask].[NetPrice], " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [BillOfLadingService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [BillOfLadingService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].ID = [BillOfLadingService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [BillOfLadingDocument] " +
                                          "ON [BillOfLadingDocument].BillOfLadingServiceID = [BillOfLadingService].ID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [BillOfLadingService].SupplyOrganizationAgreementID " +
                                          "WHERE [BillOfLadingService].SupplyOrganizationID = @Id " +
                                          "AND [BillOfLadingDocument].[Date] < @From " +
                                          "AND [BillOfLadingService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].[GrossPrice], " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [CustomService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].ID = [CustomService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                                          "WHERE " +
                                          "( " +
                                          "[CustomService].CustomOrganizationID = @Id " +
                                          "OR [CustomService].ExciseDutyOrganizationID = @Id " +
                                          ") " +
                                          "AND [CustomService].FromDate < @From " +
                                          "AND [CustomService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].[GrossPrice], " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [PortWorkService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].ID = [PortWorkService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                                          "WHERE [PortWorkService].PortWorkOrganizationID = @Id " +
                                          "AND [PortWorkService].FromDate < @From " +
                                          "AND [PortWorkService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].[GrossPrice] , " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [TransportationService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].ID = [TransportationService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                                          "WHERE [TransportationService].TransportationOrganizationID = @Id " +
                                          "AND [TransportationService].FromDate < @From " +
                                          "AND [TransportationService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].[GrossPrice], " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [PortCustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].ID = [PortCustomAgencyService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                                          "WHERE [PortCustomAgencyService].PortCustomAgencyOrganizationID = @Id " +
                                          "AND [PortCustomAgencyService].FromDate < @From " +
                                          "AND [PortCustomAgencyService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].[GrossPrice], " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [CustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].ID = [CustomAgencyService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                                          "WHERE [CustomAgencyService].CustomAgencyOrganizationID = @Id " +
                                          "AND [CustomAgencyService].FromDate < @From " +
                                          "AND [CustomAgencyService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].[GrossPrice], " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [PlaneDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].ID = [PlaneDeliveryService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                                          "WHERE [PlaneDeliveryService].PlaneDeliveryOrganizationID = @Id " +
                                          "AND [PlaneDeliveryService].FromDate < @From " +
                                          "AND [PlaneDeliveryService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].[GrossPrice], " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [VehicleDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
                                          "LEFT JOIN [SupplyPaymentTask] [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].ID = [VehicleDeliveryService].AccountingPaymentTaskID " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                                          "WHERE [VehicleDeliveryService].VehicleDeliveryOrganizationID = @Id " +
                                          "AND [VehicleDeliveryService].FromDate < @From " +
                                          "AND [VehicleDeliveryService].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[MergedService].[GrossPrice] + [MergedService].[AccountingGrossPrice], " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [MergedService] " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                                          "WHERE [MergedService].SupplyOrganizationID = @Id " +
                                          "AND [MergedService].FromDate < @From " +
                                          "AND [MergedService].Deleted = 0 " +
                                          "AND ([MergedService].[ActProvidingServiceID] IS NOT NULL " +
                                          "OR [MergedService].[AccountingActProvidingServiceID] IS NOT NULL) " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[DeliveryExpense].GrossAmount, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [DeliveryExpense] " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                          "LEFT JOIN [ActProvidingService] " +
                                          "ON [ActProvidingService].ID = [DeliveryExpense].ActProvidingServiceID " +
                                          "WHERE [DeliveryExpense].SupplyOrganizationID = @Id " +
                                          "AND [DeliveryExpense].FromDate < @From " +
                                          "AND [DeliveryExpense].Deleted = 0 " +
                                          "AND [DeliveryExpense].ActProvidingServiceID IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "[DeliveryExpense].AccountingGrossAmount, " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [DeliveryExpense] " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                          "LEFT JOIN [ActProvidingService] " +
                                          "ON [ActProvidingService].ID = [DeliveryExpense].AccountingActProvidingServiceID " +
                                          "WHERE [DeliveryExpense].SupplyOrganizationID = @Id " +
                                          "AND [DeliveryExpense].FromDate < @From " +
                                          "AND [DeliveryExpense].Deleted = 0 " +
                                          "AND [DeliveryExpense].AccountingActProvidingServiceID IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM( " +
                                          "[dbo].[GetExchangedToEuroValue] " +
                                          "( " +
                                          "([ConsumablesOrderItem].TotalPriceWithVAT), " +
                                          "[SupplyOrganizationAgreement].CurrencyID, " +
                                          "GETUTCDATE() " +
                                          ") " +
                                          "), 0) " +
                                          "FROM [ConsumablesOrder] " +
                                          "LEFT JOIN [ConsumablesOrderItem] " +
                                          "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                                          "AND [ConsumablesOrderItem].Deleted = 0 " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                                          "WHERE [ConsumablesOrderItem].ConsumableProductOrganizationID = @Id " +
                                          "AND [ConsumablesOrder].OrganizationFromDate < @From " +
                                          "AND [ConsumablesOrder].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([IncomePaymentOrder].EuroAmount), 0) " +
                                          "FROM [IncomePaymentOrder] " +
                                          "WHERE [IncomePaymentOrder].SupplyOrganizationID = @Id " +
                                          "AND [IncomePaymentOrder].FromDate < @From " +
                                          "AND [IncomePaymentOrder].Deleted = 0 " +
                                          "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                          "AND [IncomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
                                          ") " +
                                          ", 2) AS [BeforeRangeInAmount]";

        AccountingCashFlow accountingCashFlow = new(supplyOrganization) {
            BeforeRangeInAmount =
                _connection.Query<decimal>(
                    beforeRangeInAmountSqlQuery,
                    new { supplyOrganization.Id, From = from }
                ).Single(),
            BeforeRangeOutAmount =
                _connection.Query<decimal>(
                    "SELECT " +
                    "ROUND( " +
                    "( " +
                    "SELECT " +
                    "ISNULL( " +
                    "SUM( " +
                    "[dbo].[GetExchangedToEuroValue] " +
                    "( " +
                    "[OutcomePaymentOrder].AfterExchangeAmount, " +
                    "[SupplyOrganizationAgreement].CurrencyID, " +
                    "GETUTCDATE() " +
                    ") " +
                    ") " +
                    ", 0) " +
                    "FROM [OutcomePaymentOrder] " +
                    "LEFT JOIN [SupplyOrganizationAgreement] " +
                    "ON [SupplyOrganizationAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                    "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
                    "LEFT JOIN [SupplyPaymentTask] " +
                    "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
                    "WHERE [OutcomePaymentOrder].Deleted = 0 " +
                    "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                    "AND [OutcomePaymentOrder].ConsumableProductOrganizationID = @Id " +
                    "AND [OutcomePaymentOrder].FromDate < @From " +
                    "AND ([OutcomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
                    "OR [OutcomePaymentOrder].ClientAgreementID IS NOT NULL) " +
                    (
                        typePaymentTask.Equals(TypePaymentTask.All)
                            ? "AND ([SupplyPaymentTask].ID IS NOT NULL AND [SupplyPaymentTask].[IsAccounting] = @IsAccounting " +
                              "OR [SupplyPaymentTask].ID IS NULL AND [OutcomePaymentOrder].[IsAccounting] = @IsAccounting) "
                            : ""
                    ) +
                    ") " +
                    ", 2) AS [BeforeRangeOutAmount]",
                    new { supplyOrganization.Id, From = from, IsAccounting = typePaymentTask }
                ).Single()
        };

        accountingCashFlow.BeforeRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.BeforeRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        accountingCashFlow.BeforeRangeBalance = Math.Round(accountingCashFlow.BeforeRangeInAmount - accountingCashFlow.BeforeRangeOutAmount, 2);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.AfterRangeInAmount);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.AfterRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        decimal currentStepBalance = accountingCashFlow.BeforeRangeBalance;

        List<JoinService> joinServices = new();

        string joinServicesqlQuery;

        if (typePaymentTask.Equals(TypePaymentTask.PaymentTask))
            joinServicesqlQuery = ";WITH [AccountingCashFlow_CTE] " +
                                  "AS " +
                                  "( " +
                                  "SELECT [ContainerService].ID " +
                                  ", 2 AS [Type] " +
                                  ", [ContainerService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [ContainerService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ContainerService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                                  "WHERE [ContainerService].ContainerOrganizationID = @Id " +
                                  "AND [ContainerService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ContainerService].FromDate >= @From " +
                                  "AND [ContainerService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortWorkService].ID " +
                                  ", 4 AS [Type] " +
                                  ", [PortWorkService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PortWorkService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                                  "WHERE [PortWorkService].PortWorkOrganizationID = @Id " +
                                  "AND [PortWorkService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortWorkService].FromDate >= @From " +
                                  "AND [PortWorkService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleService].ID " +
                                  ", 21 AS [Type] " +
                                  ", [VehicleService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [VehicleService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                                  "WHERE [VehicleService].VehicleOrganizationID = @Id " +
                                  "AND [VehicleService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleService].FromDate >= @From " +
                                  "AND [VehicleService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [BillOfLadingService].ID " +
                                  ", 33 AS [Type] " +
                                  ", [BillOfLadingService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [BillOfLadingService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [BillOfLadingService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [BillOfLadingService].SupplyOrganizationAgreementID " +
                                  "WHERE [BillOfLadingService].SupplyOrganizationID = @Id " +
                                  "AND [BillOfLadingService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [BillOfLadingService].FromDate >= @From " +
                                  "AND [BillOfLadingService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomService].ID " +
                                  ", 3 AS [Type] " +
                                  ", [CustomService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [CustomService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                                  "WHERE " +
                                  "( " +
                                  "[CustomService].CustomOrganizationID = @Id " +
                                  "OR [CustomService].ExciseDutyOrganizationID = @Id " +
                                  ") " +
                                  "AND [CustomService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomService].FromDate >= @From " +
                                  "AND [CustomService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [TransportationService].ID " +
                                  ", 5 AS [Type] " +
                                  ", [TransportationService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [TransportationService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                                  "WHERE [TransportationService].TransportationOrganizationID = @Id " +
                                  "AND [TransportationService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [TransportationService].FromDate >= @From " +
                                  "AND [TransportationService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortCustomAgencyService].ID " +
                                  ", 6 AS [Type] " +
                                  ", [PortCustomAgencyService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PortCustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                                  "WHERE [PortCustomAgencyService].PortCustomAgencyOrganizationID = @Id " +
                                  "AND [PortCustomAgencyService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].FromDate >= @From " +
                                  "AND [PortCustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomAgencyService].ID " +
                                  ", 7 AS [Type] " +
                                  ", [CustomAgencyService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [CustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                                  "WHERE [CustomAgencyService].CustomAgencyOrganizationID = @Id " +
                                  "AND [CustomAgencyService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomAgencyService].FromDate >= @From " +
                                  "AND [CustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PlaneDeliveryService].ID " +
                                  ", 8 AS [Type] " +
                                  ", [PlaneDeliveryService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PlaneDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                                  "WHERE [PlaneDeliveryService].PlaneDeliveryOrganizationID = @Id " +
                                  "AND [PlaneDeliveryService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].FromDate >= @From " +
                                  "AND [PlaneDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleDeliveryService].ID " +
                                  ", 9 AS [Type] " +
                                  ", [VehicleDeliveryService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [VehicleDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                                  "WHERE [VehicleDeliveryService].VehicleDeliveryOrganizationID = @Id " +
                                  "AND [VehicleDeliveryService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].FromDate >= @From " +
                                  "AND [VehicleDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [MergedService].ID " +
                                  ", 17 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[MergedService].[GrossPrice], " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [MergedService] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].[ID] = [MergedService].[ActProvidingServiceID] " +
                                  "WHERE [MergedService].SupplyOrganizationID = @Id " +
                                  "AND [MergedService].Deleted = 0 " +
                                  "AND [MergedService].FromDate >= @From " +
                                  "AND [MergedService].FromDate <= @To " +
                                  "AND [MergedService].[ActProvidingServiceID] IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [DeliveryExpense].ID " +
                                  ", 39 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[DeliveryExpense].[GrossAmount], " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [DeliveryExpense] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].ID = [DeliveryExpense].ActProvidingServiceID " +
                                  "WHERE [DeliveryExpense].SupplyOrganizationID = @Id " +
                                  "AND [DeliveryExpense].Deleted = 0 " +
                                  "AND [DeliveryExpense].FromDate >= @From " +
                                  "AND [DeliveryExpense].FromDate <= @To " +
                                  "AND [DeliveryExpense].ActProvidingServiceID IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [ConsumablesOrder].ID " +
                                  ", 10 AS [Type] " +
                                  ", [ConsumablesOrder].OrganizationFromDate AS [FromDate] " +
                                  ", (CASE WHEN " +
                                  "[SupplyPaymentTask].GrossPrice IS NOT NULL " +
                                  "THEN " +
                                  "[dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") ELSE " +
                                  "0 END) AS [GrossPrice] " +
                                  "FROM [ConsumablesOrderItem] " +
                                  "LEFT JOIN [ConsumablesOrder] " +
                                  "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                                  "WHERE [ConsumablesOrderItem].ConsumableProductOrganizationID = @Id " +
                                  "AND [ConsumablesOrderItem].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ConsumablesOrder].OrganizationFromDate >= @From " +
                                  "AND [ConsumablesOrder].OrganizationFromDate <= @To " +
                                  "UNION " +
                                  "SELECT [IncomePaymentOrder].ID " +
                                  ", 12 AS [Type] " +
                                  ", [IncomePaymentOrder].FromDate " +
                                  ", [IncomePaymentOrder].EuroAmount AS [GrossPrice] " +
                                  "FROM [IncomePaymentOrder] " +
                                  "WHERE [IncomePaymentOrder].SupplyOrganizationID = @Id " +
                                  "AND [IncomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
                                  "AND [IncomePaymentOrder].Deleted = 0 " +
                                  "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                  "AND [IncomePaymentOrder].IsManagementAccounting = 1 " +
                                  "AND [IncomePaymentOrder].FromDate >= @From " +
                                  "AND [IncomePaymentOrder].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [OutcomePaymentOrder].ID AS [ID] " +
                                  ", 11 AS [Type] " +
                                  ", [OutcomePaymentOrder].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[OutcomePaymentOrder].AfterExchangeAmount, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [OutcomePaymentOrder] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                                  "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
                                  "WHERE [OutcomePaymentOrder].ConsumableProductOrganizationID = @Id " +
                                  "AND [OutcomePaymentOrder].Deleted = 0 " +
                                  "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                                  "AND [OutcomePaymentOrder].FromDate >= @From " +
                                  "AND [OutcomePaymentOrder].FromDate <= @To " +
                                  "AND ([SupplyPaymentTask].ID IS NOT NULL AND [SupplyPaymentTask].[IsAccounting] = 0 " +
                                  "OR [SupplyPaymentTask].ID IS NULL AND [OutcomePaymentOrder].[IsManagementAccounting] = 1) " +
                                  ") " +
                                  "SELECT * " +
                                  "FROM [AccountingCashFlow_CTE] " +
                                  "ORDER BY [AccountingCashFlow_CTE].FromDate";
        else if (typePaymentTask.Equals(TypePaymentTask.AccountingPaymentTask))
            joinServicesqlQuery = ";WITH [AccountingCashFlow_CTE] " +
                                  "AS " +
                                  "( " +
                                  "SELECT [ContainerService].ID " +
                                  ", 31 AS [Type] " +
                                  ", [ContainerService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [ContainerService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ContainerService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                                  "WHERE [ContainerService].ContainerOrganizationID = @Id " +
                                  "AND [ContainerService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ContainerService].FromDate >= @From " +
                                  "AND [ContainerService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortWorkService].ID " +
                                  ", 32 AS [Type] " +
                                  ", [PortWorkService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PortWorkService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortWorkService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                                  "WHERE [PortWorkService].PortWorkOrganizationID = @Id " +
                                  "AND [PortWorkService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortWorkService].FromDate >= @From " +
                                  "AND [PortWorkService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleService].ID " +
                                  ", 23 AS [Type] " +
                                  ", [VehicleService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [VehicleService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                                  "WHERE [VehicleService].VehicleOrganizationID = @Id " +
                                  "AND [VehicleService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleService].FromDate >= @From " +
                                  "AND [VehicleService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [BillOfLadingService].ID " +
                                  ", 34 AS [Type] " +
                                  ", [BillOfLadingService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [BillOfLadingService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [BillOfLadingService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [BillOfLadingService].SupplyOrganizationAgreementID " +
                                  "WHERE [BillOfLadingService].SupplyOrganizationID = @Id " +
                                  "AND [BillOfLadingService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [BillOfLadingService].FromDate >= @From " +
                                  "AND [BillOfLadingService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomService].ID " +
                                  ", 24 AS [Type] " +
                                  ", [CustomService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [CustomService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                                  "WHERE " +
                                  "( " +
                                  "[CustomService].CustomOrganizationID = @Id " +
                                  "OR [CustomService].ExciseDutyOrganizationID = @Id " +
                                  ") " +
                                  "AND [CustomService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomService].FromDate >= @From " +
                                  "AND [CustomService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [TransportationService].ID " +
                                  ", 25 AS [Type] " +
                                  ", [TransportationService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [TransportationService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [TransportationService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                                  "WHERE [TransportationService].TransportationOrganizationID = @Id " +
                                  "AND [TransportationService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [TransportationService].FromDate >= @From " +
                                  "AND [TransportationService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortCustomAgencyService].ID " +
                                  ", 26 AS [Type] " +
                                  ", [PortCustomAgencyService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PortCustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                                  "WHERE [PortCustomAgencyService].PortCustomAgencyOrganizationID = @Id " +
                                  "AND [PortCustomAgencyService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].FromDate >= @From " +
                                  "AND [PortCustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomAgencyService].ID " +
                                  ", 27 AS [Type] " +
                                  ", [CustomAgencyService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [CustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomAgencyService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                                  "WHERE [CustomAgencyService].CustomAgencyOrganizationID = @Id " +
                                  "AND [CustomAgencyService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomAgencyService].FromDate >= @From " +
                                  "AND [CustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PlaneDeliveryService].ID " +
                                  ", 28 AS [Type] " +
                                  ", [PlaneDeliveryService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PlaneDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                                  "WHERE [PlaneDeliveryService].PlaneDeliveryOrganizationID = @Id " +
                                  "AND [PlaneDeliveryService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].FromDate >= @From " +
                                  "AND [PlaneDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleDeliveryService].ID " +
                                  ", 29 AS [Type] " +
                                  ", [VehicleDeliveryService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [VehicleDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                                  "WHERE [VehicleDeliveryService].VehicleDeliveryOrganizationID = @Id " +
                                  "AND [VehicleDeliveryService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].FromDate >= @From " +
                                  "AND [VehicleDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [MergedService].ID " +
                                  ", 30 AS [Type] " +
                                  ", [ActProvidingService].[FromDate] " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[MergedService].[AccountingGrossPrice], " +
                                  "[SupplyOrganizationAgreement].[CurrencyID], " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [MergedService] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].[ID] = [MergedService].[AccountingActProvidingServiceID] " +
                                  "WHERE [MergedService].[SupplyOrganizationID] = @Id " +
                                  "AND [MergedService].[Deleted] = 0 " +
                                  "AND [MergedService].[FromDate] >= @From " +
                                  "AND [MergedService].[FromDate] <= @To " +
                                  "AND [MergedService].[AccountingActProvidingServiceID] IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [DeliveryExpense].ID " +
                                  ", 40 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[DeliveryExpense].[AccountingGrossAmount], " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [DeliveryExpense] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].ID = [DeliveryExpense].AccountingActProvidingServiceID " +
                                  "WHERE [DeliveryExpense].SupplyOrganizationID = @Id " +
                                  "AND [DeliveryExpense].Deleted = 0 " +
                                  "AND [DeliveryExpense].FromDate >= @From " +
                                  "AND [DeliveryExpense].FromDate <= @To " +
                                  "AND [DeliveryExpense].AccountingActProvidingServiceID IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [ConsumablesOrder].ID " +
                                  ", 10 AS [Type] " +
                                  ", [ConsumablesOrder].OrganizationFromDate AS [FromDate] " +
                                  ", (CASE WHEN " +
                                  "[SupplyPaymentTask].GrossPrice IS NOT NULL " +
                                  "THEN " +
                                  "[dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") ELSE " +
                                  "0 END) AS [GrossPrice] " +
                                  "FROM [ConsumablesOrderItem] " +
                                  "LEFT JOIN [ConsumablesOrder] " +
                                  "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                                  "WHERE [ConsumablesOrderItem].ConsumableProductOrganizationID = @Id " +
                                  "AND [ConsumablesOrderItem].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ConsumablesOrder].OrganizationFromDate >= @From " +
                                  "AND [ConsumablesOrder].OrganizationFromDate <= @To " +
                                  "UNION " +
                                  "SELECT [IncomePaymentOrder].ID " +
                                  ", 12 AS [Type] " +
                                  ", [IncomePaymentOrder].FromDate " +
                                  ", [IncomePaymentOrder].EuroAmount AS [GrossPrice] " +
                                  "FROM [IncomePaymentOrder] " +
                                  "WHERE [IncomePaymentOrder].SupplyOrganizationID = @Id " +
                                  "AND [IncomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
                                  "AND [IncomePaymentOrder].Deleted = 0 " +
                                  "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                  "AND [IncomePaymentOrder].IsAccounting = 1 " +
                                  "AND [IncomePaymentOrder].FromDate >= @From " +
                                  "AND [IncomePaymentOrder].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [OutcomePaymentOrder].ID AS [ID] " +
                                  ", 11 AS [Type] " +
                                  ", [OutcomePaymentOrder].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[OutcomePaymentOrder].AfterExchangeAmount, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [OutcomePaymentOrder] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                                  "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
                                  "WHERE [OutcomePaymentOrder].ConsumableProductOrganizationID = @Id " +
                                  "AND [OutcomePaymentOrder].Deleted = 0 " +
                                  "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                                  "AND [OutcomePaymentOrder].FromDate >= @From " +
                                  "AND [OutcomePaymentOrder].FromDate <= @To " +
                                  "AND ([SupplyPaymentTask].ID IS NOT NULL AND [SupplyPaymentTask].[IsAccounting] = 1 " +
                                  "OR [SupplyPaymentTask].ID IS NULL AND [OutcomePaymentOrder].[IsAccounting] = 1) " +
                                  ") " +
                                  "SELECT * " +
                                  "FROM [AccountingCashFlow_CTE] " +
                                  "ORDER BY [AccountingCashFlow_CTE].FromDate";
        else
            joinServicesqlQuery = ";WITH [AccountingCashFlow_CTE] " +
                                  "AS " +
                                  "( " +
                                  "SELECT [ContainerService].ID " +
                                  ", 2 AS [Type] " +
                                  ", [ContainerService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [ContainerService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ContainerService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                                  "WHERE [ContainerService].ContainerOrganizationID = @Id " +
                                  "AND [ContainerService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ContainerService].FromDate >= @From " +
                                  "AND [ContainerService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortWorkService].ID " +
                                  ", 4 AS [Type] " +
                                  ", [PortWorkService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PortWorkService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                                  "WHERE [PortWorkService].PortWorkOrganizationID = @Id " +
                                  "AND [PortWorkService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortWorkService].FromDate >= @From " +
                                  "AND [PortWorkService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleService].ID " +
                                  ", 21 AS [Type] " +
                                  ", [VehicleService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [VehicleService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                                  "WHERE [VehicleService].VehicleOrganizationID = @Id " +
                                  "AND [VehicleService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleService].FromDate >= @From " +
                                  "AND [VehicleService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [BillOfLadingService].ID " +
                                  ", 33 AS [Type] " +
                                  ", [BillOfLadingService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [BillOfLadingService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [BillOfLadingService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [BillOfLadingService].SupplyOrganizationAgreementID " +
                                  "WHERE [BillOfLadingService].SupplyOrganizationID = @Id " +
                                  "AND [BillOfLadingService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [BillOfLadingService].FromDate >= @From " +
                                  "AND [BillOfLadingService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomService].ID " +
                                  ", 3 AS [Type] " +
                                  ", [CustomService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [CustomService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                                  "WHERE " +
                                  "( " +
                                  "[CustomService].CustomOrganizationID = @Id " +
                                  "OR [CustomService].ExciseDutyOrganizationID = @Id " +
                                  ") " +
                                  "AND [CustomService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomService].FromDate >= @From " +
                                  "AND [CustomService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [TransportationService].ID " +
                                  ", 5 AS [Type] " +
                                  ", [TransportationService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [TransportationService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                                  "WHERE [TransportationService].TransportationOrganizationID = @Id " +
                                  "AND [TransportationService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [TransportationService].FromDate >= @From " +
                                  "AND [TransportationService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortCustomAgencyService].ID " +
                                  ", 6 AS [Type] " +
                                  ", [PortCustomAgencyService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PortCustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                                  "WHERE [PortCustomAgencyService].PortCustomAgencyOrganizationID = @Id " +
                                  "AND [PortCustomAgencyService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].FromDate >= @From " +
                                  "AND [PortCustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomAgencyService].ID " +
                                  ", 7 AS [Type] " +
                                  ", [CustomAgencyService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [CustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                                  "WHERE [CustomAgencyService].CustomAgencyOrganizationID = @Id " +
                                  "AND [CustomAgencyService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomAgencyService].FromDate >= @From " +
                                  "AND [CustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PlaneDeliveryService].ID " +
                                  ", 8 AS [Type] " +
                                  ", [PlaneDeliveryService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PlaneDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                                  "WHERE [PlaneDeliveryService].PlaneDeliveryOrganizationID = @Id " +
                                  "AND [PlaneDeliveryService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].FromDate >= @From " +
                                  "AND [PlaneDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleDeliveryService].ID " +
                                  ", 9 AS [Type] " +
                                  ", [VehicleDeliveryService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [VehicleDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                                  "WHERE [VehicleDeliveryService].VehicleDeliveryOrganizationID = @Id " +
                                  "AND [VehicleDeliveryService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].FromDate >= @From " +
                                  "AND [VehicleDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [MergedService].ID " +
                                  ", 17 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[MergedService].[GrossPrice], " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [MergedService] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].[ID] = [MergedService].[ActProvidingServiceID] " +
                                  "WHERE [MergedService].SupplyOrganizationID = @Id " +
                                  "AND [MergedService].Deleted = 0 " +
                                  "AND [MergedService].FromDate >= @From " +
                                  "AND [MergedService].FromDate <= @To " +
                                  "AND [MergedService].[ActProvidingServiceID] IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [DeliveryExpense].ID " +
                                  ", 39 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[DeliveryExpense].[GrossAmount], " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [DeliveryExpense] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].ID = [DeliveryExpense].ActProvidingServiceID " +
                                  "WHERE [DeliveryExpense].SupplyOrganizationID = @Id " +
                                  "AND [DeliveryExpense].Deleted = 0 " +
                                  "AND [DeliveryExpense].FromDate >= @From " +
                                  "AND [DeliveryExpense].FromDate <= @To " +
                                  "AND [DeliveryExpense].ActProvidingServiceID IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [DeliveryExpense].ID " +
                                  ", 40 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[DeliveryExpense].[AccountingGrossAmount], " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [DeliveryExpense] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].ID = [DeliveryExpense].AccountingActProvidingServiceID " +
                                  "WHERE [DeliveryExpense].SupplyOrganizationID = @Id " +
                                  "AND [DeliveryExpense].Deleted = 0 " +
                                  "AND [DeliveryExpense].FromDate >= @From " +
                                  "AND [DeliveryExpense].FromDate <= @To " +
                                  "AND [DeliveryExpense].AccountingActProvidingServiceID IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [ConsumablesOrder].ID " +
                                  ", 10 AS [Type] " +
                                  ", [ConsumablesOrder].OrganizationFromDate AS [FromDate] " +
                                  ", (CASE WHEN " +
                                  "[SupplyPaymentTask].GrossPrice IS NOT NULL " +
                                  "THEN " +
                                  "[dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") ELSE " +
                                  "0 END) AS [GrossPrice] " +
                                  "FROM [ConsumablesOrderItem] " +
                                  "LEFT JOIN [ConsumablesOrder] " +
                                  "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                                  "WHERE [ConsumablesOrderItem].ConsumableProductOrganizationID = @Id " +
                                  "AND [ConsumablesOrderItem].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ConsumablesOrder].OrganizationFromDate >= @From " +
                                  "AND [ConsumablesOrder].OrganizationFromDate <= @To " +
                                  "UNION " +
                                  "SELECT [OutcomePaymentOrder].ID AS [ID] " +
                                  ", 11 AS [Type] " +
                                  ", [OutcomePaymentOrder].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[OutcomePaymentOrder].AfterExchangeAmount, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [OutcomePaymentOrder] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                                  "WHERE [OutcomePaymentOrder].ConsumableProductOrganizationID = @Id " +
                                  "AND [OutcomePaymentOrder].Deleted = 0 " +
                                  "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                                  "AND [OutcomePaymentOrder].FromDate >= @From " +
                                  "AND [OutcomePaymentOrder].FromDate <= @To " +
                                  "AND ([OutcomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
                                  "OR [OutcomePaymentOrder].ClientAgreementID IS NOT NULL) " +
                                  "UNION " +
                                  "SELECT [IncomePaymentOrder].ID " +
                                  ", 12 AS [Type] " +
                                  ", [IncomePaymentOrder].FromDate " +
                                  ", [IncomePaymentOrder].EuroAmount AS [GrossPrice] " +
                                  "FROM [IncomePaymentOrder] " +
                                  "WHERE [IncomePaymentOrder].SupplyOrganizationID = @Id " +
                                  "AND [IncomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
                                  "AND [IncomePaymentOrder].Deleted = 0 " +
                                  "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                  "AND [IncomePaymentOrder].FromDate >= @From " +
                                  "AND [IncomePaymentOrder].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [ContainerService].ID " +
                                  ", 31 AS [Type] " +
                                  ", [ContainerService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [ContainerService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ContainerService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                                  "WHERE [ContainerService].ContainerOrganizationID = @Id " +
                                  "AND [ContainerService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ContainerService].FromDate >= @From " +
                                  "AND [ContainerService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortWorkService].ID " +
                                  ", 32 AS [Type] " +
                                  ", [PortWorkService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PortWorkService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortWorkService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                                  "WHERE [PortWorkService].PortWorkOrganizationID = @Id " +
                                  "AND [PortWorkService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortWorkService].FromDate >= @From " +
                                  "AND [PortWorkService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleService].ID " +
                                  ", 23 AS [Type] " +
                                  ", [VehicleService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [VehicleService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                                  "WHERE [VehicleService].VehicleOrganizationID = @Id " +
                                  "AND [VehicleService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleService].FromDate >= @From " +
                                  "AND [VehicleService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [BillOfLadingService].ID " +
                                  ", 34 AS [Type] " +
                                  ", [BillOfLadingService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].NetPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [BillOfLadingService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [BillOfLadingService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [BillOfLadingService].SupplyOrganizationAgreementID " +
                                  "WHERE [BillOfLadingService].SupplyOrganizationID = @Id " +
                                  "AND [BillOfLadingService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [BillOfLadingService].FromDate >= @From " +
                                  "AND [BillOfLadingService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomService].ID " +
                                  ", 24 AS [Type] " +
                                  ", [CustomService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [CustomService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                                  "WHERE " +
                                  "( " +
                                  "[CustomService].CustomOrganizationID = @Id " +
                                  "OR [CustomService].ExciseDutyOrganizationID = @Id " +
                                  ") " +
                                  "AND [CustomService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomService].FromDate >= @From " +
                                  "AND [CustomService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [TransportationService].ID " +
                                  ", 25 AS [Type] " +
                                  ", [TransportationService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [TransportationService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [TransportationService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                                  "WHERE [TransportationService].TransportationOrganizationID = @Id " +
                                  "AND [TransportationService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [TransportationService].FromDate >= @From " +
                                  "AND [TransportationService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortCustomAgencyService].ID " +
                                  ", 26 AS [Type] " +
                                  ", [PortCustomAgencyService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PortCustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                                  "WHERE [PortCustomAgencyService].PortCustomAgencyOrganizationID = @Id " +
                                  "AND [PortCustomAgencyService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].FromDate >= @From " +
                                  "AND [PortCustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomAgencyService].ID " +
                                  ", 27 AS [Type] " +
                                  ", [CustomAgencyService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [CustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomAgencyService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                                  "WHERE [CustomAgencyService].CustomAgencyOrganizationID = @Id " +
                                  "AND [CustomAgencyService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomAgencyService].FromDate >= @From " +
                                  "AND [CustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PlaneDeliveryService].ID " +
                                  ", 28 AS [Type] " +
                                  ", [PlaneDeliveryService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [PlaneDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                                  "WHERE [PlaneDeliveryService].PlaneDeliveryOrganizationID = @Id " +
                                  "AND [PlaneDeliveryService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].FromDate >= @From " +
                                  "AND [PlaneDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleDeliveryService].ID " +
                                  ", 29 AS [Type] " +
                                  ", [VehicleDeliveryService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[SupplyPaymentTask].GrossPrice, " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [VehicleDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].AccountingPaymentTaskID " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                                  "WHERE [VehicleDeliveryService].VehicleDeliveryOrganizationID = @Id " +
                                  "AND [VehicleDeliveryService].Deleted = 0 " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].FromDate >= @From " +
                                  "AND [VehicleDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [MergedService].ID " +
                                  ", 30 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", [dbo].[GetExchangedToEuroValue] " +
                                  "( " +
                                  "[MergedService].[AccountingGrossPrice], " +
                                  "[SupplyOrganizationAgreement].CurrencyID, " +
                                  "GETUTCDATE() " +
                                  ") AS [GrossPrice] " +
                                  "FROM [MergedService] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].[ID] = [MergedService].[AccountingActProvidingServiceID] " +
                                  "WHERE [MergedService].SupplyOrganizationID = @Id " +
                                  "AND [MergedService].Deleted = 0 " +
                                  "AND [MergedService].FromDate >= @From " +
                                  "AND [MergedService].FromDate <= @To " +
                                  "AND [MergedService].[AccountingActProvidingServiceID] IS NOT NULL " +
                                  ") " +
                                  "SELECT * " +
                                  "FROM [AccountingCashFlow_CTE] " +
                                  "ORDER BY [AccountingCashFlow_CTE].FromDate;";

        _connection.Query<JoinService, decimal, JoinService>(
            joinServicesqlQuery,
            (service, grossPrice) => {
                if (joinServices.Any(s => s.Type.Equals(service.Type) && s.Id.Equals(service.Id))) return service;

                switch (service.Type) {
                    case JoinServiceType.ContainerService:
                    case JoinServiceType.CustomService:
                    case JoinServiceType.PortWorkService:
                    case JoinServiceType.TransportationService:
                    case JoinServiceType.PortCustomAgencyService:
                    case JoinServiceType.CustomAgencyService:
                    case JoinServiceType.PlaneDeliveryService:
                    case JoinServiceType.VehicleDeliveryService:
                    case JoinServiceType.SupplyPaymentTask:
                    case JoinServiceType.ConsumablesOrder:
                    case JoinServiceType.MergedService:
                    case JoinServiceType.VehicleService:
                    case JoinServiceType.AccountingContainerPaymentTask:
                    case JoinServiceType.AccountingVehicleService:
                    case JoinServiceType.AccountingCustomService:
                    case JoinServiceType.AccountingTransportationService:
                    case JoinServiceType.AccountingPortCustomAgencyService:
                    case JoinServiceType.AccountingCustomAgencyService:
                    case JoinServiceType.AccountingPlaneDeliveryService:
                    case JoinServiceType.AccountingVehicleDeliveryService:
                    case JoinServiceType.AccountingMergedService:
                    case JoinServiceType.AccountingContainerService:
                    case JoinServiceType.AccountingPortWorkService:
                    case JoinServiceType.BillOfLadingService:
                    case JoinServiceType.DeliveryExpense:
                    case JoinServiceType.AccountingDeliveryExpense:
                    case JoinServiceType.AccountingBillOfLadingService:
                        currentStepBalance = Math.Round(currentStepBalance + grossPrice, 2);

                        accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + grossPrice, 2);

                        accountingCashFlow.AccountingCashFlowHeadItems.Add(
                            new AccountingCashFlowHeadItem {
                                CurrentBalance = currentStepBalance,
                                FromDate = service.FromDate,
                                Type = service.Type,
                                IsCreditValue = false,
                                CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                Id = service.Id
                            }
                        );
                        break;
                    case JoinServiceType.OutcomePaymentOrder:
                        currentStepBalance = Math.Round(currentStepBalance - grossPrice, 2);

                        accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + grossPrice, 2);

                        accountingCashFlow.AccountingCashFlowHeadItems.Add(
                            new AccountingCashFlowHeadItem {
                                CurrentBalance = currentStepBalance,
                                FromDate = service.FromDate,
                                Type = service.Type,
                                IsCreditValue = true,
                                CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                Id = service.Id
                            }
                        );
                        break;
                    case JoinServiceType.IncomePaymentOrder:
                        currentStepBalance = Math.Round(currentStepBalance + grossPrice, 2);

                        accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + grossPrice, 2);

                        accountingCashFlow.AccountingCashFlowHeadItems.Add(
                            new AccountingCashFlowHeadItem {
                                CurrentBalance = currentStepBalance,
                                FromDate = service.FromDate,
                                Type = service.Type,
                                CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                Id = service.Id
                            }
                        );
                        break;
                }

                joinServices.Add(service);

                return service;
            },
            new { supplyOrganization.Id, From = from, To = to },
            splitOn: "ID,GrossPrice"
        );

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ContainerService))) {
            Type[] joinTypes = {
                typeof(ContainerService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderContainerService),
                typeof(SupplyOrder),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], ContainerService> joinMapper = objects => {
                ContainerService containerService = (ContainerService)objects[0];
                SupplyOrganization containerOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                SupplyOrderContainerService junction = (SupplyOrderContainerService)objects[7];
                SupplyOrder supplyOrder = (SupplyOrder)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow
                        .AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.ContainerService) && i.Id.Equals(containerService.Id));
                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.ContainerService != null) {
                    if (invoiceDocument != null && !itemFromList.ContainerService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.ContainerService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction == null || itemFromList.ContainerService.SupplyOrderContainerServices.Any(j => j.Id.Equals(junction.Id)))
                        return containerService;

                    junction.SupplyOrder = supplyOrder;

                    itemFromList.ContainerService.SupplyOrderContainerServices.Add(junction);
                } else {
                    itemFromList.Number = containerService.ServiceNumber;

                    if (invoiceDocument != null) containerService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction != null) {
                        junction.SupplyOrder = supplyOrder;

                        containerService.SupplyOrderContainerServices.Add(junction);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;
                        supplyOrganizationAgreement.Organization = organization;
                    }

                    containerService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    containerService.ContainerOrganization = containerOrganization;
                    containerService.BillOfLadingDocument = billOfLadingDocument;
                    containerService.User = user;

                    itemFromList.ContainerService = containerService;
                }

                return containerService;
            };

            _connection.Query(
                "SELECT [ContainerService].[ID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[ContainerService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[ContainerService].[BillOfLadingDocumentID] " +
                ",[ContainerService].[Created] " +
                ",[ContainerService].[Deleted] " +
                ",[ContainerService].[IsActive] " +
                ",[ContainerService].[LoadDate] " +
                ",[ContainerService].[NetUID] " +
                ",[ContainerService].[TermDeliveryInDays] " +
                ",[ContainerService].[Updated] " +
                ",[ContainerService].[SupplyPaymentTaskID] " +
                ",[ContainerService].[UserID] " +
                ",[ContainerService].[ContainerOrganizationID] " +
                ",[ContainerService].[FromDate] " +
                ",[ContainerService].[GroosWeight] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[ContainerService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ",[ContainerService].[Vat] " +
                ",[ContainerService].[Number] " +
                ",[ContainerService].[Name] " +
                ",[ContainerService].[VatPercent] " +
                ",[ContainerService].[IsCalculatedExtraCharge] " +
                ",[ContainerService].[SupplyExtraChargeType] " +
                ",[ContainerService].[ContainerNumber] " +
                ",[ContainerService].[ServiceNumber] " +
                ",[ContainerService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[BillOfLadingDocument].* " +
                ",[InvoiceDocument].* " +
                ",[SupplyOrderContainerService].* " +
                ",[SupplyOrder].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [ContainerService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrderContainerService] " +
                "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ContainerService].UserID " +
                "WHERE [ContainerService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ContainerService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.VehicleService))) {
            Type[] joinTypes = {
                typeof(VehicleService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderVehicleService),
                typeof(SupplyOrder),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], VehicleService> joinMapper = objects => {
                VehicleService vehicleService = (VehicleService)objects[0];
                SupplyOrganization vehicleOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                SupplyOrderVehicleService junction = (SupplyOrderVehicleService)objects[7];
                SupplyOrder supplyOrder = (SupplyOrder)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow
                        .AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.VehicleService) && i.Id.Equals(vehicleService.Id));
                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.VehicleService != null) {
                    if (invoiceDocument != null && !itemFromList.VehicleService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.VehicleService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction == null || itemFromList.VehicleService.SupplyOrderVehicleServices.Any(j => j.Id.Equals(junction.Id)))
                        return vehicleService;

                    junction.SupplyOrder = supplyOrder;

                    itemFromList.VehicleService.SupplyOrderVehicleServices.Add(junction);
                } else {
                    itemFromList.Number = vehicleService.ServiceNumber;

                    if (invoiceDocument != null) vehicleService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction != null) {
                        junction.SupplyOrder = supplyOrder;

                        vehicleService.SupplyOrderVehicleServices.Add(junction);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;
                        supplyOrganizationAgreement.Organization = organization;
                    }

                    vehicleService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    vehicleService.VehicleOrganization = vehicleOrganization;
                    vehicleService.BillOfLadingDocument = billOfLadingDocument;
                    vehicleService.User = user;

                    itemFromList.VehicleService = vehicleService;
                }

                return vehicleService;
            };

            _connection.Query(
                "SELECT [VehicleService].[ID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[VehicleService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[VehicleService].[BillOfLadingDocumentID] " +
                ",[VehicleService].[Created] " +
                ",[VehicleService].[Deleted] " +
                ",[VehicleService].[IsActive] " +
                ",[VehicleService].[LoadDate] " +
                ",[VehicleService].[NetUID] " +
                ",[VehicleService].[TermDeliveryInDays] " +
                ",[VehicleService].[Updated] " +
                ",[VehicleService].[SupplyPaymentTaskID] " +
                ",[VehicleService].[UserID] " +
                ",[VehicleService].[VehicleOrganizationID] " +
                ",[VehicleService].[FromDate] " +
                ",[VehicleService].[GrossWeight] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[VehicleService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ",[VehicleService].[Vat] " +
                ",[VehicleService].[Number] " +
                ",[VehicleService].[Name] " +
                ",[VehicleService].[VatPercent] " +
                ",[VehicleService].[IsCalculatedExtraCharge] " +
                ",[VehicleService].[SupplyExtraChargeType] " +
                ",[VehicleService].[VehicleNumber] " +
                ",[VehicleService].[ServiceNumber] " +
                ",[VehicleService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[BillOfLadingDocument].* " +
                ",[InvoiceDocument].* " +
                ",[SupplyOrderVehicleService].* " +
                ",[SupplyOrder].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [VehicleService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [VehicleService].VehicleOrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [VehicleService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].VehicleServiceID = [VehicleService].ID " +
                "LEFT JOIN [SupplyOrderVehicleService] " +
                "ON [SupplyOrderVehicleService].VehicleServiceID = [VehicleService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderVehicleService].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [VehicleService].UserID " +
                "WHERE [VehicleService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.VehicleService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.CustomService))) {
            Type[] joinTypes = {
                typeof(CustomService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(OrganizationTranslation)
            };

            Func<object[], CustomService> joinMapper = objects => {
                CustomService customService = (CustomService)objects[0];
                SupplyOrganization customOrganization = (SupplyOrganization)objects[1];
                SupplyOrganization exciseDutyOrganization = (SupplyOrganization)objects[2];
                SupplyOrganizationAgreement exciseDutyOrganizationAgreement = (SupplyOrganizationAgreement)objects[3];
                Organization exciseDutyOrganizationOrganization = (Organization)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[10];
                Organization customOrganizationOrganization = (Organization)objects[11];
                Currency currency = (Currency)objects[12];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[13];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.CustomService) && i.Id.Equals(customService.Id));

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.CustomService != null) {
                    if (invoiceDocument != null && !itemFromList.CustomService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.CustomService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem == null || itemFromList.CustomService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id)))
                        return customService;

                    serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                    itemFromList.CustomService.ServiceDetailItems.Add(serviceDetailItem);

                    if (exciseDutyOrganizationAgreement != null && exciseDutyOrganization != null) {
                        exciseDutyOrganizationAgreement.Organization = exciseDutyOrganizationOrganization;
                        exciseDutyOrganization.SupplyOrganizationAgreements.Add(exciseDutyOrganizationAgreement);
                    }
                } else {
                    itemFromList.Number = customService.ServiceNumber;

                    if (invoiceDocument != null) customService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        customService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = customOrganizationOrganization;

                    if (exciseDutyOrganizationAgreement != null) exciseDutyOrganizationAgreement.Organization = exciseDutyOrganizationOrganization;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    if (exciseDutyOrganization != null && exciseDutyOrganizationAgreement != null) {
                        if (!exciseDutyOrganization.SupplyOrganizationAgreements.Any(x => x.Id == exciseDutyOrganizationAgreement.Id))
                            exciseDutyOrganization.SupplyOrganizationAgreements.Add(exciseDutyOrganizationAgreement);
                        else
                            exciseDutyOrganizationAgreement = exciseDutyOrganization.SupplyOrganizationAgreements.First(x => x.Id == exciseDutyOrganizationAgreement.Id);

                        exciseDutyOrganizationAgreement.Organization = exciseDutyOrganizationOrganization;
                    }

                    customService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    customService.CustomOrganization = customOrganization;
                    customService.ExciseDutyOrganization = exciseDutyOrganization;
                    customService.SupplyOrder = supplyOrder;
                    customService.User = user;

                    itemFromList.CustomService = customService;
                }

                return customService;
            };

            _connection.Query(
                "SELECT [CustomService].[ID] " +
                ",[CustomService].[Created] " +
                ",[CustomService].[Deleted] " +
                ",[CustomService].[IsActive] " +
                ",[CustomService].[NetUID] " +
                ",[CustomService].[SupplyOrderID] " +
                ",[CustomService].[SupplyPaymentTaskID] " +
                ",[CustomService].[Updated] " +
                ",[CustomService].[UserID] " +
                ",[CustomService].[CustomOrganizationID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[CustomService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ",[CustomService].[FromDate] " +
                ",[CustomService].[Number] " +
                ",[CustomService].[SupplyCustomType] " +
                ",[CustomService].[ExciseDutyOrganizationID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[CustomService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[CustomService].[Vat] " +
                ",[CustomService].[Name] " +
                ",[CustomService].[VatPercent] " +
                ",[CustomService].[ServiceNumber] " +
                ",[CustomService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[ExciseDutyOrganization].* " +
                ",[ExciseDutyOrganizationAgreement].* " +
                ",[ExciseDutyOrganizationOrganization].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[OrganizationTranslation].* " +
                "FROM [CustomService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [CustomService].CustomOrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
                "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ExciseDutyOrganizationAgreement] " +
                "ON [ExciseDutyOrganizationAgreement].[SupplyOrganizationID] = [ExciseDutyOrganization].[ID] " +
                "LEFT JOIN [Organization] AS [ExciseDutyOrganizationOrganization] " +
                "ON [ExciseDutyOrganizationOrganization].ID = [ExciseDutyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [CustomService].SupplyOrderID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].CustomServiceID = [CustomService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].CustomServiceID = [CustomService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [CustomService].UserID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "WHERE [CustomService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.CustomService)).Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.PortWorkService))) {
            Type[] joinTypes = {
                typeof(PortWorkService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], PortWorkService> joinMapper = objects => {
                PortWorkService portWorkService = (PortWorkService)objects[0];
                SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.PortWorkService) && i.Id.Equals(portWorkService.Id));

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.PortWorkService != null) {
                    if (invoiceDocument != null && !itemFromList.PortWorkService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PortWorkService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PortWorkService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PortWorkService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PortWorkService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PortWorkService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = portWorkService.ServiceNumber;

                    if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) portWorkService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    portWorkService.PortWorkOrganization = portWorkOrganization;
                    portWorkService.User = user;

                    itemFromList.PortWorkService = portWorkService;
                }

                return portWorkService;
            };

            _connection.Query(
                "SELECT [PortWorkService].[ID] " +
                ",[PortWorkService].[Created] " +
                ",[PortWorkService].[Deleted] " +
                ",[PortWorkService].[IsActive] " +
                ",[PortWorkService].[NetUID] " +
                ",[PortWorkService].[SupplyPaymentTaskID] " +
                ",[PortWorkService].[Updated] " +
                ",[PortWorkService].[UserID] " +
                ",[PortWorkService].[PortWorkOrganizationID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortWorkService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ",[PortWorkService].[FromDate] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortWorkService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[PortWorkService].[Vat] " +
                ",[PortWorkService].[Number] " +
                ",[PortWorkService].[Name] " +
                ",[PortWorkService].[VatPercent] " +
                ",[PortWorkService].[ServiceNumber] " +
                ",[PortWorkService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ", [OrganizationTranslation].* " +
                "FROM [PortWorkService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PortWorkService].UserID " +
                "WHERE [PortWorkService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.PortWorkService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.TransportationService))) {
            Type[] joinTypes = {
                typeof(TransportationService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], TransportationService> joinMapper = objects => {
                TransportationService transportationService = (TransportationService)objects[0];
                SupplyOrganization transportationOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.TransportationService) && i.Id.Equals(transportationService.Id));

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.TransportationService != null) {
                    if (invoiceDocument != null && !itemFromList.TransportationService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.TransportationService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.TransportationService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.TransportationService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.TransportationService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.TransportationService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = transportationService.ServiceNumber;

                    if (invoiceDocument != null) transportationService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        transportationService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) transportationService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    transportationService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    transportationService.TransportationOrganization = transportationOrganization;
                    transportationService.User = user;

                    itemFromList.TransportationService = transportationService;
                }

                return transportationService;
            };

            _connection.Query(
                "SELECT [TransportationService].[ID] " +
                ",[TransportationService].[Created] " +
                ",[TransportationService].[Deleted] " +
                ",[TransportationService].[IsActive] " +
                ",[TransportationService].[NetUID] " +
                ",[TransportationService].[SupplyPaymentTaskID] " +
                ",[TransportationService].[Updated] " +
                ",[TransportationService].[UserID] " +
                ",[TransportationService].[TransportationOrganizationID] " +
                ",[TransportationService].[FromDate] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[TransportationService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[TransportationService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ",[TransportationService].[Vat] " +
                ",[TransportationService].[Number] " +
                ",[TransportationService].[IsSealAndSignatureVerified] " +
                ",[TransportationService].[Name] " +
                ",[TransportationService].[VatPercent] " +
                ",[TransportationService].[ServiceNumber] " +
                ",[TransportationService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [TransportationService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [TransportationService].TransportationOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [TransportationService].UserID " +
                "WHERE [TransportationService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.TransportationService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.PortCustomAgencyService))) {
            Type[] joinTypes = {
                typeof(PortCustomAgencyService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], PortCustomAgencyService> joinMapper = objects => {
                PortCustomAgencyService portCustomAgencyService = (PortCustomAgencyService)objects[0];
                SupplyOrganization portCustomAgencyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.PortCustomAgencyService) && i.Id.Equals(portCustomAgencyService.Id));

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.PortCustomAgencyService != null) {
                    if (invoiceDocument != null && !itemFromList.PortCustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PortCustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PortCustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PortCustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PortCustomAgencyService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PortCustomAgencyService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = portCustomAgencyService.ServiceNumber;

                    if (invoiceDocument != null) portCustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        portCustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) portCustomAgencyService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    portCustomAgencyService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    portCustomAgencyService.PortCustomAgencyOrganization = portCustomAgencyOrganization;
                    portCustomAgencyService.User = user;

                    itemFromList.PortCustomAgencyService = portCustomAgencyService;
                }

                return portCustomAgencyService;
            };

            _connection.Query(
                "SELECT [PortCustomAgencyService].[ID] " +
                ",[PortCustomAgencyService].[Created] " +
                ",[PortCustomAgencyService].[Deleted] " +
                ",[PortCustomAgencyService].[FromDate] " +
                ",[PortCustomAgencyService].[IsActive] " +
                ",[PortCustomAgencyService].[NetUID] " +
                ",[PortCustomAgencyService].[PortCustomAgencyOrganizationID] " +
                ",[PortCustomAgencyService].[SupplyPaymentTaskID] " +
                ",[PortCustomAgencyService].[Updated] " +
                ",[PortCustomAgencyService].[UserID] " +
                ",[PortCustomAgencyService].[Vat] " +
                ",[PortCustomAgencyService].[Number] " +
                ",[PortCustomAgencyService].[Name] " +
                ",[PortCustomAgencyService].[VatPercent] " +
                ",[PortCustomAgencyService].[ServiceNumber] " +
                ",[PortCustomAgencyService].[SupplyOrganizationAgreementID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortCustomAgencyService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortCustomAgencyService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [PortCustomAgencyService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PortCustomAgencyService].UserID " +
                "WHERE [PortCustomAgencyService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.PortCustomAgencyService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.CustomAgencyService))) {
            Type[] joinTypes = {
                typeof(CustomAgencyService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], CustomAgencyService> joinMapper = objects => {
                CustomAgencyService customAgencyService = (CustomAgencyService)objects[0];
                SupplyOrganization customAgencyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.CustomAgencyService) && i.Id.Equals(customAgencyService.Id));

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.CustomAgencyService != null) {
                    if (invoiceDocument != null && !itemFromList.CustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.CustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.CustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.CustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.CustomAgencyService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.CustomAgencyService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = customAgencyService.ServiceNumber;

                    if (invoiceDocument != null) customAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        customAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) customAgencyService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    customAgencyService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    customAgencyService.CustomAgencyOrganization = customAgencyOrganization;
                    customAgencyService.User = user;

                    itemFromList.CustomAgencyService = customAgencyService;
                }

                return customAgencyService;
            };

            _connection.Query(
                "SELECT [CustomAgencyService].[ID] " +
                ",[CustomAgencyService].[Created] " +
                ",[CustomAgencyService].[CustomAgencyOrganizationID] " +
                ",[CustomAgencyService].[Deleted] " +
                ",[CustomAgencyService].[FromDate] " +
                ",[CustomAgencyService].[IsActive] " +
                ",[CustomAgencyService].[NetUID] " +
                ",[CustomAgencyService].[SupplyPaymentTaskID] " +
                ",[CustomAgencyService].[Updated] " +
                ",[CustomAgencyService].[UserID] " +
                ",[CustomAgencyService].[Vat] " +
                ",[CustomAgencyService].[Number] " +
                ",[CustomAgencyService].[Name] " +
                ",[CustomAgencyService].[VatPercent] " +
                ",[CustomAgencyService].[ServiceNumber] " +
                ",[CustomAgencyService].[SupplyOrganizationAgreementID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[CustomAgencyService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[CustomAgencyService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [CustomAgencyService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [CustomAgencyService].UserID " +
                "WHERE [CustomAgencyService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.CustomAgencyService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.PlaneDeliveryService))) {
            Type[] joinTypes = {
                typeof(PlaneDeliveryService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], PlaneDeliveryService> joinMapper = objects => {
                PlaneDeliveryService planeDeliveryService = (PlaneDeliveryService)objects[0];
                SupplyOrganization planeDeliveryOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.PlaneDeliveryService) && i.Id.Equals(planeDeliveryService.Id));

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.PlaneDeliveryService != null) {
                    if (invoiceDocument != null && !itemFromList.PlaneDeliveryService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PlaneDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PlaneDeliveryService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PlaneDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PlaneDeliveryService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PlaneDeliveryService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = planeDeliveryService.ServiceNumber;

                    if (invoiceDocument != null) planeDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        planeDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) planeDeliveryService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    planeDeliveryService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    planeDeliveryService.PlaneDeliveryOrganization = planeDeliveryOrganization;
                    planeDeliveryService.User = user;

                    itemFromList.PlaneDeliveryService = planeDeliveryService;
                }

                return planeDeliveryService;
            };

            _connection.Query(
                "SELECT [PlaneDeliveryService].[ID] " +
                ",[PlaneDeliveryService].[Created] " +
                ",[PlaneDeliveryService].[Deleted] " +
                ",[PlaneDeliveryService].[FromDate] " +
                ",[PlaneDeliveryService].[IsActive] " +
                ",[PlaneDeliveryService].[NetUID] " +
                ",[PlaneDeliveryService].[PlaneDeliveryOrganizationID] " +
                ",[PlaneDeliveryService].[SupplyPaymentTaskID] " +
                ",[PlaneDeliveryService].[Updated] " +
                ",[PlaneDeliveryService].[UserID] " +
                ",[PlaneDeliveryService].[Vat] " +
                ",[PlaneDeliveryService].[Number] " +
                ",[PlaneDeliveryService].[Name] " +
                ",[PlaneDeliveryService].[VatPercent] " +
                ",[PlaneDeliveryService].[ServiceNumber] " +
                ",[PlaneDeliveryService].[SupplyOrganizationAgreementID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PlaneDeliveryService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PlaneDeliveryService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [PlaneDeliveryService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PlaneDeliveryService].PlaneDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PlaneDeliveryService].UserID " +
                "WHERE [PlaneDeliveryService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.PlaneDeliveryService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.VehicleDeliveryService))) {
            Type[] joinTypes = {
                typeof(VehicleDeliveryService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], VehicleDeliveryService> joinMapper = objects => {
                VehicleDeliveryService vehicleDeliveryService = (VehicleDeliveryService)objects[0];
                SupplyOrganization vehicleDeliveryOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.VehicleDeliveryService) && i.Id.Equals(vehicleDeliveryService.Id));

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.VehicleDeliveryService != null) {
                    if (invoiceDocument != null && !itemFromList.VehicleDeliveryService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.VehicleDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.VehicleDeliveryService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.VehicleDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.VehicleDeliveryService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.VehicleDeliveryService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = vehicleDeliveryService.ServiceNumber;

                    if (invoiceDocument != null) vehicleDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        vehicleDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) vehicleDeliveryService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    vehicleDeliveryService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    vehicleDeliveryService.VehicleDeliveryOrganization = vehicleDeliveryOrganization;
                    vehicleDeliveryService.User = user;

                    itemFromList.VehicleDeliveryService = vehicleDeliveryService;
                }

                return vehicleDeliveryService;
            };

            _connection.Query(
                "SELECT [VehicleDeliveryService].[ID] " +
                ",[VehicleDeliveryService].[Created] " +
                ",[VehicleDeliveryService].[Deleted] " +
                ",[VehicleDeliveryService].[FromDate] " +
                ",[VehicleDeliveryService].[IsActive] " +
                ",[VehicleDeliveryService].[NetUID] " +
                ",[VehicleDeliveryService].[SupplyPaymentTaskID] " +
                ",[VehicleDeliveryService].[Updated] " +
                ",[VehicleDeliveryService].[UserID] " +
                ",[VehicleDeliveryService].[VehicleDeliveryOrganizationID] " +
                ",[VehicleDeliveryService].[Vat] " +
                ",[VehicleDeliveryService].[Number] " +
                ",[VehicleDeliveryService].[IsSealAndSignatureVerified] " +
                ",[VehicleDeliveryService].[Name] " +
                ",[VehicleDeliveryService].[VatPercent] " +
                ",[VehicleDeliveryService].[ServiceNumber] " +
                ",[VehicleDeliveryService].[SupplyOrganizationAgreementID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[VehicleDeliveryService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[VehicleDeliveryService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [VehicleDeliveryService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [VehicleDeliveryService].UserID " +
                "WHERE [VehicleDeliveryService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.VehicleDeliveryService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ConsumablesOrder))) {
            Type[] joinTypes = {
                typeof(ConsumablesOrder),
                typeof(User),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumableProduct),
                typeof(SupplyOrganization),
                typeof(ConsumablesStorage),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(MeasureUnit),
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(ConsumablesOrderDocument),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency)
            };

            Func<object[], ConsumablesOrder> joinMapper = objects => {
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[0];
                User user = (User)objects[1];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
                ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[6];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[7];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[8];
                MeasureUnit measureUnit = (MeasureUnit)objects[9];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[10];
                User supplyPaymentTaskUser = (User)objects[11];
                ConsumablesOrderDocument consumablesOrderDocument = (ConsumablesOrderDocument)objects[12];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
                Organization organization = (Organization)objects[14];
                Currency currency = (Currency)objects[15];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.ConsumablesOrder) && i.Id.Equals(consumablesOrder.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.ConsumablesOrder == null) {
                    itemFromList.Number = consumablesOrder.Number;

                    if (consumablesOrderItem != null) {
                        if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                        if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                        consumablesOrderItem.ConsumableProduct = consumableProduct;
                        consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                        consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                        consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT, 2);

                        consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                        consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPriceWithVAT, 2);

                        consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                    }

                    if (supplyPaymentTask != null) supplyPaymentTask.User = supplyPaymentTaskUser;

                    if (consumablesOrderDocument != null) consumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);

                    consumablesOrder.User = user;
                    consumablesOrder.SupplyPaymentTask = supplyPaymentTask;
                    consumablesOrder.ConsumablesStorage = consumablesStorage;
                    consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    itemFromList.ConsumablesOrder = consumablesOrder;

                    itemFromList.Comment = consumablesOrder.Comment;
                } else {
                    if (consumablesOrderItem != null && !itemFromList.ConsumablesOrder.ConsumablesOrderItems.Any(i => i.Id.Equals(consumablesOrderItem.Id))) {
                        if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                        if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                        consumablesOrderItem.ConsumableProduct = consumableProduct;
                        consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                        consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                        consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT, 2);

                        itemFromList.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                        itemFromList.ConsumablesOrder.TotalAmount = Math.Round(itemFromList.ConsumablesOrder.TotalAmount + consumablesOrderItem.TotalPriceWithVAT, 2);

                        itemFromList.ConsumablesOrder.TotalAmountWithoutVAT =
                            Math.Round(itemFromList.ConsumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                    }

                    if (consumablesOrderDocument != null
                        && !itemFromList.ConsumablesOrder.ConsumablesOrderDocuments.Any(d => d.Id.Equals(consumablesOrderDocument.Id)))
                        itemFromList.ConsumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);
                }

                return consumablesOrder;
            };

            _connection.Query(
                "SELECT [ConsumablesOrder].* " +
                ",[User].* " +
                ",[ConsumablesOrderItem].[ID] " +
                ",[ConsumablesOrderItem].[ConsumableProductCategoryID] " +
                ",[ConsumablesOrderItem].[ConsumableProductID] " +
                ",[ConsumablesOrderItem].[ConsumableProductOrganizationID] " +
                ",[ConsumablesOrderItem].[ConsumablesOrderID] " +
                ",[ConsumablesOrderItem].[Created] " +
                ",[ConsumablesOrderItem].[Deleted] " +
                ",[ConsumablesOrderItem].[NetUID] " +
                ",[dbo].[GetExchangedToEuroValue](" +
                "[ConsumablesOrderItem].[TotalPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [TotalPrice] " +
                ",[ConsumablesOrderItem].[Qty] " +
                ",[ConsumablesOrderItem].[Updated] " +
                ",[dbo].[GetExchangedToEuroValue](" +
                "[ConsumablesOrderItem].[PricePerItem], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [PricePerItem] " +
                ",[dbo].[GetExchangedToEuroValue](" +
                "[ConsumablesOrderItem].[VAT], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [VAT] " +
                ",[ConsumablesOrderItem].[VatPercent] " +
                ",[ConsumablesOrderItem].[IsService] " +
                ",[ConsumablesOrderItem].[SupplyOrganizationAgreementID] " +
                ",[ConsumableProductCategory].* " +
                ",[ConsumableProduct].* " +
                ",[ConsumableProductOrganization].* " +
                ",[ConsumablesStorage].* " +
                ",[PaymentCostMovementOperation].* " +
                ",[PaymentCostMovement].* " +
                ",[MeasureUnit].* " +
                ",[SupplyPaymentTask].* " +
                ",[SupplyPaymentTaskUser].* " +
                ",[ConsumablesOrderDocument].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[ConsumableProductOrganizationOrganization].* " +
                ",[Currency].* " +
                "FROM [ConsumablesOrder] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN (" +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture" +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN (" +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture" +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN (" +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
                "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [ConsumablesOrderDocument] " +
                "ON [ConsumablesOrderDocument].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [ConsumableProductOrganizationOrganization] " +
                "ON [ConsumableProductOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [ConsumableProductOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "WHERE [ConsumablesOrder].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ConsumablesOrder)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder))) {
            object parameters = new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder)).Select(s => s.Id)
            };

            string sqlExpression =
                "SELECT [OutcomePaymentOrder].[ID] " +
                ",[OutcomePaymentOrder].[Account] " +
                ",[OutcomePaymentOrder].[Amount] " +
                ",[OutcomePaymentOrder].[EuroAmount] " +
                ",[OutcomePaymentOrder].[Comment] " +
                ",[OutcomePaymentOrder].[Created] " +
                ",[OutcomePaymentOrder].[Deleted] " +
                ",[OutcomePaymentOrder].[FromDate] " +
                ",[OutcomePaymentOrder].[NetUID] " +
                ",[OutcomePaymentOrder].[Number] " +
                ",[OutcomePaymentOrder].[OrganizationID] " +
                ",[OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                ",[OutcomePaymentOrder].[Updated] " +
                ",[OutcomePaymentOrder].[UserID] " +
                ",[OutcomePaymentOrder].[IsUnderReport] " +
                ",[OutcomePaymentOrder].[ColleagueID] " +
                ",[OutcomePaymentOrder].[IsUnderReportDone] " +
                ",[OutcomePaymentOrder].[AdvanceNumber] " +
                ",[OutcomePaymentOrder].[ConsumableProductOrganizationID] " +
                ",[OutcomePaymentOrder].[IsAccounting] " +
                ",[OutcomePaymentOrder].[IsManagementAccounting] " +
                ",[OutcomePaymentOrder].[ExchangeRate] " +
                ",[OutcomePaymentOrder].[VAT]" +
                ",[OutcomePaymentOrder].[VatPercent] " +
                ",[dbo].[GetExchangedToEuroValue]( " +
                "[OutcomePaymentOrder].[AfterExchangeAmount], " +
                "[OutcomeAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [AfterExchangeAmount] " +
                ",[OutcomePaymentOrder].[ClientAgreementID] " +
                ",[OutcomePaymentOrder].[SupplyOrderPolandPaymentDeliveryProtocolID] " +
                ",[OutcomePaymentOrder].[SupplyOrganizationAgreementID] " +
                ", ( " +
                "SELECT ROUND( " +
                "( " +
                "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0)) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [ForDifferenceOutcome].ID " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [ForDifferenceOutcome].IsUnderReport = 1 " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([CompanyCarFueling].TotalPriceWithVat), 0)) " +
                "FROM [CompanyCarFueling] " +
                "WHERE [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "AND [CompanyCarFueling].Deleted = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT ISNULL(SUM([AssignedIncome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
                "ON [AssignedIncome].ID = [ForDifferenceAssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [AssignedIncome].IsCanceled = 0 " +
                ") " +
                "- " +
                "( " +
                "SELECT ISNULL(SUM([AssignedOutcome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
                "ON [AssignedOutcome].ID = [ForDifferenceAssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                ", 2) " +
                ") AS [DifferenceAmount] " +
                ", [Organization].* " +
                ", [User].* " +
                ", [PaymentMovementOperation].* " +
                ", [PaymentMovement].* " +
                ", [PaymentCurrencyRegister].* " +
                ", [Currency].* " +
                ", [PaymentRegister].* " +
                ", [PaymentRegisterOrganization].* " +
                ", [Colleague].* " +
                ", [OutcomeConsumableProductOrganization].* " +
                ", [OutcomePaymentOrderSupplyPaymentTask].* " +
                ", [SupplyPaymentTask].* " +
                ", [OutcomeAgreement].* " +
                ", [OutcomeAgreementCurrency].* " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [OutcomePaymentOrder].UserID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
                "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
                "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Colleague] " +
                "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
                "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
                "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                "ON [OutcomePaymentOrderSupplyPaymentTask].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
                "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
                "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
                "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrder].ID IN @Ids ";

            Type[] types = {
                typeof(OutcomePaymentOrder),
                typeof(Organization),
                typeof(User),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(OutcomePaymentOrderSupplyPaymentTask),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrder> mapper = objects => {
                OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                User user = (User)objects[2];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
                PaymentMovement paymentMovement = (PaymentMovement)objects[4];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
                Currency currency = (Currency)objects[6];
                PaymentRegister paymentRegister = (PaymentRegister)objects[7];
                Organization paymentRegisterOrganization = (Organization)objects[8];
                User colleague = (User)objects[9];
                SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[10];
                OutcomePaymentOrderSupplyPaymentTask junctionTask = (OutcomePaymentOrderSupplyPaymentTask)objects[11];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[12];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
                Currency supplyOrganizationAgreementCurrency = (Currency)objects[14];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrder.Id));

                itemFromList.IsAccounting = outcomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = outcomePaymentOrder.IsManagementAccounting;
                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.OutcomePaymentOrder == null) {
                    if (outcomePaymentOrder != null && !string.IsNullOrEmpty(outcomePaymentOrder.Number)) {
                        itemFromList.Number = outcomePaymentOrder.Number;

                        if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                        if (junctionTask != null) {
                            junctionTask.SupplyPaymentTask = supplyPaymentTask;

                            outcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                        }

                        if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                        paymentRegister.Organization = paymentRegisterOrganization;

                        paymentCurrencyRegister.PaymentRegister = paymentRegister;
                        paymentCurrencyRegister.Currency = currency;

                        outcomePaymentOrder.Organization = organization;
                        outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                        outcomePaymentOrder.User = user;
                        outcomePaymentOrder.Colleague = colleague;
                        outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                        outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                        outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    }

                    itemFromList.OutcomePaymentOrder = outcomePaymentOrder;
                } else {
                    if (!string.IsNullOrEmpty(itemFromList.OutcomePaymentOrder.Number))
                        itemFromList.Number = itemFromList.OutcomePaymentOrder.Number;

                    if (junctionTask == null
                        || itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.Id.Equals(junctionTask.Id)))
                        return outcomePaymentOrder;

                    junctionTask.SupplyPaymentTask = supplyPaymentTask;

                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                }

                return outcomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                parameters
            );

            string orderSqlQuery =
                "SELECT " +
                "* " +
                "FROM [OutcomePaymentOrderConsumablesOrder] " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
                "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
                "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
                "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
                "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrderConsumablesOrder].[OutcomePaymentOrderID] IN @Ids ";

            Type[] orderTypes = {
                typeof(OutcomePaymentOrderConsumablesOrder),
                typeof(ConsumablesOrder),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumableProduct),
                typeof(SupplyOrganization),
                typeof(User),
                typeof(ConsumablesStorage),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(MeasureUnit),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrderConsumablesOrder> orderMapper = objects => {
                OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[0];
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[1];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
                User consumablesOrderUser = (User)objects[6];
                ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[7];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[8];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[9];
                MeasureUnit measureUnit = (MeasureUnit)objects[10];
                SupplyOrganizationAgreement consumablesSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[11];
                Currency consumablesSupplyOrganizationAgreementCurrency = (Currency)objects[12];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrderConsumablesOrder.OutcomePaymentOrderId));

                if (!itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id)))
                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                else
                    outcomePaymentOrderConsumablesOrder =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                if (outcomePaymentOrderConsumablesOrder.ConsumablesOrder == null)
                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                if (paymentCostMovementOperation != null)
                    paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                if (consumableProduct != null)
                    consumableProduct.MeasureUnit = measureUnit;

                if (consumablesSupplyOrganizationAgreement != null)
                    consumablesSupplyOrganizationAgreement.Currency = consumablesSupplyOrganizationAgreementCurrency;

                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                consumablesOrderItem.ConsumableProduct = consumableProduct;
                consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                consumablesOrderItem.SupplyOrganizationAgreement = consumablesSupplyOrganizationAgreement;

                consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT, 2);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.User = consumablesOrderUser;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesStorage = consumablesStorage;

                return outcomePaymentOrderConsumablesOrder;
            };

            _connection.Query(
                orderSqlQuery,
                orderTypes,
                orderMapper,
                parameters);
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder))) {
            string sqlExpression =
                "SELECT * " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
                "AND [PaymentCurrencyRegister].CurrencyID = [Currency].ID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentMovementOperation].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "AND [PaymentMovementOperation].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [IncomePaymentOrder].UserID " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [IncomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "WHERE [IncomePaymentOrder].ID IN @Ids";

            Type[] types = {
                typeof(IncomePaymentOrder),
                typeof(Organization),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(PaymentCurrencyRegister),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], IncomePaymentOrder> mapper = objects => {
                IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                Currency currency = (Currency)objects[2];
                PaymentRegister paymentRegister = (PaymentRegister)objects[3];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[4];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[5];
                PaymentMovement paymentMovement = (PaymentMovement)objects[6];
                User user = (User)objects[7];
                SupplyOrganization incomeClient = (SupplyOrganization)objects[8];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[9];
                Currency agreementCurrency = (Currency)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.IncomePaymentOrder) && i.Id.Equals(incomePaymentOrder.Id));

                itemFromList.IsAccounting = incomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = incomePaymentOrder.IsManagementAccounting;
                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.IncomePaymentOrder != null) {
                    itemFromList.Number = itemFromList.IncomePaymentOrder.Number;
                } else {
                    if (!string.IsNullOrEmpty(incomePaymentOrder.Number))
                        itemFromList.Number = incomePaymentOrder.Number;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = agreementCurrency;

                    if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);

                    incomePaymentOrder.Organization = organization;
                    incomePaymentOrder.User = user;
                    incomePaymentOrder.Currency = currency;
                    incomePaymentOrder.PaymentRegister = paymentRegister;
                    incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                    incomePaymentOrder.SupplyOrganization = incomeClient;
                    incomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    itemFromList.IncomePaymentOrder = incomePaymentOrder;
                }

                return incomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                },
                commandTimeout: 3600
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyPaymentTask))) {
            Type[] joinTypes = {
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyPaymentTaskDocument),
                typeof(ContainerService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderContainerService),
                typeof(SupplyOrder),
                typeof(User),
                typeof(PortWorkService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], SupplyPaymentTask> joinMapper = objects => {
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[0];
                User user = (User)objects[1];
                SupplyPaymentTaskDocument supplyPaymentTaskDocument = (SupplyPaymentTaskDocument)objects[2];
                ContainerService containerService = (ContainerService)objects[3];
                SupplyOrganization containerOrganization = (SupplyOrganization)objects[4];
                SupplyOrganizationAgreement containerSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[5];
                Organization containerOrganizationOrganization = (Organization)objects[6];
                Currency containerOrganizationCurrency = (Currency)objects[7];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[8];
                InvoiceDocument containerInvoiceDocument = (InvoiceDocument)objects[9];
                SupplyOrderContainerService junction = (SupplyOrderContainerService)objects[10];
                SupplyOrder containerSupplyOrder = (SupplyOrder)objects[11];
                User containerUser = (User)objects[12];
                PortWorkService portWorkService = (PortWorkService)objects[13];
                SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[14];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[15];
                Organization organization = (Organization)objects[16];
                Currency currency = (Currency)objects[17];
                SupplyOrder portWorkSupplyOrder = (SupplyOrder)objects[18];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[19];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[20];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[21];
                User portWorkUser = (User)objects[22];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.SupplyPaymentTask) && i.Id.Equals(supplyPaymentTask.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.SupplyPaymentTask != null) {
                    if (containerSupplyOrder != null) {
                        itemFromList.Number = containerSupplyOrder.SupplyOrderNumberId.ToString();

                        itemFromList.Comment = containerSupplyOrder.Comment;
                    } else if (portWorkSupplyOrder != null) {
                        itemFromList.Number = portWorkSupplyOrder.SupplyOrderNumberId.ToString();
                    }

                    if (containerService != null) {
                        if (!itemFromList.SupplyPaymentTask.ContainerServices.Any(s => s.Id.Equals(containerService.Id))) {
                            if (junction != null) {
                                junction.SupplyOrder = containerSupplyOrder;

                                itemFromList.Comment = containerSupplyOrder.Comment;

                                containerService.SupplyOrderContainerServices.Add(junction);
                            }

                            if (containerInvoiceDocument != null) containerService.InvoiceDocuments.Add(containerInvoiceDocument);

                            if (containerSupplyOrganizationAgreement != null) {
                                containerSupplyOrganizationAgreement.Currency = containerOrganizationCurrency;

                                containerSupplyOrganizationAgreement.Organization = containerOrganizationOrganization;
                            }

                            containerService.SupplyOrganizationAgreement = containerSupplyOrganizationAgreement;
                            containerService.BillOfLadingDocument = billOfLadingDocument;
                            containerService.User = containerUser;
                            containerService.ContainerOrganization = containerOrganization;

//                                if (containerService.FromDate.HasValue) {
//                                    containerService.FromDate = TimeZoneInfo.ConvertTimeFromUtc(containerService.FromDate.Value, currentTimeZone);
//                                }
//
//                                containerService.LoadDate = TimeZoneInfo.ConvertTimeFromUtc(containerService.LoadDate, currentTimeZone);
//                                containerService.Created = TimeZoneInfo.ConvertTimeFromUtc(containerService.Created, currentTimeZone);
//                                containerService.Updated = TimeZoneInfo.ConvertTimeFromUtc(containerService.Updated, currentTimeZone);

                            itemFromList.SupplyPaymentTask.ContainerServices.Add(containerService);
                        } else {
                            ContainerService fromList = itemFromList.SupplyPaymentTask.ContainerServices.First(s => s.Id.Equals(containerService.Id));

                            if (junction != null && !fromList.SupplyOrderContainerServices.Any(j => j.Id.Equals(junction.Id))) {
                                junction.SupplyOrder = containerSupplyOrder;

                                fromList.SupplyOrderContainerServices.Add(junction);
                            }

                            if (containerInvoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(containerInvoiceDocument.Id)))
                                fromList.InvoiceDocuments.Add(containerInvoiceDocument);
                        }
                    }

                    if (portWorkService != null) {
                        if (!itemFromList.SupplyPaymentTask.PortWorkServices.Any(s => s.Id.Equals(portWorkService.Id))) {
                            if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                            if (serviceDetailItem != null) {
                                serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                                portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                            }

                            if (portWorkSupplyOrder != null) portWorkService.SupplyOrders.Add(portWorkSupplyOrder);

                            if (supplyOrganizationAgreement != null) {
                                supplyOrganizationAgreement.Currency = currency;

                                supplyOrganizationAgreement.Organization = organization;
                            }

                            portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                            portWorkService.PortWorkOrganization = portWorkOrganization;
                            portWorkService.User = portWorkUser;

//                                if (portWorkService.FromDate.HasValue) {
//                                    portWorkService.FromDate = TimeZoneInfo.ConvertTimeFromUtc(portWorkService.FromDate.Value, currentTimeZone);
//                                }
//
//                                portWorkService.Created = TimeZoneInfo.ConvertTimeFromUtc(portWorkService.Created, currentTimeZone);
//                                portWorkService.Updated = TimeZoneInfo.ConvertTimeFromUtc(portWorkService.Updated, currentTimeZone);

                            supplyPaymentTask.PortWorkServices.Add(portWorkService);
                        } else {
                            PortWorkService fromList = itemFromList.SupplyPaymentTask.PortWorkServices.First(s => s.Id.Equals(portWorkService.Id));

                            if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);

                            if (serviceDetailItem != null && !fromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                                serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                                fromList.ServiceDetailItems.Add(serviceDetailItem);
                            }

                            if (portWorkSupplyOrder != null && !fromList.SupplyOrders.Any(o => o.Id.Equals(portWorkSupplyOrder.Id))) fromList.SupplyOrders.Add(portWorkSupplyOrder);
                        }
                    }
                } else {
                    if (containerSupplyOrder != null) {
                        itemFromList.Number = containerSupplyOrder.SupplyOrderNumberId.ToString();

                        itemFromList.Comment = containerSupplyOrder.Comment;
                    } else if (portWorkSupplyOrder != null) {
                        itemFromList.Number = portWorkSupplyOrder.SupplyOrderNumberId.ToString();
                    }

                    if (containerService != null) {
                        if (junction != null) {
                            junction.SupplyOrder = containerSupplyOrder;

                            containerService.SupplyOrderContainerServices.Add(junction);
                        }

                        if (containerInvoiceDocument != null) containerService.InvoiceDocuments.Add(containerInvoiceDocument);

                        if (containerSupplyOrganizationAgreement != null) {
                            containerSupplyOrganizationAgreement.Currency = containerOrganizationCurrency;

                            containerSupplyOrganizationAgreement.Organization = containerOrganizationOrganization;
                        }

                        containerService.SupplyOrganizationAgreement = containerSupplyOrganizationAgreement;
                        containerService.BillOfLadingDocument = billOfLadingDocument;
                        containerService.User = containerUser;
                        containerService.ContainerOrganization = containerOrganization;

//                            if (containerService.FromDate.HasValue) {
//                                containerService.FromDate = TimeZoneInfo.ConvertTimeFromUtc(containerService.FromDate.Value, currentTimeZone);
//                            }
//
//                            containerService.LoadDate = TimeZoneInfo.ConvertTimeFromUtc(containerService.LoadDate, currentTimeZone);
//                            containerService.Created = TimeZoneInfo.ConvertTimeFromUtc(containerService.Created, currentTimeZone);
//                            containerService.Updated = TimeZoneInfo.ConvertTimeFromUtc(containerService.Updated, currentTimeZone);

                        supplyPaymentTask.ContainerServices.Add(containerService);
                    }

                    if (portWorkService != null) {
                        if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (portWorkSupplyOrder != null) portWorkService.SupplyOrders.Add(portWorkSupplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        portWorkService.PortWorkOrganization = portWorkOrganization;
                        portWorkService.User = portWorkUser;

//                            if (portWorkService.FromDate.HasValue) {
//                                portWorkService.FromDate = TimeZoneInfo.ConvertTimeFromUtc(portWorkService.FromDate.Value, currentTimeZone);
//                            }
//
//                            portWorkService.Created = TimeZoneInfo.ConvertTimeFromUtc(portWorkService.Created, currentTimeZone);
//                            portWorkService.Updated = TimeZoneInfo.ConvertTimeFromUtc(portWorkService.Updated, currentTimeZone);

                        supplyPaymentTask.PortWorkServices.Add(portWorkService);
                    }

                    if (supplyPaymentTaskDocument != null) supplyPaymentTask.SupplyPaymentTaskDocuments.Add(supplyPaymentTaskDocument);

                    supplyPaymentTask.User = user;

//                        if (supplyPaymentTask.PayToDate.HasValue) {
//                            supplyPaymentTask.PayToDate = TimeZoneInfo.ConvertTimeFromUtc(supplyPaymentTask.PayToDate.Value, currentTimeZone);
//                        }
//
//                        if (supplyPaymentTask.TaskStatusUpdated.HasValue) {
//                            supplyPaymentTask.TaskStatusUpdated = TimeZoneInfo.ConvertTimeFromUtc(supplyPaymentTask.TaskStatusUpdated.Value, currentTimeZone);
//                        }
//
//                        supplyPaymentTask.Created = TimeZoneInfo.ConvertTimeFromUtc(supplyPaymentTask.Created, currentTimeZone);
//                        supplyPaymentTask.Updated = TimeZoneInfo.ConvertTimeFromUtc(supplyPaymentTask.Updated, currentTimeZone);

                    itemFromList.SupplyPaymentTask = supplyPaymentTask;
                }

                return supplyPaymentTask;
            };

            _connection.Query(
                "SELECT [SupplyPaymentTask].ID " +
                ",[SupplyPaymentTask].[Created] " +
                ",[SupplyPaymentTask].[Deleted] " +
                ",[SupplyPaymentTask].[NetUID] " +
                ",[SupplyPaymentTask].[Updated] " +
                ",[SupplyPaymentTask].[Comment] " +
                ",[SupplyPaymentTask].[UserID] " +
                ",[SupplyPaymentTask].[PayToDate] " +
                ",[SupplyPaymentTask].[TaskAssignedTo] " +
                ",[SupplyPaymentTask].[TaskStatus] " +
                ",[SupplyPaymentTask].[TaskStatusUpdated] " +
                ",(CASE WHEN [ContainerSupplyOrganizationAgreement].CurrencyID IS NOT NULL " +
                "THEN [dbo].[GetExchangedToEuroValue](" +
                "[SupplyPaymentTask].[GrossPrice], " +
                "[ContainerSupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") " +
                "ELSE [dbo].[GetExchangedToEuroValue](" +
                "[SupplyPaymentTask].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") " +
                "END) AS [GrossPrice] " +
                ",(CASE WHEN [ContainerSupplyOrganizationAgreement].CurrencyID IS NOT NULL " +
                "THEN [dbo].[GetExchangedToEuroValue](" +
                "[SupplyPaymentTask].[NetPrice], " +
                "[ContainerSupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") " +
                "ELSE [dbo].[GetExchangedToEuroValue](" +
                "[SupplyPaymentTask].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") " +
                "END) AS [NetPrice] " +
                ",[SupplyPaymentTask].[IsAvailableForPayment] " +
                ",[User].* " +
                ",[SupplyPaymentTaskDocument].* " +
                ",[ContainerService].[ID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[ContainerService].[NetPrice], " +
                "[ContainerSupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[ContainerService].[BillOfLadingDocumentID] " +
                ",[ContainerService].[Created] " +
                ",[ContainerService].[Deleted] " +
                ",[ContainerService].[IsActive] " +
                ",[ContainerService].[LoadDate] " +
                ",[ContainerService].[NetUID] " +
                ",[ContainerService].[TermDeliveryInDays] " +
                ",[ContainerService].[Updated] " +
                ",[ContainerService].[SupplyPaymentTaskID] " +
                ",[ContainerService].[UserID] " +
                ",[ContainerService].[ContainerOrganizationID] " +
                ",[ContainerService].[FromDate] " +
                ",[ContainerService].[GroosWeight] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[ContainerService].[GrossPrice], " +
                "[ContainerSupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ",[ContainerService].[Vat] " +
                ",[ContainerService].[Number] " +
                ",[ContainerService].[Name] " +
                ",[ContainerService].[VatPercent] " +
                ",[ContainerService].[IsCalculatedExtraCharge] " +
                ",[ContainerService].[SupplyExtraChargeType] " +
                ",[ContainerService].[ContainerNumber] " +
                ",[ContainerService].[ServiceNumber] " +
                ",[ContainerService].[SupplyOrganizationAgreementID] " +
                ",[ContainerOrganization].* " +
                ",[ContainerSupplyOrganizationAgreement].* " +
                ",[ContainerOrganizationOrganization].* " +
                ",[ContainerOrganizationCurrency].* " +
                ",[BillOfLadingDocument].* " +
                ",[ContainerInvoiceDocument].* " +
                ",[SupplyOrderContainerService].* " +
                ",[SupplyOrder].* " +
                ",[ContainerUser].* " +
                ",[PortWorkService].[ID] " +
                ",[PortWorkService].[Created] " +
                ",[PortWorkService].[Deleted] " +
                ",[PortWorkService].[IsActive] " +
                ",[PortWorkService].[NetUID] " +
                ",[PortWorkService].[SupplyPaymentTaskID] " +
                ",[PortWorkService].[Updated] " +
                ",[PortWorkService].[UserID] " +
                ",[PortWorkService].[PortWorkOrganizationID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortWorkService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ",[PortWorkService].[FromDate] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortWorkService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[PortWorkService].[Vat] " +
                ",[PortWorkService].[Number] " +
                ",[PortWorkService].[Name] " +
                ",[PortWorkService].[VatPercent] " +
                ",[PortWorkService].[ServiceNumber] " +
                ",[PortWorkService].[SupplyOrganizationAgreementID] " +
                ",[PortWorkOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[PortWorkOrganizationOrganization].* " +
                ",[PortWorkCurrency].* " +
                ",[PortWorkSupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[PortWorkInvoiceDocument].* " +
                ",[PortWorkUser].* " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [SupplyPaymentTaskDocument] " +
                "ON [SupplyPaymentTaskDocument].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [ContainerService] " +
                "ON [ContainerService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
                "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ContainerSupplyOrganizationAgreement] " +
                "ON [ContainerSupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                "LEFT JOIN (SELECT " +
                "[Organization].[ID] " +
                ",[Organization].[Created] " +
                ",[Organization].[Deleted] " +
                ",(CASE WHEN [OrganizationTranslation].[Name] IS NOT NULL THEN [OrganizationTranslation].[Name] ELSE [Organization].[Name] END) AS [Name]  " +
                ",[Organization].[NetUID] " +
                ",[Organization].[Updated] " +
                ",[Organization].[Code] " +
                ",[Organization].[Culture] " +
                ",[Organization].[Address] " +
                ",[Organization].[FullName] " +
                ",[Organization].[IsIndividual] " +
                ",[Organization].[PFURegistrationNumber] " +
                ",[Organization].[PhoneNumber] " +
                ",[Organization].[CurrencyID] " +
                ",[Organization].[StorageID] " +
                "FROM [Organization] " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                ") AS [ContainerOrganizationOrganization] " +
                "ON [ContainerOrganizationOrganization].[ID] = [ContainerSupplyOrganizationAgreement].[OrganizationID] " +
                "LEFT JOIN [views].[CurrencyView] AS [ContainerOrganizationCurrency] " +
                "ON [ContainerOrganizationCurrency].ID = [ContainerSupplyOrganizationAgreement].CurrencyID " +
                "AND [ContainerOrganizationCurrency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] AS [ContainerInvoiceDocument] " +
                "ON [ContainerInvoiceDocument].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrderContainerService] " +
                "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                "LEFT JOIN [User] AS [ContainerUser] " +
                "ON [ContainerUser].ID = [ContainerService].UserID " +
                "LEFT JOIN [PortWorkService] " +
                "ON [PortWorkService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [PortWorkOrganization] " +
                "ON [PortWorkOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] AS [PortWorkOrganizationOrganization] " +
                "ON [PortWorkOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [PortWorkCurrency] " +
                "ON [PortWorkCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [PortWorkCurrency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] AS [PortWorkSupplyOrder] " +
                "ON [PortWorkSupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] AS [PortWorkInvoiceDocument] " +
                "ON [PortWorkInvoiceDocument].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [User] AS [PortWorkUser] " +
                "ON [PortWorkUser].ID = [PortWorkService].UserID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyPaymentTask)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.MergedService))) {
            Type[] joinTypes = {
                typeof(MergedService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(SupplyOrderUkraine),
                typeof(OrganizationTranslation),
                typeof(ConsumableProduct),
                typeof(ActProvidingService)
            };

            Func<object[], MergedService> joinMapper = objects => {
                MergedService mergedService = (MergedService)objects[0];
                SupplyOrganization mergedSupplyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[10];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[11];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[12];
                ActProvidingService actProvidingService = (ActProvidingService)objects[13];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.MergedService) && i.Id.Equals(mergedService.Id));

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.MergedService != null) {
                    itemFromList.Number = itemFromList.MergedService.ActProvidingService.Number;

                    itemFromList.MergedService.ConsumableProduct = consumableProduct;
                    itemFromList.MergedService.ActProvidingService = actProvidingService;

                    if (invoiceDocument != null && !itemFromList.MergedService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.MergedService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem == null || itemFromList.MergedService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id)))
                        return mergedService;

                    serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                    itemFromList.MergedService.ServiceDetailItems.Add(serviceDetailItem);
                } else {
                    if (mergedService != null) {
                        if (actProvidingService != null) itemFromList.Number = actProvidingService.Number;

                        mergedService.ConsumableProduct = consumableProduct;
                        mergedService.ActProvidingService = actProvidingService;

                        if (invoiceDocument != null) mergedService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            mergedService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        mergedService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        mergedService.SupplyOrganization = mergedSupplyOrganization;
                        mergedService.SupplyOrderUkraine = supplyOrderUkraine;
                        mergedService.SupplyOrder = supplyOrder;
                        mergedService.User = user;
                    }

                    itemFromList.MergedService = mergedService;
                }

                return mergedService;
            };

            _connection.Query(
                "SELECT [MergedService].[ID] " +
                ",[MergedService].[Created] " +
                ",[MergedService].[Deleted] " +
                ",[MergedService].[FromDate] " +
                ",[MergedService].[IsActive] " +
                ",[MergedService].[NetUID] " +
                ",[MergedService].[SupplyPaymentTaskID] " +
                ",[MergedService].[Updated] " +
                ",[MergedService].[UserID] " +
                ",[MergedService].[SupplyOrganizationID] " +
                ",[MergedService].[Vat] " +
                ",[MergedService].[Number] " +
                ",[MergedService].[DeliveryProductProtocolID] " +
                ",[MergedService].[Name] " +
                ",[MergedService].[VatPercent] " +
                ",[MergedService].[ServiceNumber] " +
                ",[MergedService].[SupplyOrganizationAgreementID] " +
                ",[MergedService].[ConsumableProductID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[MergedService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[MergedService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[SupplyOrderUkraine].* " +
                ",[OrganizationTranslation].* " +
                ",[ConsumableProduct].* " +
                ",[ActProvidingService].* " +
                "FROM [MergedService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [MergedService].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [MergedService].SupplyOrderID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].MergedServiceID = [MergedService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].MergedServiceID = [MergedService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [MergedService].UserID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [MergedService].SupplyOrderUkraineID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [ConsumableProduct] " +
                "ON [ConsumableProduct].[ID] = [MergedService].[ConsumableProductID] " +
                "LEFT JOIN [ActProvidingService] " +
                "ON [ActProvidingService].[ID] = [MergedService].[ActProvidingServiceID] " +
                "WHERE [MergedService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.MergedService)).Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.DeliveryExpense))) {
            Type[] joinTypes = {
                typeof(DeliveryExpense),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(User),
                typeof(SupplyOrderUkraine),
                typeof(OrganizationTranslation),
                typeof(ConsumableProduct),
                typeof(ActProvidingService)
            };

            Func<object[], DeliveryExpense> joinMapper = objects => {
                DeliveryExpense deliveryExpense = (DeliveryExpense)objects[0];
                SupplyOrganization deliverySupplyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                User user = (User)objects[5];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[6];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[7];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[8];
                ActProvidingService actProvidingService = (ActProvidingService)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.DeliveryExpense) && i.Id.Equals(deliveryExpense.Id));

                itemFromList.IsManagementAccounting = true;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.DeliveryExpense != null) {
                    itemFromList.Number = itemFromList.DeliveryExpense.ActProvidingService.Number;

                    itemFromList.DeliveryExpense.ConsumableProduct = consumableProduct;
                    itemFromList.DeliveryExpense.ActProvidingService = actProvidingService;
                } else {
                    if (deliveryExpense != null) {
                        if (actProvidingService != null) {
                            itemFromList.IsAccounting = actProvidingService.IsAccounting;
                            itemFromList.Number = actProvidingService.Number;
                        }

                        deliveryExpense.ConsumableProduct = consumableProduct;
                        deliveryExpense.ActProvidingService = actProvidingService;

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        deliveryExpense.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        deliveryExpense.SupplyOrganization = deliverySupplyOrganization;
                        deliveryExpense.SupplyOrderUkraine = supplyOrderUkraine;
                        deliveryExpense.User = user;
                    }

                    itemFromList.DeliveryExpense = deliveryExpense;
                }

                return deliveryExpense;
            };

            _connection.Query(
                "SELECT [DeliveryExpense].ID " +
                ",[DeliveryExpense].Created " +
                ",[DeliveryExpense].Deleted " +
                ",[DeliveryExpense].FromDate " +
                ",[DeliveryExpense].NetUID " +
                ",[DeliveryExpense].Updated " +
                ",[DeliveryExpense].UserID " +
                ",[DeliveryExpense].SupplyOrganizationID " +
                ",[DeliveryExpense].VatPercent " +
                ",[DeliveryExpense].AccountingVatPercent " +
                ",[DeliveryExpense].InvoiceNumber " +
                ",[DeliveryExpense].SupplyOrganizationAgreementID " +
                ",[DeliveryExpense].ConsumableProductID " +
                ", [dbo].[GetExchangedToEuroValue]( " +
                "[DeliveryExpense].[GrossAmount], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossAmount] " +
                ", [dbo].[GetExchangedToEuroValue]( " +
                "[DeliveryExpense].AccountingGrossAmount, " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [AccountingGrossAmount] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[User].* " +
                ",[SupplyOrderUkraine].* " +
                ",[OrganizationTranslation].* " +
                ",[ConsumableProduct].* " +
                ",[ActProvidingService].* " +
                "FROM [DeliveryExpense] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [DeliveryExpense].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [DeliveryExpense].UserID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [DeliveryExpense].SupplyOrderUkraineID " +
                "LEFT JOIN [ConsumableProduct] " +
                "ON [ConsumableProduct].[ID] = [DeliveryExpense].[ConsumableProductID] " +
                "LEFT JOIN [ActProvidingService] " +
                "ON [ActProvidingService].[ID] = [DeliveryExpense].[ActProvidingServiceID] " +
                "WHERE [DeliveryExpense].ID IN @Ids ",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.DeliveryExpense)).Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                });
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingDeliveryExpense))) {
            Type[] joinTypes = {
                typeof(DeliveryExpense),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(User),
                typeof(SupplyOrderUkraine),
                typeof(OrganizationTranslation),
                typeof(ConsumableProduct),
                typeof(ActProvidingService)
            };

            Func<object[], DeliveryExpense> joinMapper = objects => {
                DeliveryExpense deliveryExpense = (DeliveryExpense)objects[0];
                SupplyOrganization deliverySupplyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                User user = (User)objects[5];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[6];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[7];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[8];
                ActProvidingService actProvidingService = (ActProvidingService)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.AccountingDeliveryExpense) && i.Id.Equals(deliveryExpense.Id));

                itemFromList.IsAccounting = true;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.DeliveryExpense != null) {
                    itemFromList.Number = itemFromList.DeliveryExpense.ActProvidingService.Number;

                    itemFromList.DeliveryExpense.ConsumableProduct = consumableProduct;
                    itemFromList.DeliveryExpense.ActProvidingService = actProvidingService;
                } else {
                    if (deliveryExpense != null) {
                        if (actProvidingService != null) {
                            itemFromList.IsAccounting = actProvidingService.IsAccounting;
                            itemFromList.Number = actProvidingService.Number;
                        }

                        deliveryExpense.ConsumableProduct = consumableProduct;
                        deliveryExpense.ActProvidingService = actProvidingService;

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        deliveryExpense.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        deliveryExpense.SupplyOrganization = deliverySupplyOrganization;
                        deliveryExpense.SupplyOrderUkraine = supplyOrderUkraine;
                        deliveryExpense.User = user;
                    }

                    itemFromList.DeliveryExpense = deliveryExpense;
                }

                return deliveryExpense;
            };

            _connection.Query(
                "SELECT [DeliveryExpense].ID " +
                ",[DeliveryExpense].Created " +
                ",[DeliveryExpense].Deleted " +
                ",[DeliveryExpense].FromDate " +
                ",[DeliveryExpense].NetUID " +
                ",[DeliveryExpense].Updated " +
                ",[DeliveryExpense].UserID " +
                ",[DeliveryExpense].SupplyOrganizationID " +
                ",[DeliveryExpense].VatPercent " +
                ",[DeliveryExpense].AccountingVatPercent " +
                ",[DeliveryExpense].InvoiceNumber " +
                ",[DeliveryExpense].SupplyOrganizationAgreementID " +
                ",[DeliveryExpense].ConsumableProductID " +
                ", [dbo].[GetExchangedToEuroValue]( " +
                "[DeliveryExpense].[GrossAmount], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossAmount] " +
                ", [dbo].[GetExchangedToEuroValue]( " +
                "[DeliveryExpense].AccountingGrossAmount, " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [AccountingGrossAmount] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[User].* " +
                ",[SupplyOrderUkraine].* " +
                ",[OrganizationTranslation].* " +
                ",[ConsumableProduct].* " +
                ",[ActProvidingService].* " +
                "FROM [DeliveryExpense] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [DeliveryExpense].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [DeliveryExpense].UserID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [DeliveryExpense].SupplyOrderUkraineID " +
                "LEFT JOIN [ConsumableProduct] " +
                "ON [ConsumableProduct].[ID] = [DeliveryExpense].[ConsumableProductID] " +
                "LEFT JOIN [ActProvidingService] " +
                "ON [ActProvidingService].[ID] = [DeliveryExpense].[AccountingActProvidingServiceID] " +
                "WHERE [DeliveryExpense].ID IN @Ids ",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingDeliveryExpense)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                });
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingContainerPaymentTask))) {
            Type[] joinTypes = {
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyPaymentTaskDocument),
                typeof(ContainerService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderContainerService),
                typeof(SupplyOrder),
                typeof(User),
                typeof(PortWorkService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], SupplyPaymentTask> joinMapper = objects => {
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[0];
                User user = (User)objects[1];
                SupplyPaymentTaskDocument supplyPaymentTaskDocument = (SupplyPaymentTaskDocument)objects[2];
                ContainerService containerService = (ContainerService)objects[3];
                SupplyOrganization containerOrganization = (SupplyOrganization)objects[4];
                SupplyOrganizationAgreement containerSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[5];
                Organization containerOrganizationOrganization = (Organization)objects[6];
                Currency containerOrganizationCurrency = (Currency)objects[7];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[8];
                InvoiceDocument containerInvoiceDocument = (InvoiceDocument)objects[9];
                SupplyOrderContainerService junction = (SupplyOrderContainerService)objects[10];
                SupplyOrder containerSupplyOrder = (SupplyOrder)objects[11];
                User containerUser = (User)objects[12];
                PortWorkService portWorkService = (PortWorkService)objects[13];
                SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[14];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[15];
                Organization organization = (Organization)objects[16];
                Currency currency = (Currency)objects[17];
                SupplyOrder portWorkSupplyOrder = (SupplyOrder)objects[18];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[19];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[20];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[21];
                User portWorkUser = (User)objects[22];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.AccountingContainerPaymentTask) && i.Id.Equals(supplyPaymentTask.Id));

                itemFromList.IsAccounting = true;

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.SupplyPaymentTask != null) {
                    if (containerSupplyOrder != null)
                        itemFromList.Number = containerSupplyOrder.SupplyOrderNumberId.ToString();
                    else if (portWorkSupplyOrder != null)
                        itemFromList.Number = portWorkSupplyOrder.SupplyOrderNumberId.ToString();

                    if (containerService != null) {
                        if (!itemFromList.SupplyPaymentTask.ContainerServices.Any(s => s.Id.Equals(containerService.Id))) {
                            if (junction != null) {
                                junction.SupplyOrder = containerSupplyOrder;

                                containerService.SupplyOrderContainerServices.Add(junction);
                            }

                            if (containerInvoiceDocument != null) containerService.InvoiceDocuments.Add(containerInvoiceDocument);

                            if (containerSupplyOrganizationAgreement != null) {
                                containerSupplyOrganizationAgreement.Currency = containerOrganizationCurrency;

                                containerSupplyOrganizationAgreement.Organization = containerOrganizationOrganization;
                            }

                            containerService.SupplyOrganizationAgreement = containerSupplyOrganizationAgreement;
                            containerService.BillOfLadingDocument = billOfLadingDocument;
                            containerService.User = containerUser;
                            containerService.ContainerOrganization = containerOrganization;

                            itemFromList.SupplyPaymentTask.ContainerServices.Add(containerService);
                        } else {
                            ContainerService fromList = itemFromList.SupplyPaymentTask.ContainerServices.First(s => s.Id.Equals(containerService.Id));

                            if (junction != null && !fromList.SupplyOrderContainerServices.Any(j => j.Id.Equals(junction.Id))) {
                                junction.SupplyOrder = containerSupplyOrder;

                                fromList.SupplyOrderContainerServices.Add(junction);
                            }

                            if (containerInvoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(containerInvoiceDocument.Id)))
                                fromList.InvoiceDocuments.Add(containerInvoiceDocument);
                        }
                    }

                    if (portWorkService != null) {
                        if (!itemFromList.SupplyPaymentTask.PortWorkServices.Any(s => s.Id.Equals(portWorkService.Id))) {
                            if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                            if (serviceDetailItem != null) {
                                serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                                portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                            }

                            if (portWorkSupplyOrder != null) portWorkService.SupplyOrders.Add(portWorkSupplyOrder);

                            if (supplyOrganizationAgreement != null) {
                                supplyOrganizationAgreement.Currency = currency;

                                supplyOrganizationAgreement.Organization = organization;
                            }

                            portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                            portWorkService.PortWorkOrganization = portWorkOrganization;
                            portWorkService.User = portWorkUser;

                            supplyPaymentTask.PortWorkServices.Add(portWorkService);
                        } else {
                            PortWorkService fromList = itemFromList.SupplyPaymentTask.PortWorkServices.First(s => s.Id.Equals(portWorkService.Id));

                            if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);

                            if (serviceDetailItem != null && !fromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                                serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                                fromList.ServiceDetailItems.Add(serviceDetailItem);
                            }

                            if (portWorkSupplyOrder != null && !fromList.SupplyOrders.Any(o => o.Id.Equals(portWorkSupplyOrder.Id))) fromList.SupplyOrders.Add(portWorkSupplyOrder);
                        }
                    }
                } else {
                    if (containerSupplyOrder != null)
                        itemFromList.Number = containerSupplyOrder.SupplyOrderNumberId.ToString();
                    else if (portWorkSupplyOrder != null)
                        itemFromList.Number = portWorkSupplyOrder.SupplyOrderNumberId.ToString();

                    if (containerService != null) {
                        if (junction != null) {
                            junction.SupplyOrder = containerSupplyOrder;

                            containerService.SupplyOrderContainerServices.Add(junction);
                        }

                        if (containerInvoiceDocument != null) containerService.InvoiceDocuments.Add(containerInvoiceDocument);

                        if (containerSupplyOrganizationAgreement != null) {
                            containerSupplyOrganizationAgreement.Currency = containerOrganizationCurrency;

                            containerSupplyOrganizationAgreement.Organization = containerOrganizationOrganization;
                        }

                        containerService.SupplyOrganizationAgreement = containerSupplyOrganizationAgreement;
                        containerService.BillOfLadingDocument = billOfLadingDocument;
                        containerService.User = containerUser;
                        containerService.ContainerOrganization = containerOrganization;

                        supplyPaymentTask.ContainerServices.Add(containerService);
                    }

                    if (portWorkService != null) {
                        if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (portWorkSupplyOrder != null) portWorkService.SupplyOrders.Add(portWorkSupplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        portWorkService.PortWorkOrganization = portWorkOrganization;
                        portWorkService.User = portWorkUser;

                        supplyPaymentTask.PortWorkServices.Add(portWorkService);
                    }

                    if (supplyPaymentTaskDocument != null) supplyPaymentTask.SupplyPaymentTaskDocuments.Add(supplyPaymentTaskDocument);

                    supplyPaymentTask.User = user;

                    itemFromList.SupplyPaymentTask = supplyPaymentTask;

                    itemFromList.Comment = supplyPaymentTask.Comment;
                }

                return supplyPaymentTask;
            };

            _connection.Query(
                "SELECT [SupplyPaymentTask].ID " +
                ",[SupplyPaymentTask].[Created] " +
                ",[SupplyPaymentTask].[Deleted] " +
                ",[SupplyPaymentTask].[NetUID] " +
                ",[SupplyPaymentTask].[Updated] " +
                ",[SupplyPaymentTask].[Comment] " +
                ",[SupplyPaymentTask].[UserID] " +
                ",[SupplyPaymentTask].[PayToDate] " +
                ",[SupplyPaymentTask].[TaskAssignedTo] " +
                ",[SupplyPaymentTask].[TaskStatus] " +
                ",[SupplyPaymentTask].[TaskStatusUpdated] " +
                ",(CASE WHEN [ContainerSupplyOrganizationAgreement].CurrencyID IS NOT NULL " +
                "THEN [dbo].[GetExchangedToEuroValue](" +
                "[SupplyPaymentTask].[GrossPrice], " +
                "[ContainerSupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") " +
                "ELSE [dbo].[GetExchangedToEuroValue](" +
                "[SupplyPaymentTask].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") " +
                "END) AS [GrossPrice] " +
                ",(CASE WHEN [ContainerSupplyOrganizationAgreement].CurrencyID IS NOT NULL " +
                "THEN [dbo].[GetExchangedToEuroValue](" +
                "[SupplyPaymentTask].[NetPrice], " +
                "[ContainerSupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") " +
                "ELSE [dbo].[GetExchangedToEuroValue](" +
                "[SupplyPaymentTask].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") " +
                "END) AS [NetPrice] " +
                ",[SupplyPaymentTask].[IsAvailableForPayment] " +
                ",[User].* " +
                ",[SupplyPaymentTaskDocument].* " +
                ",[ContainerService].[ID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[ContainerService].[NetPrice], " +
                "[ContainerSupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[ContainerService].[BillOfLadingDocumentID] " +
                ",[ContainerService].[Created] " +
                ",[ContainerService].[Deleted] " +
                ",[ContainerService].[IsActive] " +
                ",[ContainerService].[LoadDate] " +
                ",[ContainerService].[NetUID] " +
                ",[ContainerService].[TermDeliveryInDays] " +
                ",[ContainerService].[Updated] " +
                ",[ContainerService].[SupplyPaymentTaskID] " +
                ",[ContainerService].[UserID] " +
                ",[ContainerService].[ContainerOrganizationID] " +
                ",[ContainerService].[FromDate] " +
                ",[ContainerService].[GroosWeight] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[ContainerService].[GrossPrice], " +
                "[ContainerSupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ",[ContainerService].[Vat] " +
                ",[ContainerService].[Number] " +
                ",[ContainerService].[Name] " +
                ",[ContainerService].[VatPercent] " +
                ",[ContainerService].[IsCalculatedExtraCharge] " +
                ",[ContainerService].[SupplyExtraChargeType] " +
                ",[ContainerService].[ContainerNumber] " +
                ",[ContainerService].[ServiceNumber] " +
                ",[ContainerService].[SupplyOrganizationAgreementID] " +
                ",[ContainerOrganization].* " +
                ",[ContainerSupplyOrganizationAgreement].* " +
                ",[ContainerOrganizationOrganization].* " +
                ",[ContainerOrganizationCurrency].* " +
                ",[BillOfLadingDocument].* " +
                ",[ContainerInvoiceDocument].* " +
                ",[SupplyOrderContainerService].* " +
                ",[SupplyOrder].* " +
                ",[ContainerUser].* " +
                ",[PortWorkService].[ID] " +
                ",[PortWorkService].[Created] " +
                ",[PortWorkService].[Deleted] " +
                ",[PortWorkService].[IsActive] " +
                ",[PortWorkService].[NetUID] " +
                ",[PortWorkService].[SupplyPaymentTaskID] " +
                ",[PortWorkService].[Updated] " +
                ",[PortWorkService].[UserID] " +
                ",[PortWorkService].[PortWorkOrganizationID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortWorkService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [GrossPrice] " +
                ",[PortWorkService].[FromDate] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortWorkService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE()" +
                ") AS [NetPrice] " +
                ",[PortWorkService].[Vat] " +
                ",[PortWorkService].[Number] " +
                ",[PortWorkService].[Name] " +
                ",[PortWorkService].[VatPercent] " +
                ",[PortWorkService].[ServiceNumber] " +
                ",[PortWorkService].[SupplyOrganizationAgreementID] " +
                ",[PortWorkOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[PortWorkOrganizationOrganization].* " +
                ",[PortWorkCurrency].* " +
                ",[PortWorkSupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[PortWorkInvoiceDocument].* " +
                ",[PortWorkUser].* " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [SupplyPaymentTaskDocument] " +
                "ON [SupplyPaymentTaskDocument].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [ContainerService] " +
                "ON [ContainerService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
                "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ContainerSupplyOrganizationAgreement] " +
                "ON [ContainerSupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                "LEFT JOIN (SELECT " +
                "[Organization].[ID] " +
                ",[Organization].[Created] " +
                ",[Organization].[Deleted] " +
                ",(CASE WHEN [OrganizationTranslation].[Name] IS NOT NULL THEN [OrganizationTranslation].[Name] ELSE [Organization].[Name] END) AS [Name]  " +
                ",[Organization].[NetUID] " +
                ",[Organization].[Updated] " +
                ",[Organization].[Code] " +
                ",[Organization].[Culture] " +
                ",[Organization].[Address] " +
                ",[Organization].[FullName] " +
                ",[Organization].[IsIndividual] " +
                ",[Organization].[PFURegistrationNumber] " +
                ",[Organization].[PhoneNumber] " +
                ",[Organization].[CurrencyID] " +
                ",[Organization].[StorageID] " +
                "FROM [Organization] " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                ") AS [ContainerOrganizationOrganization] " +
                "ON [ContainerOrganizationOrganization].[ID] = [ContainerSupplyOrganizationAgreement].[OrganizationID] " +
                "LEFT JOIN [views].[CurrencyView] AS [ContainerOrganizationCurrency] " +
                "ON [ContainerOrganizationCurrency].ID = [ContainerSupplyOrganizationAgreement].CurrencyID " +
                "AND [ContainerOrganizationCurrency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] AS [ContainerInvoiceDocument] " +
                "ON [ContainerInvoiceDocument].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrderContainerService] " +
                "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                "LEFT JOIN [User] AS [ContainerUser] " +
                "ON [ContainerUser].ID = [ContainerService].UserID " +
                "LEFT JOIN [PortWorkService] " +
                "ON [PortWorkService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [PortWorkOrganization] " +
                "ON [PortWorkOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] AS [PortWorkOrganizationOrganization] " +
                "ON [PortWorkOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [PortWorkCurrency] " +
                "ON [PortWorkCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [PortWorkCurrency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] AS [PortWorkSupplyOrder] " +
                "ON [PortWorkSupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] AS [PortWorkInvoiceDocument] " +
                "ON [PortWorkInvoiceDocument].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [User] AS [PortWorkUser] " +
                "ON [PortWorkUser].ID = [PortWorkService].UserID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingContainerPaymentTask)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingVehicleService))) {
            Type[] joinTypes = {
                typeof(VehicleService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderVehicleService),
                typeof(SupplyOrder),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], VehicleService> joinMapper = objects => {
                VehicleService vehicleService = (VehicleService)objects[0];
                SupplyOrganization vehicleOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                SupplyOrderVehicleService junction = (SupplyOrderVehicleService)objects[7];
                SupplyOrder supplyOrder = (SupplyOrder)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow
                        .AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.AccountingVehicleService) && i.Id.Equals(vehicleService.Id));
                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.VehicleService != null) {
                    if (invoiceDocument != null && !itemFromList.VehicleService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.VehicleService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction == null || itemFromList.VehicleService.SupplyOrderVehicleServices.Any(j => j.Id.Equals(junction.Id)))
                        return vehicleService;

                    junction.SupplyOrder = supplyOrder;

                    itemFromList.VehicleService.SupplyOrderVehicleServices.Add(junction);
                } else {
                    itemFromList.Number = vehicleService.ServiceNumber;

                    if (invoiceDocument != null) vehicleService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction != null) {
                        junction.SupplyOrder = supplyOrder;

                        vehicleService.SupplyOrderVehicleServices.Add(junction);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    vehicleService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    vehicleService.VehicleOrganization = vehicleOrganization;
                    vehicleService.BillOfLadingDocument = billOfLadingDocument;
                    vehicleService.User = user;

                    itemFromList.VehicleService = vehicleService;
                }

                return vehicleService;
            };

            _connection.Query(
                "SELECT [VehicleService].[ID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[VehicleService].[AccountingNetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ",[VehicleService].[BillOfLadingDocumentID] " +
                ",[VehicleService].[Created] " +
                ",[VehicleService].[Deleted] " +
                ",[VehicleService].[IsActive] " +
                ",[VehicleService].[LoadDate] " +
                ",[VehicleService].[NetUID] " +
                ",[VehicleService].[TermDeliveryInDays] " +
                ",[VehicleService].[Updated] " +
                ",[VehicleService].[SupplyPaymentTaskID] " +
                ",[VehicleService].[UserID] " +
                ",[VehicleService].[VehicleOrganizationID] " +
                ",[VehicleService].[FromDate] " +
                ",[VehicleService].[GrossWeight] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[VehicleService].[AccountingGrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ",[VehicleService].[Vat] " +
                ",[VehicleService].[Number] " +
                ",[VehicleService].[Name] " +
                ",[VehicleService].[VatPercent] " +
                ",[VehicleService].[IsCalculatedExtraCharge] " +
                ",[VehicleService].[SupplyExtraChargeType] " +
                ",[VehicleService].[VehicleNumber] " +
                ",[VehicleService].[ServiceNumber] " +
                ",[VehicleService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[BillOfLadingDocument].* " +
                ",[InvoiceDocument].* " +
                ",[SupplyOrderVehicleService].* " +
                ",[SupplyOrder].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [VehicleService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [VehicleService].VehicleOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [VehicleService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].VehicleServiceID = [VehicleService].ID " +
                "LEFT JOIN [SupplyOrderVehicleService] " +
                "ON [SupplyOrderVehicleService].VehicleServiceID = [VehicleService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderVehicleService].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [VehicleService].UserID " +
                "WHERE [VehicleService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingVehicleService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingCustomService))) {
            Type[] joinTypes = {
                typeof(CustomService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(OrganizationTranslation)
            };

            Func<object[], CustomService> joinMapper = objects => {
                CustomService customService = (CustomService)objects[0];
                SupplyOrganization customOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement customOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization customOrganizationOrganization = (Organization)objects[3];
                SupplyOrganization exciseDutyOrganization = (SupplyOrganization)objects[4];
                SupplyOrganizationAgreement exciseDutyOrganizationAgreement = (SupplyOrganizationAgreement)objects[5];
                Organization exciseDutyOrganizationOrganization = (Organization)objects[6];
                SupplyOrder supplyOrder = (SupplyOrder)objects[7];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[8];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[9];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[10];
                User user = (User)objects[11];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[12];
                Currency currency = (Currency)objects[13];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[14];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.AccountingCustomService) && i.Id.Equals(customService.Id));

                itemFromList.IsAccounting = true;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.CustomService != null) {
                    if (invoiceDocument != null && !itemFromList.CustomService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.CustomService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem == null || itemFromList.CustomService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id)))
                        return customService;

                    serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                    itemFromList.CustomService.ServiceDetailItems.Add(serviceDetailItem);
                } else {
                    itemFromList.Number = customService.ServiceNumber;

                    if (invoiceDocument != null) customService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        customService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (customOrganization != null && customOrganizationAgreement != null) {
                        if (!customOrganization.SupplyOrganizationAgreements.Any(x => x.Id == customOrganizationAgreement.Id))
                            customOrganization.SupplyOrganizationAgreements.Add(customOrganizationAgreement);
                        else
                            customOrganizationAgreement = customOrganization.SupplyOrganizationAgreements.First(x => x.Id == customOrganizationAgreement.Id);

                        customOrganizationAgreement.Organization = customOrganizationOrganization;
                    }

                    if (exciseDutyOrganization != null && exciseDutyOrganizationAgreement != null) {
                        if (!exciseDutyOrganization.SupplyOrganizationAgreements.Any(x => x.Id == exciseDutyOrganizationAgreement.Id))
                            exciseDutyOrganization.SupplyOrganizationAgreements.Add(exciseDutyOrganizationAgreement);
                        else
                            exciseDutyOrganizationAgreement = exciseDutyOrganization.SupplyOrganizationAgreements.First(x => x.Id == exciseDutyOrganizationAgreement.Id);

                        exciseDutyOrganizationAgreement.Organization = exciseDutyOrganizationOrganization;
                    }

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                    customService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    customService.CustomOrganization = customOrganization;
                    customService.ExciseDutyOrganization = exciseDutyOrganization;
                    customService.SupplyOrder = supplyOrder;
                    customService.User = user;

                    itemFromList.CustomService = customService;
                }

                return customService;
            };

            _connection.Query(
                "SELECT [CustomService].[ID] " +
                ",[CustomService].[Created] " +
                ",[CustomService].[Deleted] " +
                ",[CustomService].[IsActive] " +
                ",[CustomService].[NetUID] " +
                ",[CustomService].[SupplyOrderID] " +
                ",[CustomService].[SupplyPaymentTaskID] " +
                ",[CustomService].[Updated] " +
                ",[CustomService].[UserID] " +
                ",[CustomService].[CustomOrganizationID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[CustomService].[AccountingGrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ",[CustomService].[FromDate] " +
                ",[CustomService].[Number] " +
                ",[CustomService].[SupplyCustomType] " +
                ",[CustomService].[ExciseDutyOrganizationID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[CustomService].[AccountingNetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ",[CustomService].[Vat] " +
                ",[CustomService].[Name] " +
                ",[CustomService].[VatPercent] " +
                ",[CustomService].[ServiceNumber] " +
                ",[CustomService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[ExciseDutyOrganization].* " +
                ",[ExciseDutyOrganizationAgreement].* " +
                ",[ExciseDutyOrganizationOrganization].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Currency].* " +
                ",[OrganizationTranslation].* " +
                "FROM [CustomService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [CustomService].CustomOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].[SupplyOrganizationID] = [SupplyOrganization].[ID] " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
                "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ExciseDutyOrganizationAgreement] " +
                "ON [ExciseDutyOrganizationAgreement].[SupplyOrganizationID] = [ExciseDutyOrganization].[ID] " +
                "LEFT JOIN [Organization] AS [ExciseDutyOrganizationOrganization] " +
                "ON [ExciseDutyOrganizationOrganization].ID = [ExciseDutyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [CustomService].SupplyOrderID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].CustomServiceID = [CustomService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].CustomServiceID = [CustomService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [CustomService].UserID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "WHERE [CustomService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingCustomService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingTransportationService))) {
            Type[] joinTypes = {
                typeof(TransportationService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], TransportationService> joinMapper = objects => {
                TransportationService transportationService = (TransportationService)objects[0];
                SupplyOrganization transportationOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.AccountingTransportationService) && i.Id.Equals(transportationService.Id));

                itemFromList.IsAccounting = true;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.TransportationService != null) {
                    if (invoiceDocument != null && !itemFromList.TransportationService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.TransportationService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.TransportationService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.TransportationService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.TransportationService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.TransportationService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = transportationService.ServiceNumber;

                    if (invoiceDocument != null) transportationService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        transportationService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) transportationService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    transportationService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    transportationService.TransportationOrganization = transportationOrganization;
                    transportationService.User = user;

                    itemFromList.TransportationService = transportationService;
                }

                return transportationService;
            };

            _connection.Query(
                "SELECT [TransportationService].[ID] " +
                ",[TransportationService].[Created] " +
                ",[TransportationService].[Deleted] " +
                ",[TransportationService].[IsActive] " +
                ",[TransportationService].[NetUID] " +
                ",[TransportationService].[SupplyPaymentTaskID] " +
                ",[TransportationService].[Updated] " +
                ",[TransportationService].[UserID] " +
                ",[TransportationService].[TransportationOrganizationID] " +
                ",[TransportationService].[FromDate] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[TransportationService].[AccountingNetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[TransportationService].[AccountingGrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ",[TransportationService].[Vat] " +
                ",[TransportationService].[Number] " +
                ",[TransportationService].[IsSealAndSignatureVerified] " +
                ",[TransportationService].[Name] " +
                ",[TransportationService].[VatPercent] " +
                ",[TransportationService].[ServiceNumber] " +
                ",[TransportationService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [TransportationService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [TransportationService].TransportationOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [TransportationService].UserID " +
                "WHERE [TransportationService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingTransportationService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingPortCustomAgencyService))) {
            Type[] joinTypes = {
                typeof(PortCustomAgencyService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], PortCustomAgencyService> joinMapper = objects => {
                PortCustomAgencyService portCustomAgencyService = (PortCustomAgencyService)objects[0];
                SupplyOrganization portCustomAgencyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.AccountingPortCustomAgencyService) && i.Id.Equals(portCustomAgencyService.Id));

                itemFromList.IsAccounting = true;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.PortCustomAgencyService != null) {
                    if (invoiceDocument != null && !itemFromList.PortCustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PortCustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PortCustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PortCustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PortCustomAgencyService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PortCustomAgencyService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = portCustomAgencyService.ServiceNumber;

                    if (invoiceDocument != null) portCustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        portCustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) portCustomAgencyService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    portCustomAgencyService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    portCustomAgencyService.PortCustomAgencyOrganization = portCustomAgencyOrganization;
                    portCustomAgencyService.User = user;

                    itemFromList.PortCustomAgencyService = portCustomAgencyService;
                }

                return portCustomAgencyService;
            };

            _connection.Query(
                "SELECT [PortCustomAgencyService].[ID] " +
                ",[PortCustomAgencyService].[Created] " +
                ",[PortCustomAgencyService].[Deleted] " +
                ",[PortCustomAgencyService].[FromDate] " +
                ",[PortCustomAgencyService].[IsActive] " +
                ",[PortCustomAgencyService].[NetUID] " +
                ",[PortCustomAgencyService].[PortCustomAgencyOrganizationID] " +
                ",[PortCustomAgencyService].[SupplyPaymentTaskID] " +
                ",[PortCustomAgencyService].[Updated] " +
                ",[PortCustomAgencyService].[UserID] " +
                ",[PortCustomAgencyService].[Vat] " +
                ",[PortCustomAgencyService].[Number] " +
                ",[PortCustomAgencyService].[Name] " +
                ",[PortCustomAgencyService].[VatPercent] " +
                ",[PortCustomAgencyService].[ServiceNumber] " +
                ",[PortCustomAgencyService].[SupplyOrganizationAgreementID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortCustomAgencyService].[AccountingGrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortCustomAgencyService].[AccountingNetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [PortCustomAgencyService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PortCustomAgencyService].UserID " +
                "WHERE [PortCustomAgencyService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingPortCustomAgencyService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingCustomAgencyService))) {
            Type[] joinTypes = {
                typeof(CustomAgencyService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], CustomAgencyService> joinMapper = objects => {
                CustomAgencyService customAgencyService = (CustomAgencyService)objects[0];
                SupplyOrganization customAgencyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.AccountingCustomAgencyService) && i.Id.Equals(customAgencyService.Id));

                itemFromList.IsAccounting = true;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.CustomAgencyService != null) {
                    if (invoiceDocument != null && !itemFromList.CustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.CustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.CustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.CustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.CustomAgencyService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.CustomAgencyService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = customAgencyService.ServiceNumber;

                    if (invoiceDocument != null) customAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        customAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) customAgencyService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    customAgencyService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    customAgencyService.CustomAgencyOrganization = customAgencyOrganization;
                    customAgencyService.User = user;

                    itemFromList.CustomAgencyService = customAgencyService;
                }

                return customAgencyService;
            };

            _connection.Query(
                "SELECT [CustomAgencyService].[ID] " +
                ",[CustomAgencyService].[Created] " +
                ",[CustomAgencyService].[CustomAgencyOrganizationID] " +
                ",[CustomAgencyService].[Deleted] " +
                ",[CustomAgencyService].[FromDate] " +
                ",[CustomAgencyService].[IsActive] " +
                ",[CustomAgencyService].[NetUID] " +
                ",[CustomAgencyService].[SupplyPaymentTaskID] " +
                ",[CustomAgencyService].[Updated] " +
                ",[CustomAgencyService].[UserID] " +
                ",[CustomAgencyService].[Vat] " +
                ",[CustomAgencyService].[Number] " +
                ",[CustomAgencyService].[Name] " +
                ",[CustomAgencyService].[VatPercent] " +
                ",[CustomAgencyService].[ServiceNumber] " +
                ",[CustomAgencyService].[SupplyOrganizationAgreementID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[CustomAgencyService].[AccountingGrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[CustomAgencyService].[AccountingNetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [CustomAgencyService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [CustomAgencyService].UserID " +
                "WHERE [CustomAgencyService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingCustomAgencyService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingPlaneDeliveryService))) {
            Type[] joinTypes = {
                typeof(PlaneDeliveryService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], PlaneDeliveryService> joinMapper = objects => {
                PlaneDeliveryService planeDeliveryService = (PlaneDeliveryService)objects[0];
                SupplyOrganization planeDeliveryOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.AccountingPlaneDeliveryService) && i.Id.Equals(planeDeliveryService.Id));

                itemFromList.IsAccounting = true;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.PlaneDeliveryService != null) {
                    if (invoiceDocument != null && !itemFromList.PlaneDeliveryService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PlaneDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PlaneDeliveryService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PlaneDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PlaneDeliveryService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PlaneDeliveryService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = planeDeliveryService.ServiceNumber;

                    if (invoiceDocument != null) planeDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        planeDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) planeDeliveryService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    planeDeliveryService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    planeDeliveryService.PlaneDeliveryOrganization = planeDeliveryOrganization;
                    planeDeliveryService.User = user;

                    itemFromList.PlaneDeliveryService = planeDeliveryService;
                }

                return planeDeliveryService;
            };

            _connection.Query(
                "SELECT [PlaneDeliveryService].[ID] " +
                ",[PlaneDeliveryService].[Created] " +
                ",[PlaneDeliveryService].[Deleted] " +
                ",[PlaneDeliveryService].[FromDate] " +
                ",[PlaneDeliveryService].[IsActive] " +
                ",[PlaneDeliveryService].[NetUID] " +
                ",[PlaneDeliveryService].[PlaneDeliveryOrganizationID] " +
                ",[PlaneDeliveryService].[SupplyPaymentTaskID] " +
                ",[PlaneDeliveryService].[Updated] " +
                ",[PlaneDeliveryService].[UserID] " +
                ",[PlaneDeliveryService].[Vat] " +
                ",[PlaneDeliveryService].[Number] " +
                ",[PlaneDeliveryService].[Name] " +
                ",[PlaneDeliveryService].[VatPercent] " +
                ",[PlaneDeliveryService].[ServiceNumber] " +
                ",[PlaneDeliveryService].[SupplyOrganizationAgreementID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PlaneDeliveryService].[AccountingGrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PlaneDeliveryService].[AccountingNetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [PlaneDeliveryService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PlaneDeliveryService].PlaneDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PlaneDeliveryService].UserID " +
                "WHERE [PlaneDeliveryService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingPlaneDeliveryService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingVehicleDeliveryService))) {
            Type[] joinTypes = {
                typeof(VehicleDeliveryService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], VehicleDeliveryService> joinMapper = objects => {
                VehicleDeliveryService vehicleDeliveryService = (VehicleDeliveryService)objects[0];
                SupplyOrganization vehicleDeliveryOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.AccountingVehicleDeliveryService) && i.Id.Equals(vehicleDeliveryService.Id));

                itemFromList.IsAccounting = true;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.VehicleDeliveryService != null) {
                    if (invoiceDocument != null && !itemFromList.VehicleDeliveryService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.VehicleDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.VehicleDeliveryService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.VehicleDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.VehicleDeliveryService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.VehicleDeliveryService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = vehicleDeliveryService.ServiceNumber;

                    if (invoiceDocument != null) vehicleDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        vehicleDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) vehicleDeliveryService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    vehicleDeliveryService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    vehicleDeliveryService.VehicleDeliveryOrganization = vehicleDeliveryOrganization;
                    vehicleDeliveryService.User = user;

                    itemFromList.VehicleDeliveryService = vehicleDeliveryService;
                }

                return vehicleDeliveryService;
            };

            _connection.Query(
                "SELECT [VehicleDeliveryService].[ID] " +
                ",[VehicleDeliveryService].[Created] " +
                ",[VehicleDeliveryService].[Deleted] " +
                ",[VehicleDeliveryService].[FromDate] " +
                ",[VehicleDeliveryService].[IsActive] " +
                ",[VehicleDeliveryService].[NetUID] " +
                ",[VehicleDeliveryService].[SupplyPaymentTaskID] " +
                ",[VehicleDeliveryService].[Updated] " +
                ",[VehicleDeliveryService].[UserID] " +
                ",[VehicleDeliveryService].[VehicleDeliveryOrganizationID] " +
                ",[VehicleDeliveryService].[Vat] " +
                ",[VehicleDeliveryService].[Number] " +
                ",[VehicleDeliveryService].[IsSealAndSignatureVerified] " +
                ",[VehicleDeliveryService].[Name] " +
                ",[VehicleDeliveryService].[VatPercent] " +
                ",[VehicleDeliveryService].[ServiceNumber] " +
                ",[VehicleDeliveryService].[SupplyOrganizationAgreementID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[VehicleDeliveryService].[AccountingGrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[VehicleDeliveryService].[AccountingNetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [VehicleDeliveryService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [VehicleDeliveryService].UserID " +
                "WHERE [VehicleDeliveryService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingVehicleDeliveryService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingMergedService))) {
            Type[] joinTypes = {
                typeof(MergedService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(SupplyOrderUkraine),
                typeof(OrganizationTranslation),
                typeof(ConsumableProduct),
                typeof(ActProvidingService)
            };

            Func<object[], MergedService> joinMapper = objects => {
                MergedService mergedService = (MergedService)objects[0];
                SupplyOrganization mergedSupplyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[10];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[11];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[12];
                ActProvidingService actProvidingService = (ActProvidingService)objects[13];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.AccountingMergedService) && i.Id.Equals(mergedService.Id));

                itemFromList.IsAccounting = true;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.MergedService != null) {
                    itemFromList.Number = itemFromList.MergedService.AccountingActProvidingService.Number;

                    itemFromList.MergedService.ConsumableProduct = consumableProduct;
                    itemFromList.MergedService.AccountingActProvidingService = actProvidingService;

                    if (invoiceDocument != null && !itemFromList.MergedService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.MergedService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem == null || itemFromList.MergedService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id)))
                        return mergedService;

                    serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                    itemFromList.MergedService.ServiceDetailItems.Add(serviceDetailItem);
                } else {
                    if (mergedService != null) {
                        if (actProvidingService != null) itemFromList.Number = actProvidingService.Number;

                        mergedService.ConsumableProduct = consumableProduct;
                        mergedService.AccountingActProvidingService = actProvidingService;

                        if (invoiceDocument != null) mergedService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            mergedService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        mergedService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        mergedService.SupplyOrganization = mergedSupplyOrganization;
                        mergedService.SupplyOrderUkraine = supplyOrderUkraine;
                        mergedService.SupplyOrder = supplyOrder;
                        mergedService.User = user;
                    }

                    itemFromList.MergedService = mergedService;
                }

                return mergedService;
            };

            _connection.Query(
                "SELECT [MergedService].[ID] " +
                ",[MergedService].[Created] " +
                ",[MergedService].[Deleted] " +
                ",[MergedService].[FromDate] " +
                ",[MergedService].[IsActive] " +
                ",[MergedService].[NetUID] " +
                ",[MergedService].[SupplyPaymentTaskID] " +
                ",[MergedService].[Updated] " +
                ",[MergedService].[UserID] " +
                ",[MergedService].[SupplyOrganizationID] " +
                ",[MergedService].[Vat] " +
                ",[MergedService].[Number] " +
                ",[MergedService].[DeliveryProductProtocolID] " +
                ",[MergedService].[AccountingGrossPrice] " +
                ",[MergedService].[AccountingNetPrice] " +
                ",[MergedService].[Name] " +
                ",[MergedService].[VatPercent] " +
                ",[MergedService].[ServiceNumber] " +
                ",[MergedService].[SupplyOrganizationAgreementID] " +
                ",[MergedService].[ConsumableProductID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[MergedService].[AccountingGrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[MergedService].[AccountingNetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[SupplyOrderUkraine].* " +
                ",[OrganizationTranslation].* " +
                ",[ConsumableProduct].* " +
                ",[ActProvidingService].* " +
                "FROM [MergedService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [MergedService].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [MergedService].SupplyOrderID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].MergedServiceID = [MergedService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].MergedServiceID = [MergedService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [MergedService].UserID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [MergedService].SupplyOrderUkraineID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [ConsumableProduct] " +
                "ON [ConsumableProduct].[ID] = [MergedService].[ConsumableProductID] " +
                "LEFT JOIN [ActProvidingService] " +
                "ON [ActProvidingService].[ID] = [MergedService].[AccountingActProvidingServiceID] " +
                "WHERE [MergedService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingMergedService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingContainerService))) {
            Type[] joinTypes = {
                typeof(ContainerService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderContainerService),
                typeof(SupplyOrder),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], ContainerService> joinMapper = objects => {
                ContainerService containerService = (ContainerService)objects[0];
                SupplyOrganization containerOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                SupplyOrderContainerService junction = (SupplyOrderContainerService)objects[7];
                SupplyOrder supplyOrder = (SupplyOrder)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow
                        .AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.AccountingContainerService) && i.Id.Equals(containerService.Id));
                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.ContainerService != null) {
                    if (invoiceDocument != null && !itemFromList.ContainerService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.ContainerService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction == null || itemFromList.ContainerService.SupplyOrderContainerServices.Any(j => j.Id.Equals(junction.Id)))
                        return containerService;

                    junction.SupplyOrder = supplyOrder;

                    itemFromList.ContainerService.SupplyOrderContainerServices.Add(junction);
                } else {
                    itemFromList.Number = containerService.ServiceNumber;

                    if (invoiceDocument != null) containerService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction != null) {
                        junction.SupplyOrder = supplyOrder;

                        containerService.SupplyOrderContainerServices.Add(junction);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    containerService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    containerService.ContainerOrganization = containerOrganization;
                    containerService.BillOfLadingDocument = billOfLadingDocument;
                    containerService.User = user;

                    itemFromList.ContainerService = containerService;
                }

                return containerService;
            };

            _connection.Query(
                "SELECT [ContainerService].[ID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[ContainerService].[AccountingNetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ",[ContainerService].[BillOfLadingDocumentID] " +
                ",[ContainerService].[Created] " +
                ",[ContainerService].[Deleted] " +
                ",[ContainerService].[IsActive] " +
                ",[ContainerService].[LoadDate] " +
                ",[ContainerService].[NetUID] " +
                ",[ContainerService].[TermDeliveryInDays] " +
                ",[ContainerService].[Updated] " +
                ",[ContainerService].[SupplyPaymentTaskID] " +
                ",[ContainerService].[UserID] " +
                ",[ContainerService].[ContainerOrganizationID] " +
                ",[ContainerService].[FromDate] " +
                ",[ContainerService].[GroosWeight] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[ContainerService].[AccountingGrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ",[ContainerService].[Vat] " +
                ",[ContainerService].[Number] " +
                ",[ContainerService].[Name] " +
                ",[ContainerService].[VatPercent] " +
                ",[ContainerService].[IsCalculatedExtraCharge] " +
                ",[ContainerService].[SupplyExtraChargeType] " +
                ",[ContainerService].[ContainerNumber] " +
                ",[ContainerService].[ServiceNumber] " +
                ",[ContainerService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[BillOfLadingDocument].* " +
                ",[InvoiceDocument].* " +
                ",[SupplyOrderContainerService].* " +
                ",[SupplyOrder].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [ContainerService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrderContainerService] " +
                "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ContainerService].UserID " +
                "WHERE [ContainerService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingContainerService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingPortWorkService))) {
            Type[] joinTypes = {
                typeof(PortWorkService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], PortWorkService> joinMapper = objects => {
                PortWorkService portWorkService = (PortWorkService)objects[0];
                SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.AccountingPortWorkService) && i.Id.Equals(portWorkService.Id));

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.PortWorkService != null) {
                    if (invoiceDocument != null && !itemFromList.PortWorkService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PortWorkService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PortWorkService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PortWorkService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PortWorkService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PortWorkService.SupplyOrders.Add(supplyOrder);
                } else {
                    itemFromList.Number = portWorkService.ServiceNumber;

                    if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null) portWorkService.SupplyOrders.Add(supplyOrder);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    portWorkService.PortWorkOrganization = portWorkOrganization;
                    portWorkService.User = user;

                    itemFromList.PortWorkService = portWorkService;
                }

                return portWorkService;
            };

            _connection.Query(
                "SELECT [PortWorkService].[ID] " +
                ",[PortWorkService].[Created] " +
                ",[PortWorkService].[Deleted] " +
                ",[PortWorkService].[IsActive] " +
                ",[PortWorkService].[NetUID] " +
                ",[PortWorkService].[SupplyPaymentTaskID] " +
                ",[PortWorkService].[Updated] " +
                ",[PortWorkService].[UserID] " +
                ",[PortWorkService].[PortWorkOrganizationID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortWorkService].[AccountingGrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ",[PortWorkService].[FromDate] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[PortWorkService].[AccountingNetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ",[PortWorkService].[Vat] " +
                ",[PortWorkService].[Number] " +
                ",[PortWorkService].[Name] " +
                ",[PortWorkService].[VatPercent] " +
                ",[PortWorkService].[ServiceNumber] " +
                ",[PortWorkService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ", [OrganizationTranslation].* " +
                "FROM [PortWorkService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PortWorkService].UserID " +
                "WHERE [PortWorkService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingPortWorkService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.BillOfLadingService))) {
            Type[] joinTypes = {
                typeof(BillOfLadingService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(SupplyInvoiceBillOfLadingService),
                typeof(SupplyInvoice),
                typeof(InvoiceDocument),
                typeof(SupplyOrder),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], BillOfLadingService> joinMapper = objects => {
                BillOfLadingService service = (BillOfLadingService)objects[0];
                SupplyOrganization supplyOrganizationService = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                SupplyInvoiceBillOfLadingService supplyInvoiceBillOfLadingService = (SupplyInvoiceBillOfLadingService)objects[6];
                SupplyInvoice invoice = (SupplyInvoice)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                User user = (User)objects[10];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[11];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow
                        .AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.BillOfLadingService) && i.Id.Equals(service.Id));
                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.BillOfLadingService == null)
                    itemFromList.BillOfLadingService = service;
                else
                    service = itemFromList.BillOfLadingService;

                itemFromList.Number = service.ServiceNumber;

                supplyOrganizationAgreement.Organization = organization;
                supplyOrganizationAgreement.Currency = currency;
                service.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                service.SupplyOrganization = supplyOrganizationService;
                service.User = user;

                if (supplyInvoiceBillOfLadingService != null) {
                    if (!service.SupplyInvoiceBillOfLadingServices.Any(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id)))
                        service.SupplyInvoiceBillOfLadingServices.Add(supplyInvoiceBillOfLadingService);
                    else
                        supplyInvoiceBillOfLadingService = service.SupplyInvoiceBillOfLadingServices.First(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id));

                    invoice.SupplyOrder = supplyOrder;

                    supplyInvoiceBillOfLadingService.SupplyInvoice = invoice;

                    if (invoiceDocument != null)
                        invoice.InvoiceDocuments.Add(invoiceDocument);
                }

                if (billOfLadingDocument == null) return service;

                if (!service.BillOfLadingDocuments.Any(x => x.Id.Equals(billOfLadingDocument.Id)))
                    service.BillOfLadingDocuments.Add(billOfLadingDocument);
                else
                    billOfLadingDocument = service.BillOfLadingDocuments.First(x => x.Id.Equals(billOfLadingDocument.Id));

                return service;
            };

            _connection.Query(
                "SELECT [BillOfLadingService].[ID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[BillOfLadingService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ",[BillOfLadingService].[Created] " +
                ",[BillOfLadingService].[Deleted] " +
                ",[BillOfLadingService].[IsActive] " +
                ",[BillOfLadingService].[LoadDate] " +
                ",[BillOfLadingService].[NetUID] " +
                ",[BillOfLadingService].[TermDeliveryInDays] " +
                ",[BillOfLadingService].[Updated] " +
                ",[BillOfLadingService].[SupplyPaymentTaskID] " +
                ",[BillOfLadingService].[AccountingPaymentTaskID] " +
                ",[BillOfLadingService].[UserID] " +
                ",[BillOfLadingService].[SupplyOrganizationID] " +
                ",[BillOfLadingService].[FromDate] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[BillOfLadingService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ",[BillOfLadingService].[Vat] " +
                ",[BillOfLadingService].[Number] " +
                ",[BillOfLadingService].[Name] " +
                ",[BillOfLadingService].[VatPercent] " +
                ",[BillOfLadingService].[BillOfLadingNumber] " +
                ",[BillOfLadingService].[ServiceNumber] " +
                ",[BillOfLadingService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[BillOfLadingDocument].* " +
                ",[SupplyInvoiceBillOfLadingService].* " +
                ",[SupplyInvoice].* " +
                ",[InvoiceDocument].* " +
                ",[SupplyOrder].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [BillOfLadingService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [BillOfLadingService].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [BillOfLadingService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].[BillOfLadingServiceID] = [BillOfLadingService].[ID] " +
                "LEFT JOIN [SupplyInvoiceBillOfLadingService] " +
                "ON [SupplyInvoiceBillOfLadingService].BillOfLadingServiceID = [BillOfLadingService].ID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyInvoiceBillOfLadingService].SupplyInvoiceID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [BillOfLadingService].UserID " +
                "WHERE [BillOfLadingService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.BillOfLadingService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingBillOfLadingService))) {
            Type[] joinTypes = {
                typeof(BillOfLadingService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(SupplyInvoiceBillOfLadingService),
                typeof(SupplyInvoice),
                typeof(InvoiceDocument),
                typeof(SupplyOrder),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], BillOfLadingService> joinMapper = objects => {
                BillOfLadingService service = (BillOfLadingService)objects[0];
                SupplyOrganization supplyOrganizationService = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                SupplyInvoiceBillOfLadingService supplyInvoiceBillOfLadingService = (SupplyInvoiceBillOfLadingService)objects[6];
                SupplyInvoice invoice = (SupplyInvoice)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                User user = (User)objects[10];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[11];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow
                        .AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.AccountingBillOfLadingService) && i.Id.Equals(service.Id));

                itemFromList.IsAccounting = true;

                itemFromList.Number = service.ServiceNumber;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.BillOfLadingService == null)
                    itemFromList.BillOfLadingService = service;
                else
                    service = itemFromList.BillOfLadingService;

                supplyOrganizationAgreement.Organization = organization;
                supplyOrganizationAgreement.Currency = currency;
                service.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                service.SupplyOrganization = supplyOrganizationService;
                service.User = user;

                if (supplyInvoiceBillOfLadingService != null) {
                    if (!service.SupplyInvoiceBillOfLadingServices.Any(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id)))
                        service.SupplyInvoiceBillOfLadingServices.Add(supplyInvoiceBillOfLadingService);
                    else
                        supplyInvoiceBillOfLadingService = service.SupplyInvoiceBillOfLadingServices.First(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id));

                    invoice.SupplyOrder = supplyOrder;

                    supplyInvoiceBillOfLadingService.SupplyInvoice = invoice;

                    if (invoiceDocument != null)
                        invoice.InvoiceDocuments.Add(invoiceDocument);
                }

                if (billOfLadingDocument == null) return service;

                if (!service.BillOfLadingDocuments.Any(x => x.Id.Equals(billOfLadingDocument.Id)))
                    service.BillOfLadingDocuments.Add(billOfLadingDocument);
                else
                    billOfLadingDocument = service.BillOfLadingDocuments.First(x => x.Id.Equals(billOfLadingDocument.Id));

                return service;
            };

            _connection.Query(
                "SELECT [BillOfLadingService].[ID] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[BillOfLadingService].[AccountingNetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [NetPrice] " +
                ",[BillOfLadingService].[Created] " +
                ",[BillOfLadingService].[Deleted] " +
                ",[BillOfLadingService].[IsActive] " +
                ",[BillOfLadingService].[LoadDate] " +
                ",[BillOfLadingService].[NetUID] " +
                ",[BillOfLadingService].[TermDeliveryInDays] " +
                ",[BillOfLadingService].[Updated] " +
                ",[BillOfLadingService].[SupplyPaymentTaskID] " +
                ",[BillOfLadingService].[AccountingPaymentTaskID] " +
                ",[BillOfLadingService].[UserID] " +
                ",[BillOfLadingService].[SupplyOrganizationID] " +
                ",[BillOfLadingService].[FromDate] " +
                ", [dbo].[GetExchangedToEuroValue](" +
                "[BillOfLadingService].[AccountingGrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossPrice] " +
                ",[BillOfLadingService].[Vat] " +
                ",[BillOfLadingService].[Number] " +
                ",[BillOfLadingService].[Name] " +
                ",[BillOfLadingService].[VatPercent] " +
                ",[BillOfLadingService].[BillOfLadingNumber] " +
                ",[BillOfLadingService].[ServiceNumber] " +
                ",[BillOfLadingService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[BillOfLadingDocument].* " +
                ",[SupplyInvoiceBillOfLadingService].* " +
                ",[SupplyInvoice].* " +
                ",[InvoiceDocument].* " +
                ",[SupplyOrder].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [BillOfLadingService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [BillOfLadingService].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [BillOfLadingService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].[BillOfLadingServiceID] = [BillOfLadingService].[ID] " +
                "LEFT JOIN [SupplyInvoiceBillOfLadingService] " +
                "ON [SupplyInvoiceBillOfLadingService].BillOfLadingServiceID = [BillOfLadingService].ID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyInvoiceBillOfLadingService].SupplyInvoiceID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [BillOfLadingService].UserID " +
                "WHERE [BillOfLadingService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingBillOfLadingService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        return accountingCashFlow;
    }

    public AccountingCashFlow GetRangedBySupplyOrganizationAgreement(SupplyOrganizationAgreement preDefinedAgreement, DateTime from, DateTime to,
        TypePaymentTask typePaymentTask) {
        string beforeRangeInAmountSqlQuery;

        if (typePaymentTask.Equals(TypePaymentTask.PaymentTask))
            beforeRangeInAmountSqlQuery = "SELECT " +
                                          "ROUND( " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].NetPrice), 0) " +
                                          "FROM [ContainerService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [ContainerService].[SupplyPaymentTaskID] " +
                                          "WHERE [ContainerService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [ContainerService].FromDate < @From " +
                                          "AND [ContainerService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].NetPrice), 0) " +
                                          "FROM [VehicleService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [VehicleService].[SupplyPaymentTaskID] " +
                                          "WHERE [VehicleService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [VehicleService].FromDate < @From " +
                                          "AND [VehicleService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].NetPrice), 0) " +
                                          "FROM [BillOfLadingService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [BillOfLadingService].[SupplyPaymentTaskID] " +
                                          "WHERE [BillOfLadingService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [BillOfLadingService].FromDate < @From " +
                                          "AND [BillOfLadingService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [CustomService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
                                          "WHERE [CustomService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [CustomService].FromDate < @From " +
                                          "AND [CustomService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [PortWorkService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
                                          "WHERE [PortWorkService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [PortWorkService].FromDate < @From " +
                                          "AND [PortWorkService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [TransportationService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
                                          "WHERE [TransportationService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [TransportationService].FromDate < @From " +
                                          "AND [TransportationService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [PortCustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
                                          "WHERE [PortCustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [PortCustomAgencyService].FromDate < @From " +
                                          "AND [PortCustomAgencyService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [CustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
                                          "WHERE [CustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [CustomAgencyService].FromDate < @From " +
                                          "AND [CustomAgencyService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [PlaneDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
                                          "WHERE [PlaneDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [PlaneDeliveryService].FromDate < @From " +
                                          "AND [PlaneDeliveryService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [VehicleDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
                                          "WHERE [VehicleDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [VehicleDeliveryService].FromDate < @From " +
                                          "AND [VehicleDeliveryService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([MergedService].[GrossPrice]), 0) " +
                                          "FROM [MergedService] " +
                                          "WHERE [MergedService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [MergedService].FromDate < @From " +
                                          "AND [MergedService].Deleted = 0 " +
                                          "AND [MergedService].[ActProvidingServiceID] IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([DeliveryExpense].GrossAmount), 0) " +
                                          "FROM [DeliveryExpense] " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                          "LEFT JOIN [ActProvidingService] " +
                                          "ON [ActProvidingService].ID = [DeliveryExpense].ActProvidingServiceID " +
                                          "WHERE [DeliveryExpense].SupplyOrganizationAgreementID = @Id " +
                                          "AND [DeliveryExpense].FromDate < @From " +
                                          "AND [DeliveryExpense].Deleted = 0 " +
                                          "AND [DeliveryExpense].ActProvidingServiceID IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0) " +
                                          "FROM [ConsumablesOrder] " +
                                          "LEFT JOIN [ConsumablesOrderItem] " +
                                          "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                                          "AND [ConsumablesOrderItem].Deleted = 0 " +
                                          "WHERE [ConsumablesOrderItem].SupplyOrganizationAgreementID = @Id " +
                                          "AND [ConsumablesOrder].OrganizationFromDate < @From " +
                                          "AND [ConsumablesOrder].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([IncomePaymentOrder].AgreementExchangedAmount), 0) " +
                                          //"SELECT 0 - ISNULL(SUM([IncomePaymentOrder].EuroAmount * [IncomePaymentOrder].AgreementEuroExchangeRate), 0)  " +
                                          "FROM [IncomePaymentOrder] " +
                                          "WHERE [IncomePaymentOrder].SupplyOrganizationAgreementID = @Id " +
                                          "AND [IncomePaymentOrder].FromDate < @From " +
                                          "AND [IncomePaymentOrder].Deleted = 0 " +
                                          "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                          "AND [IncomePaymentOrder].IsManagementAccounting = 1 " +
                                          ") " +
                                          ", 2) AS [BeforeRangeInAmount]; ";
        else if (typePaymentTask.Equals(TypePaymentTask.AccountingPaymentTask))
            beforeRangeInAmountSqlQuery = "SELECT " +
                                          "ROUND( " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].NetPrice), 0) " +
                                          "FROM [ContainerService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [ContainerService].[AccountingPaymentTaskID] " +
                                          "WHERE [ContainerService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [ContainerService].FromDate < @From " +
                                          "AND [ContainerService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].NetPrice), 0) " +
                                          "FROM [VehicleService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [VehicleService].[AccountingPaymentTaskID] " +
                                          "WHERE [VehicleService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [VehicleService].FromDate < @From " +
                                          "AND [VehicleService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].NetPrice), 0) " +
                                          "FROM [BillOfLadingService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [BillOfLadingService].[AccountingPaymentTaskID] " +
                                          "WHERE [BillOfLadingService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [BillOfLadingService].FromDate < @From " +
                                          "AND [BillOfLadingService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [CustomService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [CustomService].AccountingPaymentTaskID " +
                                          "WHERE [CustomService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [CustomService].FromDate < @From " +
                                          "AND [CustomService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [PortWorkService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PortWorkService].AccountingPaymentTaskID " +
                                          "WHERE [PortWorkService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [PortWorkService].FromDate < @From " +
                                          "AND [PortWorkService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [TransportationService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [TransportationService].AccountingPaymentTaskID " +
                                          "WHERE [TransportationService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [TransportationService].FromDate < @From " +
                                          "AND [TransportationService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [PortCustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].AccountingPaymentTaskID " +
                                          "WHERE [PortCustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [PortCustomAgencyService].FromDate < @From " +
                                          "AND [PortCustomAgencyService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [CustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [CustomAgencyService].AccountingPaymentTaskID " +
                                          "WHERE [CustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [CustomAgencyService].FromDate < @From " +
                                          "AND [CustomAgencyService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [PlaneDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].AccountingPaymentTaskID " +
                                          "WHERE [PlaneDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [PlaneDeliveryService].FromDate < @From " +
                                          "AND [PlaneDeliveryService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice), 0) " +
                                          "FROM [VehicleDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].AccountingPaymentTaskID " +
                                          "WHERE [VehicleDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [VehicleDeliveryService].FromDate < @From " +
                                          "AND [VehicleDeliveryService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([MergedService].[AccountingGrossPrice]), 0) " +
                                          "FROM [MergedService] " +
                                          "WHERE [MergedService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [MergedService].FromDate < @From " +
                                          "AND [MergedService].Deleted = 0 " +
                                          "AND [MergedService].[AccountingActProvidingServiceID] IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([DeliveryExpense].AccountingGrossAmount), 0) " +
                                          "FROM [DeliveryExpense] " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                          "LEFT JOIN [ActProvidingService] " +
                                          "ON [ActProvidingService].ID = [DeliveryExpense].AccountingActProvidingServiceID " +
                                          "WHERE [DeliveryExpense].SupplyOrganizationAgreementID = @Id " +
                                          "AND [DeliveryExpense].FromDate < @From " +
                                          "AND [DeliveryExpense].Deleted = 0 " +
                                          "AND [DeliveryExpense].AccountingActProvidingServiceID IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0) " +
                                          "FROM [ConsumablesOrder] " +
                                          "LEFT JOIN [ConsumablesOrderItem] " +
                                          "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                                          "AND [ConsumablesOrderItem].Deleted = 0 " +
                                          "WHERE [ConsumablesOrderItem].SupplyOrganizationAgreementID = @Id " +
                                          "AND [ConsumablesOrder].OrganizationFromDate < @From " +
                                          "AND [ConsumablesOrder].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([IncomePaymentOrder].AgreementExchangedAmount), 0) " +
                                          //"SELECT 0 - ISNULL(SUM([IncomePaymentOrder].EuroAmount * [IncomePaymentOrder].AgreementEuroExchangeRate), 0)  " +
                                          "FROM [IncomePaymentOrder] " +
                                          "WHERE [IncomePaymentOrder].SupplyOrganizationAgreementID = @Id " +
                                          "AND [IncomePaymentOrder].FromDate < @From " +
                                          "AND [IncomePaymentOrder].Deleted = 0 " +
                                          "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                          "AND [IncomePaymentOrder].IsAccounting = 1 " +
                                          ") " +
                                          ", 2) AS [BeforeRangeInAmount]; ";
        else
            beforeRangeInAmountSqlQuery = "SELECT " +
                                          "ROUND( " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].NetPrice + [AccountingPaymentTask].NetPrice), 0) " +
                                          "FROM [ContainerService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [ContainerService].[SupplyPaymentTaskID] " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].[ID] = [ContainerService].[AccountingPaymentTaskID] " +
                                          "WHERE [ContainerService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [ContainerService].FromDate < @From " +
                                          "AND [ContainerService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].NetPrice + [AccountingPaymentTask].NetPrice), 0) " +
                                          "FROM [VehicleService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [VehicleService].[SupplyPaymentTaskID] " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].[ID] = [VehicleService].[AccountingPaymentTaskID] " +
                                          "WHERE [VehicleService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [VehicleService].FromDate < @From " +
                                          "AND [VehicleService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].NetPrice + [AccountingPaymentTask].NetPrice), 0) " +
                                          "FROM [BillOfLadingService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [BillOfLadingService].[SupplyPaymentTaskID] " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].[ID] = [BillOfLadingService].[AccountingPaymentTaskID] " +
                                          "WHERE [BillOfLadingService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [BillOfLadingService].FromDate < @From " +
                                          "AND [BillOfLadingService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].GrossPrice), 0) " +
                                          "FROM [CustomService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [CustomService].[SupplyPaymentTaskID] " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].[ID] = [CustomService].[AccountingPaymentTaskID] " +
                                          "WHERE [CustomService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [CustomService].FromDate < @From " +
                                          "AND [CustomService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].GrossPrice), 0) " +
                                          "FROM [PortWorkService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [PortWorkService].[SupplyPaymentTaskID] " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].[ID] = [PortWorkService].[AccountingPaymentTaskID] " +
                                          "WHERE [PortWorkService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [PortWorkService].FromDate < @From " +
                                          "AND [PortWorkService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].GrossPrice), 0) " +
                                          "FROM [TransportationService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [TransportationService].[SupplyPaymentTaskID] " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].[ID] = [TransportationService].[AccountingPaymentTaskID] " +
                                          "WHERE [TransportationService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [TransportationService].FromDate < @From " +
                                          "AND [TransportationService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].GrossPrice), 0) " +
                                          "FROM [PortCustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [PortCustomAgencyService].[SupplyPaymentTaskID] " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].[ID] = [PortCustomAgencyService].[AccountingPaymentTaskID] " +
                                          "WHERE [PortCustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [PortCustomAgencyService].FromDate < @From " +
                                          "AND [PortCustomAgencyService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].GrossPrice), 0) " +
                                          "FROM [CustomAgencyService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [CustomAgencyService].[SupplyPaymentTaskID] " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].[ID] = [CustomAgencyService].[AccountingPaymentTaskID] " +
                                          "WHERE [CustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [CustomAgencyService].FromDate < @From " +
                                          "AND [CustomAgencyService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].GrossPrice), 0) " +
                                          "FROM [PlaneDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [PlaneDeliveryService].[SupplyPaymentTaskID] " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].[ID] = [PlaneDeliveryService].[AccountingPaymentTaskID] " +
                                          "WHERE [PlaneDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [PlaneDeliveryService].FromDate < @From " +
                                          "AND [PlaneDeliveryService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([SupplyPaymentTask].GrossPrice + [AccountingPaymentTask].GrossPrice), 0) " +
                                          "FROM [VehicleDeliveryService] " +
                                          "LEFT JOIN [SupplyPaymentTask] " +
                                          "ON [SupplyPaymentTask].[ID] = [VehicleDeliveryService].[SupplyPaymentTaskID] " +
                                          "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
                                          "ON [AccountingPaymentTask].[ID] = [VehicleDeliveryService].[AccountingPaymentTaskID] " +
                                          "WHERE [VehicleDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [VehicleDeliveryService].FromDate < @From " +
                                          "AND [VehicleDeliveryService].Deleted = 0 " +
                                          "AND [SupplyPaymentTask].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([MergedService].[GrossPrice] + [MergedService].[AccountingGrossPrice]), 0) " +
                                          "FROM [MergedService] " +
                                          "WHERE [MergedService].SupplyOrganizationAgreementID = @Id " +
                                          "AND [MergedService].FromDate < @From " +
                                          "AND [MergedService].Deleted = 0 " +
                                          "AND ([MergedService].[ActProvidingServiceID] IS NOT NULL " +
                                          "OR [MergedService].[AccountingActProvidingServiceID] IS NOT NULL) " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([DeliveryExpense].GrossAmount), 0) " +
                                          "FROM [DeliveryExpense] " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                          "LEFT JOIN [ActProvidingService] " +
                                          "ON [ActProvidingService].ID = [DeliveryExpense].ActProvidingServiceID " +
                                          "WHERE [DeliveryExpense].SupplyOrganizationAgreementID = @Id " +
                                          "AND [DeliveryExpense].FromDate < @From " +
                                          "AND [DeliveryExpense].Deleted = 0 " +
                                          "AND [DeliveryExpense].ActProvidingServiceID IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([DeliveryExpense].AccountingGrossAmount), 0) " +
                                          "FROM [DeliveryExpense] " +
                                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                                          "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                          "LEFT JOIN [ActProvidingService] " +
                                          "ON [ActProvidingService].ID = [DeliveryExpense].AccountingActProvidingServiceID " +
                                          "WHERE [DeliveryExpense].SupplyOrganizationAgreementID = @Id " +
                                          "AND [DeliveryExpense].FromDate < @From " +
                                          "AND [DeliveryExpense].Deleted = 0 " +
                                          "AND [DeliveryExpense].AccountingActProvidingServiceID IS NOT NULL " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0) " +
                                          "FROM [ConsumablesOrder] " +
                                          "LEFT JOIN [ConsumablesOrderItem] " +
                                          "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                                          "AND [ConsumablesOrderItem].Deleted = 0 " +
                                          "WHERE [ConsumablesOrderItem].SupplyOrganizationAgreementID = @Id " +
                                          "AND [ConsumablesOrder].OrganizationFromDate < @From " +
                                          "AND [ConsumablesOrder].Deleted = 0 " +
                                          ") " +
                                          "+ " +
                                          "( " +
                                          "SELECT 0 - ISNULL(SUM([IncomePaymentOrder].AgreementExchangedAmount), 0) " +
                                          //"SELECT 0 - ISNULL(SUM([IncomePaymentOrder].EuroAmount * [IncomePaymentOrder].AgreementEuroExchangeRate), 0)  " +
                                          "FROM [IncomePaymentOrder] " +
                                          "WHERE [IncomePaymentOrder].SupplyOrganizationAgreementID = @Id " +
                                          "AND [IncomePaymentOrder].FromDate < @From " +
                                          "AND [IncomePaymentOrder].Deleted = 0 " +
                                          "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                          ") " +
                                          ", 2) AS [BeforeRangeInAmount]; ";

        AccountingCashFlow accountingCashFlow = new(preDefinedAgreement) {
            BeforeRangeInAmount =
                _connection.Query<decimal>(
                    beforeRangeInAmountSqlQuery,
                    new { preDefinedAgreement.Id, From = from }
                ).Single(),
            BeforeRangeOutAmount =
                _connection.Query<decimal>(
                    "SELECT " +
                    "ROUND( " +
                    "( " +
                    "SELECT " +
                    "ISNULL( " +
                    "SUM([OutcomePaymentOrder].AfterExchangeAmount) " +
                    ", 0) " +
                    "FROM [OutcomePaymentOrder] " +
                    "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                    "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
                    "LEFT JOIN [SupplyPaymentTask] " +
                    "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
                    "WHERE [OutcomePaymentOrder].Deleted = 0 " +
                    "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                    "AND [OutcomePaymentOrder].SupplyOrganizationAgreementID = @Id " +
                    "AND [OutcomePaymentOrder].FromDate < @From " +
                    (
                        !typePaymentTask.Equals(TypePaymentTask.All)
                            ? "AND ([SupplyPaymentTask].ID IS NOT NULL AND [SupplyPaymentTask].[IsAccounting] = @IsAccounting " +
                              "OR [SupplyPaymentTask].ID IS NULL AND [OutcomePaymentOrder].[IsAccounting] = @IsAccounting) "
                            : ""
                    ) +
                    ") " +
                    ", 2) AS [BeforeRangeOutAmount]",
                    new { preDefinedAgreement.Id, From = from, IsAccounting = typePaymentTask }
                ).Single()
        };


        accountingCashFlow.BeforeRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.BeforeRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        accountingCashFlow.BeforeRangeBalance = Math.Round(accountingCashFlow.BeforeRangeInAmount - accountingCashFlow.BeforeRangeOutAmount, 2);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.AfterRangeInAmount);

        accountingCashFlow.AfterRangeInAmount = Math.Abs(accountingCashFlow.BeforeRangeInAmount);
        accountingCashFlow.AfterRangeOutAmount = Math.Abs(accountingCashFlow.BeforeRangeOutAmount);

        decimal currentStepBalance = accountingCashFlow.BeforeRangeBalance;

        List<JoinService> joinServices = new();

        string joinServicesqlQuery;

        if (typePaymentTask.Equals(TypePaymentTask.PaymentTask))
            joinServicesqlQuery = ";WITH [AccountingCashFlow_CTE] " +
                                  "AS " +
                                  "( " +
                                  "SELECT [ContainerService].ID " +
                                  ", 2 AS [Type] " +
                                  ", [ContainerService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].NetPrice, 0) AS [GrossPrice] " +
                                  "FROM [ContainerService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ContainerService].SupplyPaymentTaskID " +
                                  "WHERE [ContainerService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ContainerService].Deleted = 0 " +
                                  "AND [ContainerService].FromDate >= @From " +
                                  "AND [ContainerService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortWorkService].ID " +
                                  ", 4 AS [Type] " +
                                  ", [PortWorkService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PortWorkService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
                                  "WHERE [PortWorkService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortWorkService].Deleted = 0 " +
                                  "AND [PortWorkService].FromDate >= @From " +
                                  "AND [PortWorkService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleService].ID " +
                                  ", 21 AS [Type] " +
                                  ", [VehicleService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].NetPrice, 0) AS [GrossPrice] " +
                                  "FROM [VehicleService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleService].SupplyPaymentTaskID " +
                                  "WHERE [VehicleService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleService].Deleted = 0 " +
                                  "AND [VehicleService].FromDate >= @From " +
                                  "AND [VehicleService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [BillOfLadingService].ID " +
                                  ", 33 AS [Type] " +
                                  ", [BillOfLadingService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].NetPrice, 0) AS [GrossPrice] " +
                                  "FROM [BillOfLadingService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [BillOfLadingService].SupplyPaymentTaskID " +
                                  "WHERE [BillOfLadingService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [BillOfLadingService].Deleted = 0 " +
                                  "AND [BillOfLadingService].FromDate >= @From " +
                                  "AND [BillOfLadingService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomService].ID " +
                                  ", 3 AS [Type] " +
                                  ", [CustomService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [CustomService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
                                  "WHERE [CustomService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomService].Deleted = 0 " +
                                  "AND [CustomService].FromDate >= @From " +
                                  "AND [CustomService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [TransportationService].ID " +
                                  ", 5 AS [Type] " +
                                  ", [TransportationService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [TransportationService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
                                  "WHERE [TransportationService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [TransportationService].Deleted = 0 " +
                                  "AND [TransportationService].FromDate >= @From " +
                                  "AND [TransportationService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortCustomAgencyService].ID " +
                                  ", 6 AS [Type] " +
                                  ", [PortCustomAgencyService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PortCustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
                                  "WHERE [PortCustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].FromDate >= @From " +
                                  "AND [PortCustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomAgencyService].ID " +
                                  ", 7 AS [Type] " +
                                  ", [CustomAgencyService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [CustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
                                  "WHERE [CustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomAgencyService].Deleted = 0 " +
                                  "AND [CustomAgencyService].FromDate >= @From " +
                                  "AND [CustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PlaneDeliveryService].ID " +
                                  ", 8 AS [Type] " +
                                  ", [PlaneDeliveryService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PlaneDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
                                  "WHERE [PlaneDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].FromDate >= @From " +
                                  "AND [PlaneDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleDeliveryService].ID " +
                                  ", 9 AS [Type] " +
                                  ", [VehicleDeliveryService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [VehicleDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
                                  "WHERE [VehicleDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].FromDate >= @From " +
                                  "AND [VehicleDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [MergedService].ID " +
                                  ", 17 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", ISNULL([MergedService].[GrossPrice], 0) AS [GrossPrice] " +
                                  "FROM [MergedService] " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].[ID] = [MergedService].[ActProvidingServiceID] " +
                                  "WHERE [MergedService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [MergedService].Deleted = 0 " +
                                  "AND [MergedService].FromDate >= @From " +
                                  "AND [MergedService].FromDate <= @To " +
                                  "AND [MergedService].[ActProvidingServiceID] IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [DeliveryExpense].ID " +
                                  ", 39 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", ISNULL([DeliveryExpense].[GrossAmount], 0) AS [GrossPrice] " +
                                  "FROM [DeliveryExpense] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].ID = [DeliveryExpense].ActProvidingServiceID " +
                                  "WHERE [DeliveryExpense].SupplyOrganizationAgreementID = @Id " +
                                  "AND [DeliveryExpense].Deleted = 0 " +
                                  "AND [DeliveryExpense].FromDate >= @From " +
                                  "AND [DeliveryExpense].FromDate <= @To " +
                                  "AND [DeliveryExpense].ActProvidingServiceID IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [ConsumablesOrder].ID " +
                                  ", 10 AS [Type] " +
                                  ", [ConsumablesOrder].OrganizationFromDate AS [FromDate] " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [ConsumablesOrderItem] " +
                                  "LEFT JOIN [ConsumablesOrder] " +
                                  "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
                                  "WHERE [ConsumablesOrderItem].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ConsumablesOrderItem].Deleted = 0 " +
                                  "AND [ConsumablesOrder].OrganizationFromDate >= @From " +
                                  "AND [ConsumablesOrder].OrganizationFromDate <= @To " +
                                  "UNION " +
                                  "SELECT [IncomePaymentOrder].ID " +
                                  ", 12 AS [Type] " +
                                  ", [IncomePaymentOrder].FromDate " +
                                  ", ISNULL([IncomePaymentOrder].AgreementExchangedAmount, 0) AS [GrossPrice] " +
                                  //", ISNULL([IncomePaymentOrder].EuroAmount * [IncomePaymentOrder].AgreementEuroExchangeRate, 0) AS [GrossPrice] " +
                                  "FROM [IncomePaymentOrder] " +
                                  "WHERE [IncomePaymentOrder].SupplyOrganizationAgreementID = @Id " +
                                  "AND [IncomePaymentOrder].Deleted = 0 " +
                                  "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                  "AND [IncomePaymentOrder].IsManagementAccounting = 1 " +
                                  "AND [IncomePaymentOrder].FromDate >= @From " +
                                  "AND [IncomePaymentOrder].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [OutcomePaymentOrder].ID AS [ID] " +
                                  ", 11 AS [Type] " +
                                  ", [OutcomePaymentOrder].FromDate " +
                                  ", [OutcomePaymentOrder].AfterExchangeAmount AS [GrossPrice] " +
                                  "FROM [OutcomePaymentOrder] " +
                                  "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                                  "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
                                  "WHERE [OutcomePaymentOrder].SupplyOrganizationAgreementID = @Id " +
                                  "AND [OutcomePaymentOrder].Deleted = 0 " +
                                  "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                                  "AND [OutcomePaymentOrder].FromDate >= @From " +
                                  "AND [OutcomePaymentOrder].FromDate <= @To " +
                                  "AND ([SupplyPaymentTask].ID IS NOT NULL AND [SupplyPaymentTask].[IsAccounting] = 0 " +
                                  "OR [SupplyPaymentTask].ID IS NULL AND [OutcomePaymentOrder].[IsManagementAccounting] = 1) " +
                                  ") " +
                                  "SELECT * " +
                                  "FROM [AccountingCashFlow_CTE] " +
                                  "ORDER BY [AccountingCashFlow_CTE].FromDate";
        else if (typePaymentTask.Equals(TypePaymentTask.AccountingPaymentTask))
            joinServicesqlQuery = ";WITH [AccountingCashFlow_CTE] " +
                                  "AS " +
                                  "( " +
                                  "SELECT [ContainerService].ID " +
                                  ", 31 AS [Type] " +
                                  ", [ContainerService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [ContainerService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ContainerService].AccountingPaymentTaskID " +
                                  "WHERE [ContainerService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ContainerService].Deleted = 0 " +
                                  "AND [ContainerService].FromDate >= @From " +
                                  "AND [ContainerService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortWorkService].ID " +
                                  ", 32 AS [Type] " +
                                  ", [PortWorkService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PortWorkService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortWorkService].AccountingPaymentTaskID " +
                                  "WHERE [PortWorkService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortWorkService].Deleted = 0 " +
                                  "AND [PortWorkService].FromDate >= @From " +
                                  "AND [PortWorkService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleService].ID " +
                                  ", 23 AS [Type] " +
                                  ", [VehicleService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [VehicleService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleService].AccountingPaymentTaskID " +
                                  "WHERE [VehicleService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleService].Deleted = 0 " +
                                  "AND [VehicleService].FromDate >= @From " +
                                  "AND [VehicleService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [BillOfLadingService].ID " +
                                  ", 34 AS [Type] " +
                                  ", [BillOfLadingService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [BillOfLadingService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [BillOfLadingService].AccountingPaymentTaskID " +
                                  "WHERE [BillOfLadingService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [BillOfLadingService].Deleted = 0 " +
                                  "AND [BillOfLadingService].FromDate >= @From " +
                                  "AND [BillOfLadingService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomService].ID " +
                                  ", 24 AS [Type] " +
                                  ", [CustomService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [CustomService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomService].AccountingPaymentTaskID " +
                                  "WHERE [CustomService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomService].Deleted = 0 " +
                                  "AND [CustomService].FromDate >= @From " +
                                  "AND [CustomService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [TransportationService].ID " +
                                  ", 25 AS [Type] " +
                                  ", [TransportationService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [TransportationService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [TransportationService].AccountingPaymentTaskID " +
                                  "WHERE [TransportationService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [TransportationService].Deleted = 0 " +
                                  "AND [TransportationService].FromDate >= @From " +
                                  "AND [TransportationService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortCustomAgencyService].ID " +
                                  ", 26 AS [Type] " +
                                  ", [PortCustomAgencyService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PortCustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].AccountingPaymentTaskID " +
                                  "WHERE [PortCustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].FromDate >= @From " +
                                  "AND [PortCustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomAgencyService].ID " +
                                  ", 27 AS [Type] " +
                                  ", [CustomAgencyService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [CustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomAgencyService].AccountingPaymentTaskID " +
                                  "WHERE [CustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomAgencyService].Deleted = 0 " +
                                  "AND [CustomAgencyService].FromDate >= @From " +
                                  "AND [CustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PlaneDeliveryService].ID " +
                                  ", 28 AS [Type] " +
                                  ", [PlaneDeliveryService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PlaneDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].AccountingPaymentTaskID " +
                                  "WHERE [PlaneDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].FromDate >= @From " +
                                  "AND [PlaneDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleDeliveryService].ID " +
                                  ", 29 AS [Type] " +
                                  ", [VehicleDeliveryService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [VehicleDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].AccountingPaymentTaskID " +
                                  "WHERE [VehicleDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].FromDate >= @From " +
                                  "AND [VehicleDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [MergedService].ID " +
                                  ", 30 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", ISNULL([MergedService].[AccountingGrossPrice], 0) AS [GrossPrice] " +
                                  "FROM [MergedService] " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].[ID] = [MergedService].[ActProvidingServiceID] " +
                                  "WHERE [MergedService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [MergedService].Deleted = 0 " +
                                  "AND [MergedService].FromDate >= @From " +
                                  "AND [MergedService].FromDate <= @To " +
                                  "AND [MergedService].[ActProvidingServiceID] IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [DeliveryExpense].ID " +
                                  ", 40 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", ISNULL([DeliveryExpense].[AccountingGrossAmount], 0) AS [GrossPrice] " +
                                  "FROM [DeliveryExpense] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].ID = [DeliveryExpense].AccountingActProvidingServiceID " +
                                  "WHERE [DeliveryExpense].SupplyOrganizationAgreementID = @Id " +
                                  "AND [DeliveryExpense].Deleted = 0 " +
                                  "AND [DeliveryExpense].FromDate >= @From " +
                                  "AND [DeliveryExpense].FromDate <= @To " +
                                  "AND [DeliveryExpense].AccountingActProvidingServiceID IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [ConsumablesOrder].ID " +
                                  ", 10 AS [Type] " +
                                  ", [ConsumablesOrder].OrganizationFromDate AS [FromDate] " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [ConsumablesOrderItem] " +
                                  "LEFT JOIN [ConsumablesOrder] " +
                                  "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
                                  "WHERE [ConsumablesOrderItem].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ConsumablesOrderItem].Deleted = 0 " +
                                  "AND [ConsumablesOrder].OrganizationFromDate >= @From " +
                                  "AND [ConsumablesOrder].OrganizationFromDate <= @To " +
                                  "UNION " +
                                  "SELECT [IncomePaymentOrder].ID " +
                                  ", 12 AS [Type] " +
                                  ", [IncomePaymentOrder].FromDate " +
                                  ", ISNULL([IncomePaymentOrder].AgreementExchangedAmount, 0) AS [GrossPrice] " +
                                  //", ISNULL([IncomePaymentOrder].EuroAmount * [IncomePaymentOrder].AgreementEuroExchangeRate, 0) AS [GrossPrice] " +
                                  "FROM [IncomePaymentOrder] " +
                                  "WHERE [IncomePaymentOrder].SupplyOrganizationAgreementID = @Id " +
                                  "AND [IncomePaymentOrder].Deleted = 0 " +
                                  "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                  "AND [IncomePaymentOrder].IsAccounting = 1 " +
                                  "AND [IncomePaymentOrder].FromDate >= @From " +
                                  "AND [IncomePaymentOrder].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [OutcomePaymentOrder].ID AS [ID] " +
                                  ", 11 AS [Type] " +
                                  ", [OutcomePaymentOrder].FromDate " +
                                  ", [OutcomePaymentOrder].AfterExchangeAmount AS [GrossPrice] " +
                                  "FROM [OutcomePaymentOrder] " +
                                  "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                                  "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
                                  "WHERE [OutcomePaymentOrder].SupplyOrganizationAgreementID = @Id " +
                                  "AND [OutcomePaymentOrder].Deleted = 0 " +
                                  "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                                  "AND [OutcomePaymentOrder].FromDate >= @From " +
                                  "AND [OutcomePaymentOrder].FromDate <= @To " +
                                  "AND ([SupplyPaymentTask].ID IS NOT NULL AND [SupplyPaymentTask].[IsAccounting] = 1 " +
                                  "OR [SupplyPaymentTask].ID IS NULL AND [OutcomePaymentOrder].[IsAccounting] = 1) " +
                                  ") " +
                                  "SELECT * " +
                                  "FROM [AccountingCashFlow_CTE] " +
                                  "ORDER BY [AccountingCashFlow_CTE].FromDate";
        else
            joinServicesqlQuery = ";WITH [AccountingCashFlow_CTE] " +
                                  "AS " +
                                  "( " +
                                  "SELECT [ContainerService].ID " +
                                  ", 2 AS [Type] " +
                                  ", [ContainerService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].NetPrice, 0) AS [GrossPrice] " +
                                  "FROM [ContainerService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ContainerService].SupplyPaymentTaskID " +
                                  "WHERE [ContainerService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ContainerService].Deleted = 0 " +
                                  "AND [ContainerService].FromDate >= @From " +
                                  "AND [ContainerService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortWorkService].ID " +
                                  ", 4 AS [Type] " +
                                  ", [PortWorkService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PortWorkService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
                                  "WHERE [PortWorkService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortWorkService].Deleted = 0 " +
                                  "AND [PortWorkService].FromDate >= @From " +
                                  "AND [PortWorkService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleService].ID " +
                                  ", 21 AS [Type] " +
                                  ", [VehicleService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].NetPrice, 0) AS [GrossPrice] " +
                                  "FROM [VehicleService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleService].SupplyPaymentTaskID " +
                                  "WHERE [VehicleService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleService].Deleted = 0 " +
                                  "AND [VehicleService].FromDate >= @From " +
                                  "AND [VehicleService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [BillOfLadingService].ID " +
                                  ", 33 AS [Type] " +
                                  ", [BillOfLadingService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].NetPrice, 0) AS [GrossPrice] " +
                                  "FROM [BillOfLadingService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [BillOfLadingService].SupplyPaymentTaskID " +
                                  "WHERE [BillOfLadingService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [BillOfLadingService].Deleted = 0 " +
                                  "AND [BillOfLadingService].FromDate >= @From " +
                                  "AND [BillOfLadingService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomService].ID " +
                                  ", 3 AS [Type] " +
                                  ", [CustomService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [CustomService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
                                  "WHERE [CustomService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomService].Deleted = 0 " +
                                  "AND [CustomService].FromDate >= @From " +
                                  "AND [CustomService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [TransportationService].ID " +
                                  ", 5 AS [Type] " +
                                  ", [TransportationService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [TransportationService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
                                  "WHERE [TransportationService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [TransportationService].Deleted = 0 " +
                                  "AND [TransportationService].FromDate >= @From " +
                                  "AND [TransportationService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortCustomAgencyService].ID " +
                                  ", 6 AS [Type] " +
                                  ", [PortCustomAgencyService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PortCustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
                                  "WHERE [PortCustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].FromDate >= @From " +
                                  "AND [PortCustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomAgencyService].ID " +
                                  ", 7 AS [Type] " +
                                  ", [CustomAgencyService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [CustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
                                  "WHERE [CustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomAgencyService].Deleted = 0 " +
                                  "AND [CustomAgencyService].FromDate >= @From " +
                                  "AND [CustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PlaneDeliveryService].ID " +
                                  ", 8 AS [Type] " +
                                  ", [PlaneDeliveryService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PlaneDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
                                  "WHERE [PlaneDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].FromDate >= @From " +
                                  "AND [PlaneDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleDeliveryService].ID " +
                                  ", 9 AS [Type] " +
                                  ", [VehicleDeliveryService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [VehicleDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
                                  "WHERE [VehicleDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].FromDate >= @From " +
                                  "AND [VehicleDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [MergedService].ID " +
                                  ", 17 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", ISNULL([MergedService].[GrossPrice], 0) AS [GrossPrice] " +
                                  "FROM [MergedService] " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].[ID] = [MergedService].[ActProvidingServiceID] " +
                                  "WHERE [MergedService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [MergedService].Deleted = 0 " +
                                  "AND [MergedService].FromDate >= @From " +
                                  "AND [MergedService].FromDate <= @To " +
                                  "AND [MergedService].[ActProvidingServiceID] IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [DeliveryExpense].ID " +
                                  ", 39 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", ISNULL([DeliveryExpense].[GrossAmount], 0) AS [GrossPrice] " +
                                  "FROM [DeliveryExpense] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].ID = [DeliveryExpense].ActProvidingServiceID " +
                                  "WHERE [DeliveryExpense].SupplyOrganizationAgreementID = @Id " +
                                  "AND [DeliveryExpense].Deleted = 0 " +
                                  "AND [DeliveryExpense].FromDate >= @From " +
                                  "AND [DeliveryExpense].FromDate <= @To " +
                                  "AND [DeliveryExpense].ActProvidingServiceID IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [DeliveryExpense].ID " +
                                  ", 40 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", ISNULL([DeliveryExpense].[AccountingGrossAmount], 0) AS [GrossPrice] " +
                                  "FROM [DeliveryExpense] " +
                                  "LEFT JOIN [SupplyOrganizationAgreement] " +
                                  "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].ID = [DeliveryExpense].AccountingActProvidingServiceID " +
                                  "WHERE [DeliveryExpense].SupplyOrganizationAgreementID = @Id " +
                                  "AND [DeliveryExpense].Deleted = 0 " +
                                  "AND [DeliveryExpense].FromDate >= @From " +
                                  "AND [DeliveryExpense].FromDate <= @To " +
                                  "AND [DeliveryExpense].AccountingActProvidingServiceID IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [ContainerService].ID " +
                                  ", 31 AS [Type] " +
                                  ", [ContainerService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [ContainerService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ContainerService].AccountingPaymentTaskID " +
                                  "WHERE [ContainerService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ContainerService].Deleted = 0 " +
                                  "AND [ContainerService].FromDate >= @From " +
                                  "AND [ContainerService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortWorkService].ID " +
                                  ", 32 AS [Type] " +
                                  ", [PortWorkService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PortWorkService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortWorkService].AccountingPaymentTaskID " +
                                  "WHERE [PortWorkService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortWorkService].Deleted = 0 " +
                                  "AND [PortWorkService].FromDate >= @From " +
                                  "AND [PortWorkService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleService].ID " +
                                  ", 23 AS [Type] " +
                                  ", [VehicleService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [VehicleService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleService].AccountingPaymentTaskID " +
                                  "WHERE [VehicleService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleService].Deleted = 0 " +
                                  "AND [VehicleService].FromDate >= @From " +
                                  "AND [VehicleService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [BillOfLadingService].ID " +
                                  ", 34 AS [Type] " +
                                  ", [BillOfLadingService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [BillOfLadingService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [BillOfLadingService].AccountingPaymentTaskID " +
                                  "WHERE [BillOfLadingService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [BillOfLadingService].Deleted = 0 " +
                                  "AND [BillOfLadingService].FromDate >= @From " +
                                  "AND [BillOfLadingService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomService].ID " +
                                  ", 24 AS [Type] " +
                                  ", [CustomService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [CustomService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomService].AccountingPaymentTaskID " +
                                  "WHERE [CustomService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomService].Deleted = 0 " +
                                  "AND [CustomService].FromDate >= @From " +
                                  "AND [CustomService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [TransportationService].ID " +
                                  ", 25 AS [Type] " +
                                  ", [TransportationService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [TransportationService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [TransportationService].AccountingPaymentTaskID " +
                                  "WHERE [TransportationService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [TransportationService].Deleted = 0 " +
                                  "AND [TransportationService].FromDate >= @From " +
                                  "AND [TransportationService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PortCustomAgencyService].ID " +
                                  ", 26 AS [Type] " +
                                  ", [PortCustomAgencyService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PortCustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].AccountingPaymentTaskID " +
                                  "WHERE [PortCustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].Deleted = 0 " +
                                  "AND [PortCustomAgencyService].FromDate >= @From " +
                                  "AND [PortCustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [CustomAgencyService].ID " +
                                  ", 27 AS [Type] " +
                                  ", [CustomAgencyService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [CustomAgencyService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [CustomAgencyService].AccountingPaymentTaskID " +
                                  "WHERE [CustomAgencyService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [CustomAgencyService].Deleted = 0 " +
                                  "AND [CustomAgencyService].FromDate >= @From " +
                                  "AND [CustomAgencyService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [PlaneDeliveryService].ID " +
                                  ", 28 AS [Type] " +
                                  ", [PlaneDeliveryService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [PlaneDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].AccountingPaymentTaskID " +
                                  "WHERE [PlaneDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].Deleted = 0 " +
                                  "AND [PlaneDeliveryService].FromDate >= @From " +
                                  "AND [PlaneDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [VehicleDeliveryService].ID " +
                                  ", 29 AS [Type] " +
                                  ", [VehicleDeliveryService].FromDate " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [VehicleDeliveryService] " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].AccountingPaymentTaskID " +
                                  "WHERE [VehicleDeliveryService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].Deleted = 0 " +
                                  "AND [VehicleDeliveryService].FromDate >= @From " +
                                  "AND [VehicleDeliveryService].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [MergedService].ID " +
                                  ", 30 AS [Type] " +
                                  ", [ActProvidingService].FromDate " +
                                  ", ISNULL([MergedService].[AccountingGrossPrice], 0) AS [GrossPrice] " +
                                  "FROM [MergedService] " +
                                  "LEFT JOIN [ActProvidingService] " +
                                  "ON [ActProvidingService].[ID] = [MergedService].[AccountingActProvidingServiceID] " +
                                  "WHERE [MergedService].SupplyOrganizationAgreementID = @Id " +
                                  "AND [MergedService].Deleted = 0 " +
                                  "AND [MergedService].FromDate >= @From " +
                                  "AND [MergedService].FromDate <= @To " +
                                  "AND [MergedService].[AccountingActProvidingServiceID] IS NOT NULL " +
                                  "UNION " +
                                  "SELECT [ConsumablesOrder].ID " +
                                  ", 10 AS [Type] " +
                                  ", [ConsumablesOrder].OrganizationFromDate AS [FromDate] " +
                                  ", ISNULL([SupplyPaymentTask].GrossPrice, 0) AS [GrossPrice] " +
                                  "FROM [ConsumablesOrderItem] " +
                                  "LEFT JOIN [ConsumablesOrder] " +
                                  "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                                  "LEFT JOIN [SupplyPaymentTask] " +
                                  "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
                                  "WHERE [ConsumablesOrderItem].SupplyOrganizationAgreementID = @Id " +
                                  "AND [SupplyPaymentTask].Deleted = 0 " +
                                  "AND [ConsumablesOrderItem].Deleted = 0 " +
                                  "AND [ConsumablesOrder].OrganizationFromDate >= @From " +
                                  "AND [ConsumablesOrder].OrganizationFromDate <= @To " +
                                  "UNION " +
                                  "SELECT [IncomePaymentOrder].ID " +
                                  ", 12 AS [Type] " +
                                  ", [IncomePaymentOrder].FromDate " +
                                  ", ISNULL([IncomePaymentOrder].AgreementExchangedAmount, 0) AS [GrossPrice] " +
                                  //", ISNULL([IncomePaymentOrder].EuroAmount * [IncomePaymentOrder].AgreementEuroExchangeRate, 0) AS [GrossPrice] " +
                                  "FROM [IncomePaymentOrder] " +
                                  "WHERE [IncomePaymentOrder].SupplyOrganizationAgreementID = @Id " +
                                  "AND [IncomePaymentOrder].Deleted = 0 " +
                                  "AND [IncomePaymentOrder].IsCanceled = 0 " +
                                  "AND [IncomePaymentOrder].FromDate >= @From " +
                                  "AND [IncomePaymentOrder].FromDate <= @To " +
                                  "UNION " +
                                  "SELECT [OutcomePaymentOrder].ID AS [ID] " +
                                  ", 11 AS [Type] " +
                                  ", [OutcomePaymentOrder].FromDate " +
                                  ", [OutcomePaymentOrder].AfterExchangeAmount AS [GrossPrice] " +
                                  "FROM [OutcomePaymentOrder] " +
                                  "WHERE [OutcomePaymentOrder].SupplyOrganizationAgreementID = @Id " +
                                  "AND [OutcomePaymentOrder].Deleted = 0 " +
                                  "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                                  "AND [OutcomePaymentOrder].FromDate >= @From " +
                                  "AND [OutcomePaymentOrder].FromDate <= @To " +
                                  ") " +
                                  "SELECT * " +
                                  "FROM [AccountingCashFlow_CTE] " +
                                  "ORDER BY [AccountingCashFlow_CTE].FromDate";

        _connection.Query<JoinService, decimal, JoinService>(
            joinServicesqlQuery,
            (service, grossPrice) => {
                if (joinServices.Any(s => s.Type.Equals(service.Type) && s.Id.Equals(service.Id))) return service;

                switch (service.Type) {
                    case JoinServiceType.ContainerService:
                    case JoinServiceType.CustomService:
                    case JoinServiceType.PortWorkService:
                    case JoinServiceType.TransportationService:
                    case JoinServiceType.PortCustomAgencyService:
                    case JoinServiceType.CustomAgencyService:
                    case JoinServiceType.PlaneDeliveryService:
                    case JoinServiceType.VehicleDeliveryService:
                    case JoinServiceType.SupplyPaymentTask:
                    case JoinServiceType.ConsumablesOrder:
                    case JoinServiceType.MergedService:
                    case JoinServiceType.VehicleService:
                    case JoinServiceType.AccountingContainerPaymentTask:
                    case JoinServiceType.AccountingVehicleService:
                    case JoinServiceType.AccountingCustomService:
                    case JoinServiceType.AccountingTransportationService:
                    case JoinServiceType.AccountingPortCustomAgencyService:
                    case JoinServiceType.AccountingCustomAgencyService:
                    case JoinServiceType.AccountingPlaneDeliveryService:
                    case JoinServiceType.AccountingVehicleDeliveryService:
                    case JoinServiceType.AccountingMergedService:
                    case JoinServiceType.AccountingContainerService:
                    case JoinServiceType.AccountingPortWorkService:
                    case JoinServiceType.BillOfLadingService:
                    case JoinServiceType.DeliveryExpense:
                    case JoinServiceType.AccountingDeliveryExpense:
                    case JoinServiceType.AccountingBillOfLadingService:
                        currentStepBalance = Math.Round(currentStepBalance + grossPrice, 2);

                        accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + grossPrice, 2);

                        accountingCashFlow.AccountingCashFlowHeadItems.Add(
                            new AccountingCashFlowHeadItem {
                                CurrentBalance = currentStepBalance,
                                FromDate = service.FromDate,
                                Type = service.Type,
                                IsCreditValue = false,
                                CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                Id = service.Id
                            }
                        );
                        break;
                    case JoinServiceType.OutcomePaymentOrder:
                        currentStepBalance = Math.Round(currentStepBalance - grossPrice, 2);

                        accountingCashFlow.AfterRangeOutAmount = Math.Round(accountingCashFlow.AfterRangeOutAmount + grossPrice, 2);

                        accountingCashFlow.AccountingCashFlowHeadItems.Add(
                            new AccountingCashFlowHeadItem {
                                CurrentBalance = currentStepBalance,
                                FromDate = service.FromDate,
                                Type = service.Type,
                                IsCreditValue = true,
                                CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                Id = service.Id
                            }
                        );
                        break;
                    case JoinServiceType.IncomePaymentOrder:
                        currentStepBalance = Math.Round(currentStepBalance + grossPrice, 2);

                        accountingCashFlow.AfterRangeInAmount = Math.Round(accountingCashFlow.AfterRangeInAmount + grossPrice, 2);

                        accountingCashFlow.AccountingCashFlowHeadItems.Add(
                            new AccountingCashFlowHeadItem {
                                CurrentBalance = currentStepBalance,
                                FromDate = service.FromDate,
                                Type = service.Type,
                                CurrentValue = decimal.Round(grossPrice, 2, MidpointRounding.AwayFromZero),
                                Id = service.Id
                            }
                        );
                        break;
                    case JoinServiceType.SupplyOrderPaymentDeliveryProtocol:
                    case JoinServiceType.SupplyOrderPolandPaymentDeliveryProtocol:
                    default:
                        break;
                }

                joinServices.Add(service);

                return service;
            },
            new { preDefinedAgreement.Id, From = from, To = to },
            splitOn: "ID,GrossPrice"
        );

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ContainerService))) {
            Type[] joinTypes = {
                typeof(ContainerService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderContainerService),
                typeof(SupplyOrder),
                typeof(User)
            };

            Func<object[], ContainerService> joinMapper = objects => {
                ContainerService containerService = (ContainerService)objects[0];
                SupplyOrganization containerOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                SupplyOrderContainerService junction = (SupplyOrderContainerService)objects[7];
                SupplyOrder supplyOrder = (SupplyOrder)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.ContainerService) && i.Id.Equals(containerService.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.ContainerService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.ContainerService.ContainerNumber))
                        itemFromList.Number = itemFromList.ContainerService.ContainerNumber;

                    if (invoiceDocument != null && !itemFromList.ContainerService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.ContainerService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction == null || itemFromList.ContainerService.SupplyOrderContainerServices.Any(j => j.Id.Equals(junction.Id)))
                        return containerService;

                    junction.SupplyOrder = supplyOrder;

                    itemFromList.ContainerService.SupplyOrderContainerServices.Add(junction);
                } else {
                    if (containerService != null && !string.IsNullOrEmpty(containerService.ContainerNumber)) {
                        itemFromList.Number = containerService.ContainerNumber;

                        if (invoiceDocument != null) containerService.InvoiceDocuments.Add(invoiceDocument);

                        if (junction != null) {
                            junction.SupplyOrder = supplyOrder;

                            containerService.SupplyOrderContainerServices.Add(junction);
                        }

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        containerService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        containerService.ContainerOrganization = containerOrganization;
                        containerService.BillOfLadingDocument = billOfLadingDocument;
                        containerService.User = user;
                    }

                    itemFromList.ContainerService = containerService;
                }

                return containerService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ContainerService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrderContainerService] " +
                "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ContainerService].UserID " +
                "WHERE [ContainerService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ContainerService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.CustomService))) {
            Type[] joinTypes = {
                typeof(CustomService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], CustomService> joinMapper = objects => {
                CustomService customService = (CustomService)objects[0];
                SupplyOrganization customOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement customOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization customOrganizationOrganization = (Organization)objects[3];
                SupplyOrganization exciseDutyOrganization = (SupplyOrganization)objects[4];
                SupplyOrganizationAgreement exciseDutyOrganizationAgreement = (SupplyOrganizationAgreement)objects[5];
                Organization exciseDutyOrganizationOrganization = (Organization)objects[6];
                SupplyOrder supplyOrder = (SupplyOrder)objects[7];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[8];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[9];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[10];
                User user = (User)objects[11];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[12];
                Currency currency = (Currency)objects[13];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.CustomService) && i.Id.Equals(customService.Id));

                itemFromList.OrganizationName = customOrganizationOrganization?.Name ?? "";

                if (itemFromList.CustomService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.CustomService.ServiceNumber))
                        itemFromList.Number = itemFromList.CustomService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.CustomService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.CustomService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem == null || itemFromList.CustomService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id)))
                        return customService;

                    serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                    itemFromList.CustomService.ServiceDetailItems.Add(serviceDetailItem);
                } else {
                    if (customService != null && !string.IsNullOrEmpty(customService.ServiceNumber)) {
                        itemFromList.Number = customService.ServiceNumber;

                        if (invoiceDocument != null) customService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            customService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (customOrganization != null && customOrganizationAgreement != null) {
                            if (!customOrganization.SupplyOrganizationAgreements.Any(x => x.Id == customOrganizationAgreement.Id))
                                customOrganization.SupplyOrganizationAgreements.Add(customOrganizationAgreement);
                            else
                                customOrganizationAgreement = customOrganization.SupplyOrganizationAgreements.First(x => x.Id == customOrganizationAgreement.Id);

                            customOrganizationAgreement.Organization = customOrganizationOrganization;
                        }

                        if (exciseDutyOrganization != null && exciseDutyOrganizationAgreement != null) {
                            if (!exciseDutyOrganization.SupplyOrganizationAgreements.Any(x => x.Id == exciseDutyOrganizationAgreement.Id))
                                exciseDutyOrganization.SupplyOrganizationAgreements.Add(exciseDutyOrganizationAgreement);
                            else
                                exciseDutyOrganizationAgreement = exciseDutyOrganization.SupplyOrganizationAgreements.First(x => x.Id == exciseDutyOrganizationAgreement.Id);

                            exciseDutyOrganizationAgreement.Organization = exciseDutyOrganizationOrganization;
                        }

                        if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = currency;

                        customService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        customService.CustomOrganization = customOrganization;
                        customService.ExciseDutyOrganization = exciseDutyOrganization;
                        customService.SupplyOrder = supplyOrder;
                        customService.User = user;
                    }

                    itemFromList.CustomService = customService;
                }

                return customService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [CustomService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [CustomService].CustomOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].[SupplyOrganizationID] = [SupplyOrganization].[ID] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
                "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ExciseDutyOrganizationAgreement] " +
                "ON [ExciseDutyOrganizationAgreement].[SupplyOrganizationID] = [ExciseDutyOrganization].[ID] " +
                "LEFT JOIN [Organization] AS [ExciseDutyOrganizationOrganization] " +
                "ON [ExciseDutyOrganizationOrganization].ID = [ExciseDutyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [CustomService].SupplyOrderID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].CustomServiceID = [CustomService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].CustomServiceID = [CustomService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [CustomService].UserID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "WHERE [CustomService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.CustomService)).Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.PortWorkService))) {
            Type[] joinTypes = {
                typeof(PortWorkService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], PortWorkService> joinMapper = objects => {
                PortWorkService portWorkService = (PortWorkService)objects[0];
                SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.PortWorkService) && i.Id.Equals(portWorkService.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.PortWorkService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.PortWorkService.ServiceNumber))
                        itemFromList.Number = itemFromList.PortWorkService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.PortWorkService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PortWorkService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PortWorkService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PortWorkService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PortWorkService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PortWorkService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (portWorkService != null && !string.IsNullOrEmpty(portWorkService.ServiceNumber)) {
                        itemFromList.Number = portWorkService.ServiceNumber;

                        if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) portWorkService.SupplyOrders.Add(supplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        portWorkService.PortWorkOrganization = portWorkOrganization;
                        portWorkService.User = user;
                    }

                    itemFromList.PortWorkService = portWorkService;
                }

                return portWorkService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [PortWorkService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PortWorkService].UserID " +
                "WHERE [PortWorkService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.PortWorkService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.TransportationService))) {
            Type[] joinTypes = {
                typeof(TransportationService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], TransportationService> joinMapper = objects => {
                TransportationService transportationService = (TransportationService)objects[0];
                SupplyOrganization transportationOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.TransportationService) && i.Id.Equals(transportationService.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.TransportationService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.TransportationService.ServiceNumber))
                        itemFromList.Number = itemFromList.TransportationService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.TransportationService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.TransportationService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.TransportationService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.TransportationService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.TransportationService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.TransportationService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (transportationService != null && !string.IsNullOrEmpty(transportationService.ServiceNumber)) {
                        itemFromList.Number = transportationService.ServiceNumber;

                        if (invoiceDocument != null) transportationService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            transportationService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) transportationService.SupplyOrders.Add(supplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        transportationService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        transportationService.TransportationOrganization = transportationOrganization;
                        transportationService.User = user;
                    }

                    itemFromList.TransportationService = transportationService;
                }

                return transportationService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [TransportationService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [TransportationService].TransportationOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [TransportationService].UserID " +
                "WHERE [TransportationService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.TransportationService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.PortCustomAgencyService))) {
            Type[] joinTypes = {
                typeof(PortCustomAgencyService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], PortCustomAgencyService> joinMapper = objects => {
                PortCustomAgencyService portCustomAgencyService = (PortCustomAgencyService)objects[0];
                SupplyOrganization portCustomAgencyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.PortCustomAgencyService) && i.Id.Equals(portCustomAgencyService.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.PortCustomAgencyService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.PortCustomAgencyService.ServiceNumber))
                        itemFromList.Number = itemFromList.PortCustomAgencyService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.PortCustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PortCustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PortCustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PortCustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PortCustomAgencyService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PortCustomAgencyService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (portCustomAgencyService != null && !string.IsNullOrEmpty(portCustomAgencyService.ServiceNumber)) {
                        itemFromList.Number = portCustomAgencyService.ServiceNumber;

                        if (invoiceDocument != null) portCustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            portCustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) portCustomAgencyService.SupplyOrders.Add(supplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        portCustomAgencyService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        portCustomAgencyService.PortCustomAgencyOrganization = portCustomAgencyOrganization;
                        portCustomAgencyService.User = user;
                    }

                    itemFromList.PortCustomAgencyService = portCustomAgencyService;
                }

                return portCustomAgencyService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [PortCustomAgencyService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PortCustomAgencyService].UserID " +
                "WHERE [PortCustomAgencyService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.PortCustomAgencyService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.CustomAgencyService))) {
            Type[] joinTypes = {
                typeof(CustomAgencyService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], CustomAgencyService> joinMapper = objects => {
                CustomAgencyService customAgencyService = (CustomAgencyService)objects[0];
                SupplyOrganization customAgencyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.CustomAgencyService) && i.Id.Equals(customAgencyService.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.CustomAgencyService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.CustomAgencyService.ServiceNumber))
                        itemFromList.Number = itemFromList.CustomAgencyService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.CustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.CustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.CustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.CustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.CustomAgencyService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.CustomAgencyService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (customAgencyService != null && !string.IsNullOrEmpty(customAgencyService.ServiceNumber)) {
                        itemFromList.Number = customAgencyService.ServiceNumber;

                        if (invoiceDocument != null) customAgencyService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            customAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) customAgencyService.SupplyOrders.Add(supplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        customAgencyService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        customAgencyService.CustomAgencyOrganization = customAgencyOrganization;
                        customAgencyService.User = user;
                    }

                    itemFromList.CustomAgencyService = customAgencyService;
                }

                return customAgencyService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [CustomAgencyService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [CustomAgencyService].UserID " +
                "WHERE [CustomAgencyService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.CustomAgencyService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.PlaneDeliveryService))) {
            Type[] joinTypes = {
                typeof(PlaneDeliveryService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], PlaneDeliveryService> joinMapper = objects => {
                PlaneDeliveryService planeDeliveryService = (PlaneDeliveryService)objects[0];
                SupplyOrganization planeDeliveryOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.PlaneDeliveryService) && i.Id.Equals(planeDeliveryService.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.PlaneDeliveryService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.PlaneDeliveryService.ServiceNumber))
                        itemFromList.Number = itemFromList.PlaneDeliveryService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.PlaneDeliveryService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PlaneDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PlaneDeliveryService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PlaneDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PlaneDeliveryService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PlaneDeliveryService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (planeDeliveryService != null && !string.IsNullOrEmpty(planeDeliveryService.ServiceNumber)) {
                        itemFromList.Number = planeDeliveryService.ServiceNumber;

                        if (invoiceDocument != null) planeDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            planeDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) planeDeliveryService.SupplyOrders.Add(supplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        planeDeliveryService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        planeDeliveryService.PlaneDeliveryOrganization = planeDeliveryOrganization;
                        planeDeliveryService.User = user;
                    }

                    itemFromList.PlaneDeliveryService = planeDeliveryService;
                }

                return planeDeliveryService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [PlaneDeliveryService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PlaneDeliveryService].PlaneDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PlaneDeliveryService].UserID " +
                "WHERE [PlaneDeliveryService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.PlaneDeliveryService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.VehicleDeliveryService))) {
            Type[] joinTypes = {
                typeof(VehicleDeliveryService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], VehicleDeliveryService> joinMapper = objects => {
                VehicleDeliveryService vehicleDeliveryService = (VehicleDeliveryService)objects[0];
                SupplyOrganization vehicleDeliveryOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.VehicleDeliveryService) && i.Id.Equals(vehicleDeliveryService.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.VehicleDeliveryService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.VehicleDeliveryService.ServiceNumber))
                        itemFromList.Number = itemFromList.VehicleDeliveryService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.VehicleDeliveryService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.VehicleDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.VehicleDeliveryService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.VehicleDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.VehicleDeliveryService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.VehicleDeliveryService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (vehicleDeliveryService != null && !string.IsNullOrEmpty(vehicleDeliveryService.ServiceNumber)) {
                        itemFromList.Number = vehicleDeliveryService.ServiceNumber;

                        if (invoiceDocument != null) vehicleDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            vehicleDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) vehicleDeliveryService.SupplyOrders.Add(supplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        vehicleDeliveryService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        vehicleDeliveryService.VehicleDeliveryOrganization = vehicleDeliveryOrganization;
                        vehicleDeliveryService.User = user;
                    }

                    itemFromList.VehicleDeliveryService = vehicleDeliveryService;
                }

                return vehicleDeliveryService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [VehicleDeliveryService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [VehicleDeliveryService].UserID " +
                "WHERE [VehicleDeliveryService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.VehicleDeliveryService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ConsumablesOrder))) {
            Type[] joinTypes = {
                typeof(ConsumablesOrder),
                typeof(User),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumableProduct),
                typeof(SupplyOrganization),
                typeof(ConsumablesStorage),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(MeasureUnit),
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(ConsumablesOrderDocument),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency)
            };

            Func<object[], ConsumablesOrder> joinMapper = objects => {
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[0];
                User user = (User)objects[1];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
                ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[6];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[7];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[8];
                MeasureUnit measureUnit = (MeasureUnit)objects[9];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[10];
                User supplyPaymentTaskUser = (User)objects[11];
                ConsumablesOrderDocument consumablesOrderDocument = (ConsumablesOrderDocument)objects[12];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
                Organization organization = (Organization)objects[14];
                Currency currency = (Currency)objects[15];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.ConsumablesOrder) && i.Id.Equals(consumablesOrder.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.ConsumablesOrder == null) {
                    if (consumablesOrder != null && !string.IsNullOrEmpty(consumablesOrder.Number)) {
                        itemFromList.Number = consumablesOrder.Number;

                        if (consumablesOrderItem != null) {
                            if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            if (supplyOrganizationAgreement != null) {
                                supplyOrganizationAgreement.Currency = currency;

                                supplyOrganizationAgreement.Organization = organization;
                            }

                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                            consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                            consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                            consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT, 2);

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                            consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPriceWithVAT, 2);

                            consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                        }

                        if (supplyPaymentTask != null) supplyPaymentTask.User = supplyPaymentTaskUser;

                        if (consumablesOrderDocument != null) consumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);

                        consumablesOrder.User = user;
                        consumablesOrder.SupplyPaymentTask = supplyPaymentTask;
                        consumablesOrder.ConsumablesStorage = consumablesStorage;
                        consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    }

                    itemFromList.ConsumablesOrder = consumablesOrder;

                    itemFromList.Comment = consumablesOrder.Comment;
                } else {
                    if (!string.IsNullOrEmpty(itemFromList.ConsumablesOrder.Number))
                        itemFromList.Number = itemFromList.ConsumablesOrder.Number;

                    if (consumablesOrderItem != null && !itemFromList.ConsumablesOrder.ConsumablesOrderItems.Any(i => i.Id.Equals(consumablesOrderItem.Id))) {
                        if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                        if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                        consumablesOrderItem.ConsumableProduct = consumableProduct;
                        consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                        consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                        consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT + consumablesOrderItem.VAT, 2);

                        itemFromList.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                        itemFromList.ConsumablesOrder.TotalAmount = Math.Round(itemFromList.ConsumablesOrder.TotalAmount + consumablesOrderItem.TotalPriceWithVAT, 2);

                        itemFromList.ConsumablesOrder.TotalAmountWithoutVAT =
                            Math.Round(itemFromList.ConsumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                    }

                    if (consumablesOrderDocument != null && !itemFromList.ConsumablesOrder.ConsumablesOrderDocuments.Any(d => d.Id.Equals(consumablesOrderDocument.Id)))
                        itemFromList.ConsumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);
                }

                return consumablesOrder;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ConsumablesOrder] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN (" +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture" +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN (" +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture" +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN (" +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
                "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [ConsumablesOrderDocument] " +
                "ON [ConsumablesOrderDocument].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [ConsumableProductOrganizationOrganization] " +
                "ON [ConsumableProductOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [ConsumableProductOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "WHERE [ConsumablesOrder].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ConsumablesOrder)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder))) {
            object parameters = new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.OutcomePaymentOrder)).Select(s => s.Id)
            };

            string sqlExpression =
                "SELECT [OutcomePaymentOrder].[ID] " +
                ",[OutcomePaymentOrder].[Account] " +
                ",[OutcomePaymentOrder].[Amount] " +
                ",[OutcomePaymentOrder].[EuroAmount] " +
                ",[OutcomePaymentOrder].[Comment] " +
                ",[OutcomePaymentOrder].[Created] " +
                ",[OutcomePaymentOrder].[Deleted] " +
                ",[OutcomePaymentOrder].[FromDate] " +
                ",[OutcomePaymentOrder].[NetUID] " +
                ",[OutcomePaymentOrder].[Number] " +
                ",[OutcomePaymentOrder].[OrganizationID] " +
                ",[OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                ",[OutcomePaymentOrder].[Updated] " +
                ",[OutcomePaymentOrder].[UserID] " +
                ",[OutcomePaymentOrder].[IsUnderReport] " +
                ",[OutcomePaymentOrder].[ColleagueID] " +
                ",[OutcomePaymentOrder].[IsUnderReportDone] " +
                ",[OutcomePaymentOrder].[AdvanceNumber] " +
                ",[OutcomePaymentOrder].[ConsumableProductOrganizationID] " +
                ",[OutcomePaymentOrder].[IsAccounting] " +
                ",[OutcomePaymentOrder].[IsManagementAccounting] " +
                ",[OutcomePaymentOrder].[ExchangeRate] " +
                ",[dbo].[GetGovExchangedToEuroValue]( " +
                "[OutcomePaymentOrder].[AfterExchangeAmount], " +
                "[OutcomeAgreement].CurrencyID, " +
                "[OutcomePaymentOrder].[FromDate] " +
                ") AS [AfterExchangeAmount] " +
                ",[OutcomePaymentOrder].[ClientAgreementID] " +
                ",[OutcomePaymentOrder].[SupplyOrderPolandPaymentDeliveryProtocolID] " +
                ",[OutcomePaymentOrder].[SupplyOrganizationAgreementID] " +
                ", ( " +
                "SELECT ROUND( " +
                "( " +
                "SELECT (0 - [ForDifferenceOutcome].Amount) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0)) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [ForDifferenceOutcome].ID " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [ForDifferenceOutcome].IsUnderReport = 1 " +
                ") " +
                "+ " +
                "( " +
                "SELECT (ISNULL(SUM([CompanyCarFueling].TotalPriceWithVat), 0)) " +
                "FROM [CompanyCarFueling] " +
                "WHERE [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "AND [CompanyCarFueling].Deleted = 0 " +
                ") " +
                "+ " +
                "( " +
                "SELECT ISNULL(SUM([AssignedIncome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
                "ON [AssignedIncome].ID = [ForDifferenceAssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                "AND [AssignedIncome].IsCanceled = 0 " +
                ") " +
                "- " +
                "( " +
                "SELECT ISNULL(SUM([AssignedOutcome].Amount), 0) " +
                "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
                "ON [AssignedOutcome].ID = [ForDifferenceAssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                ") " +
                ", 2) " +
                ") AS [DifferenceAmount] " +
                ", [Organization].* " +
                ", [User].* " +
                ", [PaymentMovementOperation].* " +
                ", [PaymentMovement].* " +
                ", [PaymentCurrencyRegister].* " +
                ", [Currency].* " +
                ", [PaymentRegister].* " +
                ", [PaymentRegisterOrganization].* " +
                ", [Colleague].* " +
                ", [OutcomeConsumableProductOrganization].* " +
                ", [OutcomePaymentOrderSupplyPaymentTask].* " +
                ", [SupplyPaymentTask].* " +
                ", [OutcomeAgreement].* " +
                ", [OutcomeAgreementCurrency].* " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [OutcomePaymentOrder].UserID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
                "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
                "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Colleague] " +
                "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
                "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
                "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                "ON [OutcomePaymentOrderSupplyPaymentTask].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
                "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
                "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
                "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrder].ID IN @Ids ";

            Type[] types = {
                typeof(OutcomePaymentOrder),
                typeof(Organization),
                typeof(User),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(PaymentCurrencyRegister),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(Organization),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(OutcomePaymentOrderSupplyPaymentTask),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrder> mapper = objects => {
                OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                User user = (User)objects[2];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
                PaymentMovement paymentMovement = (PaymentMovement)objects[4];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
                Currency currency = (Currency)objects[6];
                PaymentRegister paymentRegister = (PaymentRegister)objects[7];
                Organization paymentRegisterOrganization = (Organization)objects[8];
                User colleague = (User)objects[9];
                SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[10];
                OutcomePaymentOrderSupplyPaymentTask junctionTask = (OutcomePaymentOrderSupplyPaymentTask)objects[11];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[12];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[13];
                Currency supplyOrganizationAgreementCurrency = (Currency)objects[14];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrder.Id));

                itemFromList.IsAccounting = outcomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = outcomePaymentOrder.IsManagementAccounting;
                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.OutcomePaymentOrder == null) {
                    if (outcomePaymentOrder != null && !string.IsNullOrEmpty(outcomePaymentOrder.Number)) {
                        itemFromList.Number = outcomePaymentOrder.Number;

                        if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                        if (junctionTask != null) {
                            junctionTask.SupplyPaymentTask = supplyPaymentTask;

                            outcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                        }

                        if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                        paymentRegister.Organization = paymentRegisterOrganization;

                        paymentCurrencyRegister.PaymentRegister = paymentRegister;
                        paymentCurrencyRegister.Currency = currency;

                        outcomePaymentOrder.Organization = organization;
                        outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                        outcomePaymentOrder.User = user;
                        outcomePaymentOrder.Colleague = colleague;
                        outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                        outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                        outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    }

                    itemFromList.OutcomePaymentOrder = outcomePaymentOrder;
                } else {
                    if (!string.IsNullOrEmpty(itemFromList.OutcomePaymentOrder.Number))
                        itemFromList.Number = itemFromList.OutcomePaymentOrder.Number;

                    if (junctionTask == null
                        || itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.Id.Equals(junctionTask.Id)))
                        return outcomePaymentOrder;

                    junctionTask.SupplyPaymentTask = supplyPaymentTask;

                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                }

                return outcomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                parameters
            );

            string orderSqlQuery =
                "SELECT " +
                "* " +
                "FROM [OutcomePaymentOrderConsumablesOrder] " +
                "LEFT JOIN [ConsumablesOrder] " +
                "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN ( " +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
                "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN ( " +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
                "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
                "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
                "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
                "WHERE [OutcomePaymentOrderConsumablesOrder].[OutcomePaymentOrderID] IN @Ids ";

            Type[] orderTypes = {
                typeof(OutcomePaymentOrderConsumablesOrder),
                typeof(ConsumablesOrder),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumableProduct),
                typeof(SupplyOrganization),
                typeof(User),
                typeof(ConsumablesStorage),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(MeasureUnit),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], OutcomePaymentOrderConsumablesOrder> orderMapper = objects => {
                OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[0];
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[1];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[5];
                User consumablesOrderUser = (User)objects[6];
                ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[7];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[8];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[9];
                MeasureUnit measureUnit = (MeasureUnit)objects[10];
                SupplyOrganizationAgreement consumablesSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[11];
                Currency consumablesSupplyOrganizationAgreementCurrency = (Currency)objects[12];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.OutcomePaymentOrder) && i.Id.Equals(outcomePaymentOrderConsumablesOrder.OutcomePaymentOrderId));

                if (!itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id)))
                    itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                else
                    outcomePaymentOrderConsumablesOrder =
                        itemFromList.OutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                if (outcomePaymentOrderConsumablesOrder.ConsumablesOrder == null)
                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                if (paymentCostMovementOperation != null)
                    paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                if (consumableProduct != null)
                    consumableProduct.MeasureUnit = measureUnit;

                if (consumablesSupplyOrganizationAgreement != null)
                    consumablesSupplyOrganizationAgreement.Currency = consumablesSupplyOrganizationAgreementCurrency;

                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                consumablesOrderItem.ConsumableProduct = consumableProduct;
                consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                consumablesOrderItem.SupplyOrganizationAgreement = consumablesSupplyOrganizationAgreement;

                consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPriceWithVAT, 2);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.User = consumablesOrderUser;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesStorage = consumablesStorage;

                return outcomePaymentOrderConsumablesOrder;
            };

            _connection.Query(
                orderSqlQuery,
                orderTypes,
                orderMapper,
                parameters);
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder))) {
            string sqlExpression =
                "SELECT * " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
                "AND [PaymentCurrencyRegister].CurrencyID = [Currency].ID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentMovementOperation].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                "AND [PaymentMovementOperation].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT [PaymentMovement].ID " +
                ", [PaymentMovement].[Created] " +
                ", [PaymentMovement].[Deleted] " +
                ", [PaymentMovement].[NetUID] " +
                ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentMovement].[Updated] " +
                "FROM [PaymentMovement] " +
                "LEFT JOIN [PaymentMovementTranslation] " +
                "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
                "AND [PaymentMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentMovement] " +
                "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [IncomePaymentOrder].UserID " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [IncomePaymentOrder].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [IncomePaymentOrder].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "WHERE [IncomePaymentOrder].ID IN @Ids";

            Type[] types = {
                typeof(IncomePaymentOrder),
                typeof(Organization),
                typeof(Currency),
                typeof(PaymentRegister),
                typeof(PaymentCurrencyRegister),
                typeof(PaymentMovementOperation),
                typeof(PaymentMovement),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency)
            };

            Func<object[], IncomePaymentOrder> mapper = objects => {
                IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
                Organization organization = (Organization)objects[1];
                Currency currency = (Currency)objects[2];
                PaymentRegister paymentRegister = (PaymentRegister)objects[3];
                PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[4];
                PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[5];
                PaymentMovement paymentMovement = (PaymentMovement)objects[6];
                User user = (User)objects[7];
                SupplyOrganization incomeClient = (SupplyOrganization)objects[8];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[9];
                Currency agreementCurrency = (Currency)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.IncomePaymentOrder) && i.Id.Equals(incomePaymentOrder.Id));

                itemFromList.IsAccounting = incomePaymentOrder.IsAccounting;
                itemFromList.IsManagementAccounting = incomePaymentOrder.IsManagementAccounting;
                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.IncomePaymentOrder != null) {
                    itemFromList.Number = itemFromList.IncomePaymentOrder.Number;
                } else {
                    if (!string.IsNullOrEmpty(incomePaymentOrder.Number))
                        itemFromList.Number = incomePaymentOrder.Number;

                    if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = agreementCurrency;

                    if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);

                    incomePaymentOrder.Organization = organization;
                    incomePaymentOrder.User = user;
                    incomePaymentOrder.Currency = currency;
                    incomePaymentOrder.PaymentRegister = paymentRegister;
                    incomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                    incomePaymentOrder.SupplyOrganization = incomeClient;
                    incomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    itemFromList.IncomePaymentOrder = incomePaymentOrder;
                }

                return incomePaymentOrder;
            };

            _connection.Query(
                sqlExpression,
                types,
                mapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.IncomePaymentOrder)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                },
                commandTimeout: 3600
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyPaymentTask))) {
            Type[] joinTypes = {
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyPaymentTaskDocument),
                typeof(ContainerService),
                typeof(SupplyOrganization),
                typeof(Organization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderContainerService),
                typeof(SupplyOrder),
                typeof(User),
                typeof(PortWorkService),
                typeof(SupplyOrganization),
                typeof(Organization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], SupplyPaymentTask> joinMapper = objects => {
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[0];
                User user = (User)objects[1];
                SupplyPaymentTaskDocument supplyPaymentTaskDocument = (SupplyPaymentTaskDocument)objects[2];
                ContainerService containerService = (ContainerService)objects[3];
                SupplyOrganization containerOrganization = (SupplyOrganization)objects[4];
                SupplyOrganizationAgreement containerSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[5];
                Organization containerOrganizationOrganization = (Organization)objects[6];
                Currency containerOrganizationCurrency = (Currency)objects[7];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[8];
                InvoiceDocument containerInvoiceDocument = (InvoiceDocument)objects[9];
                SupplyOrderContainerService junction = (SupplyOrderContainerService)objects[10];
                SupplyOrder containerSupplyOrder = (SupplyOrder)objects[11];
                User containerUser = (User)objects[12];
                PortWorkService portWorkService = (PortWorkService)objects[13];
                SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[14];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[15];
                Organization organization = (Organization)objects[16];
                Currency currency = (Currency)objects[17];
                SupplyOrder portWorkSupplyOrder = (SupplyOrder)objects[18];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[19];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[20];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[21];
                User portWorkUser = (User)objects[22];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.SupplyPaymentTask) && i.Id.Equals(supplyPaymentTask.Id));

                itemFromList.OrganizationName = portWorkOrganization?.Name ?? "";

                if (itemFromList.SupplyPaymentTask != null) {
                    if (containerSupplyOrder != null) {
                        itemFromList.Number = containerSupplyOrder.SupplyOrderNumberId.ToString();

                        itemFromList.Comment = containerSupplyOrder.Comment;
                    } else if (portWorkSupplyOrder != null) {
                        itemFromList.Number = portWorkSupplyOrder.SupplyOrderNumberId.ToString();
                    }

                    if (containerService != null) {
                        if (!itemFromList.SupplyPaymentTask.ContainerServices.Any(s => s.Id.Equals(containerService.Id))) {
                            if (junction != null) {
                                junction.SupplyOrder = containerSupplyOrder;

                                containerService.SupplyOrderContainerServices.Add(junction);
                            }

                            if (containerInvoiceDocument != null) containerService.InvoiceDocuments.Add(containerInvoiceDocument);

                            if (containerSupplyOrganizationAgreement != null) {
                                containerSupplyOrganizationAgreement.Currency = containerOrganizationCurrency;

                                containerSupplyOrganizationAgreement.Organization = containerOrganizationOrganization;
                            }

                            containerService.SupplyOrganizationAgreement = containerSupplyOrganizationAgreement;
                            containerService.BillOfLadingDocument = billOfLadingDocument;
                            containerService.User = containerUser;
                            containerService.ContainerOrganization = containerOrganization;

                            itemFromList.SupplyPaymentTask.ContainerServices.Add(containerService);
                        } else {
                            ContainerService fromList = itemFromList.SupplyPaymentTask.ContainerServices.First(s => s.Id.Equals(containerService.Id));

                            if (junction != null && !fromList.SupplyOrderContainerServices.Any(j => j.Id.Equals(junction.Id))) {
                                junction.SupplyOrder = containerSupplyOrder;

                                fromList.SupplyOrderContainerServices.Add(junction);
                            }

                            if (containerInvoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(containerInvoiceDocument.Id)))
                                fromList.InvoiceDocuments.Add(containerInvoiceDocument);
                        }
                    }

                    if (portWorkService == null) return supplyPaymentTask;

                    if (!itemFromList.SupplyPaymentTask.PortWorkServices.Any(s => s.Id.Equals(portWorkService.Id))) {
                        if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (portWorkSupplyOrder != null) portWorkService.SupplyOrders.Add(portWorkSupplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        portWorkService.PortWorkOrganization = portWorkOrganization;
                        portWorkService.User = portWorkUser;

                        supplyPaymentTask.PortWorkServices.Add(portWorkService);
                    } else {
                        PortWorkService fromList = itemFromList.SupplyPaymentTask.PortWorkServices.First(s => s.Id.Equals(portWorkService.Id));

                        if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null && !fromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            fromList.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (portWorkSupplyOrder != null && !fromList.SupplyOrders.Any(o => o.Id.Equals(portWorkSupplyOrder.Id))) fromList.SupplyOrders.Add(portWorkSupplyOrder);
                    }
                } else {
                    if (containerSupplyOrder != null) {
                        itemFromList.Comment = containerSupplyOrder.Comment;
                        itemFromList.Number = containerSupplyOrder.SupplyOrderNumberId.ToString();
                    } else if (portWorkSupplyOrder != null) {
                        itemFromList.Number = portWorkSupplyOrder.SupplyOrderNumberId.ToString();
                    }

                    if (containerService != null) {
                        if (junction != null) {
                            junction.SupplyOrder = containerSupplyOrder;

                            containerService.SupplyOrderContainerServices.Add(junction);
                        }

                        if (containerInvoiceDocument != null) containerService.InvoiceDocuments.Add(containerInvoiceDocument);

                        if (containerSupplyOrganizationAgreement != null) {
                            containerSupplyOrganizationAgreement.Currency = containerOrganizationCurrency;

                            containerSupplyOrganizationAgreement.Organization = containerOrganizationOrganization;
                        }

                        containerService.SupplyOrganizationAgreement = containerSupplyOrganizationAgreement;
                        containerService.BillOfLadingDocument = billOfLadingDocument;
                        containerService.User = containerUser;
                        containerService.ContainerOrganization = containerOrganization;

                        supplyPaymentTask.ContainerServices.Add(containerService);
                    }

                    if (portWorkService != null) {
                        if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (portWorkSupplyOrder != null) portWorkService.SupplyOrders.Add(portWorkSupplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        portWorkService.PortWorkOrganization = portWorkOrganization;
                        portWorkService.User = portWorkUser;

                        supplyPaymentTask.PortWorkServices.Add(portWorkService);
                    }

                    if (supplyPaymentTaskDocument != null) supplyPaymentTask.SupplyPaymentTaskDocuments.Add(supplyPaymentTaskDocument);

                    supplyPaymentTask.User = user;

                    itemFromList.SupplyPaymentTask = supplyPaymentTask;
                }

                return supplyPaymentTask;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [SupplyPaymentTaskDocument] " +
                "ON [SupplyPaymentTaskDocument].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [ContainerService] " +
                "ON [ContainerService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
                "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ContainerSupplyOrganizationAgreement] " +
                "ON [ContainerSupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] AS [ContainerOrganizationOrganization] " +
                "ON [ContainerOrganizationOrganization].ID = [ContainerSupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [ContainerOrganizationCurrency] " +
                "ON [ContainerOrganizationCurrency].ID = [ContainerSupplyOrganizationAgreement].CurrencyID " +
                "AND [ContainerOrganizationCurrency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] AS [ContainerInvoiceDocument] " +
                "ON [ContainerInvoiceDocument].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrderContainerService] " +
                "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                "LEFT JOIN [User] AS [ContainerUser] " +
                "ON [ContainerUser].ID = [ContainerService].UserID " +
                "LEFT JOIN [PortWorkService] " +
                "ON [PortWorkService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [PortWorkOrganization] " +
                "ON [PortWorkOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [PortWorkOrganizationOrganization] " +
                "ON [PortWorkOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [PortWorkOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [PortWorkCurrency] " +
                "ON [PortWorkCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [PortWorkCurrency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] AS [PortWorkSupplyOrder] " +
                "ON [PortWorkSupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] AS [PortWorkInvoiceDocument] " +
                "ON [PortWorkInvoiceDocument].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [User] AS [PortWorkUser] " +
                "ON [PortWorkUser].ID = [PortWorkService].UserID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyPaymentTask)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.MergedService))) {
            Type[] joinTypes = {
                typeof(MergedService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(SupplyOrderUkraine),
                typeof(ConsumableProduct),
                typeof(ActProvidingService)
            };

            Func<object[], MergedService> joinMapper = objects => {
                MergedService mergedService = (MergedService)objects[0];
                SupplyOrganization mergedSupplyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[10];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[11];
                ActProvidingService actProvidingService = (ActProvidingService)objects[12];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.MergedService) && i.Id.Equals(mergedService.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.MergedService != null) {
                    itemFromList.Number = itemFromList.MergedService.ActProvidingService.Number;

                    itemFromList.MergedService.ConsumableProduct = consumableProduct;

                    itemFromList.MergedService.ActProvidingService = actProvidingService;

                    if (invoiceDocument != null && !itemFromList.MergedService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.MergedService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem == null || itemFromList.MergedService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id)))
                        return mergedService;

                    serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                    itemFromList.MergedService.ServiceDetailItems.Add(serviceDetailItem);
                } else {
                    if (mergedService != null) {
                        if (actProvidingService != null) itemFromList.Number = actProvidingService.Number;

                        mergedService.ConsumableProduct = consumableProduct;
                        mergedService.ActProvidingService = actProvidingService;

                        if (invoiceDocument != null) mergedService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            mergedService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        mergedService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        mergedService.SupplyOrganization = mergedSupplyOrganization;
                        mergedService.SupplyOrderUkraine = supplyOrderUkraine;
                        mergedService.SupplyOrder = supplyOrder;
                        mergedService.User = user;
                    }

                    itemFromList.MergedService = mergedService;
                }

                return mergedService;
            };

            _connection.Query(
                "SELECT [MergedService].[ID] " +
                ",[MergedService].[Created] " +
                ",[MergedService].[Deleted] " +
                ",[MergedService].[FromDate] " +
                ",[MergedService].[IsActive] " +
                ",[MergedService].[NetUID] " +
                ",[MergedService].[SupplyPaymentTaskID] " +
                ",[MergedService].[Updated] " +
                ",[MergedService].[UserID] " +
                ",[MergedService].[SupplyOrganizationID] " +
                ",[MergedService].[Vat] " +
                ",[MergedService].[AccountingVat] " +
                ",[MergedService].[Number] " +
                ",[MergedService].[DeliveryProductProtocolID] " +
                ",[MergedService].[Name] " +
                ",[MergedService].[VatPercent] " +
                ",[MergedService].[AccountingVatPercent] " +
                ",[MergedService].[ServiceNumber] " +
                ",[MergedService].[SupplyOrganizationAgreementID] " +
                ",[MergedService].[ConsumableProductID] " +
                ", [MergedService].[GrossPrice] " +
                ", [MergedService].[AccountingGrossPrice] " +
                ", [MergedService].[NetPrice] " +
                ", [MergedService].[AccountingNetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[SupplyOrderUkraine].* " +
                ",[ConsumableProduct].* " +
                ",[ActProvidingService].* " +
                "FROM [MergedService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [MergedService].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [MergedService].SupplyOrderID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].MergedServiceID = [MergedService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].MergedServiceID = [MergedService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [MergedService].UserID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [MergedService].SupplyOrderUkraineID " +
                "LEFT JOIN [ConsumableProduct] " +
                "ON [ConsumableProduct].[ID] = [MergedService].[ConsumableProductID] " +
                "LEFT JOIN [ActProvidingService] " +
                "ON [ActProvidingService].[ID] = [MergedService].[ActProvidingServiceID] " +
                "WHERE [MergedService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.MergedService)).Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.DeliveryExpense))) {
            Type[] joinTypes = {
                typeof(DeliveryExpense),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(User),
                typeof(SupplyOrderUkraine),
                typeof(OrganizationTranslation),
                typeof(ConsumableProduct),
                typeof(ActProvidingService)
            };

            Func<object[], DeliveryExpense> joinMapper = objects => {
                DeliveryExpense deliveryExpense = (DeliveryExpense)objects[0];
                SupplyOrganization deliverySupplyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                User user = (User)objects[5];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[6];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[7];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[8];
                ActProvidingService actProvidingService = (ActProvidingService)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.DeliveryExpense) && i.Id.Equals(deliveryExpense.Id));

                itemFromList.IsManagementAccounting = true;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.DeliveryExpense != null) {
                    itemFromList.Number = itemFromList.DeliveryExpense.ActProvidingService.Number;

                    itemFromList.DeliveryExpense.ConsumableProduct = consumableProduct;
                    itemFromList.DeliveryExpense.ActProvidingService = actProvidingService;
                } else {
                    if (deliveryExpense != null) {
                        if (actProvidingService != null) {
                            itemFromList.Number = actProvidingService.Number;
                            itemFromList.IsAccounting = actProvidingService.IsAccounting;
                        }

                        deliveryExpense.ConsumableProduct = consumableProduct;
                        deliveryExpense.ActProvidingService = actProvidingService;

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        deliveryExpense.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        deliveryExpense.SupplyOrganization = deliverySupplyOrganization;
                        deliveryExpense.SupplyOrderUkraine = supplyOrderUkraine;
                        deliveryExpense.User = user;
                    }

                    itemFromList.DeliveryExpense = deliveryExpense;
                }

                return deliveryExpense;
            };

            _connection.Query(
                "SELECT [DeliveryExpense].ID " +
                ",[DeliveryExpense].Created " +
                ",[DeliveryExpense].Deleted " +
                ",[DeliveryExpense].FromDate " +
                ",[DeliveryExpense].NetUID " +
                ",[DeliveryExpense].Updated " +
                ",[DeliveryExpense].UserID " +
                ",[DeliveryExpense].SupplyOrganizationID " +
                ",[DeliveryExpense].VatPercent " +
                ",[DeliveryExpense].AccountingVatPercent " +
                ",[DeliveryExpense].InvoiceNumber " +
                ",[DeliveryExpense].SupplyOrganizationAgreementID " +
                ",[DeliveryExpense].ConsumableProductID " +
                ", [dbo].[GetExchangedToEuroValue]( " +
                "[DeliveryExpense].[GrossAmount], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossAmount] " +
                ", [dbo].[GetExchangedToEuroValue]( " +
                "[DeliveryExpense].AccountingGrossAmount, " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [AccountingGrossAmount] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[User].* " +
                ",[SupplyOrderUkraine].* " +
                ",[OrganizationTranslation].* " +
                ",[ConsumableProduct].* " +
                ",[ActProvidingService].* " +
                "FROM [DeliveryExpense] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [DeliveryExpense].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [DeliveryExpense].UserID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [DeliveryExpense].SupplyOrderUkraineID " +
                "LEFT JOIN [ConsumableProduct] " +
                "ON [ConsumableProduct].[ID] = [DeliveryExpense].[ConsumableProductID] " +
                "LEFT JOIN [ActProvidingService] " +
                "ON [ActProvidingService].[ID] = [DeliveryExpense].[ActProvidingServiceID] " +
                "WHERE [DeliveryExpense].ID IN @Ids ",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.DeliveryExpense)).Select(s => s.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                });
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingDeliveryExpense))) {
            Type[] joinTypes = {
                typeof(DeliveryExpense),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(User),
                typeof(SupplyOrderUkraine),
                typeof(OrganizationTranslation),
                typeof(ConsumableProduct),
                typeof(ActProvidingService)
            };

            Func<object[], DeliveryExpense> joinMapper = objects => {
                DeliveryExpense deliveryExpense = (DeliveryExpense)objects[0];
                SupplyOrganization deliverySupplyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                User user = (User)objects[5];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[6];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[7];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[8];
                ActProvidingService actProvidingService = (ActProvidingService)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.AccountingDeliveryExpense) && i.Id.Equals(deliveryExpense.Id));

                itemFromList.IsAccounting = true;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.DeliveryExpense != null) {
                    itemFromList.Number = itemFromList.DeliveryExpense.ActProvidingService.Number;

                    itemFromList.DeliveryExpense.ConsumableProduct = consumableProduct;
                    itemFromList.DeliveryExpense.ActProvidingService = actProvidingService;
                } else {
                    if (deliveryExpense != null) {
                        if (actProvidingService != null) {
                            itemFromList.Number = actProvidingService.Number;
                            itemFromList.IsAccounting = actProvidingService.IsAccounting;
                        }

                        deliveryExpense.ConsumableProduct = consumableProduct;
                        deliveryExpense.ActProvidingService = actProvidingService;

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        deliveryExpense.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        deliveryExpense.SupplyOrganization = deliverySupplyOrganization;
                        deliveryExpense.SupplyOrderUkraine = supplyOrderUkraine;
                        deliveryExpense.User = user;
                    }

                    itemFromList.DeliveryExpense = deliveryExpense;
                }

                return deliveryExpense;
            };

            _connection.Query(
                "SELECT [DeliveryExpense].ID " +
                ",[DeliveryExpense].Created " +
                ",[DeliveryExpense].Deleted " +
                ",[DeliveryExpense].FromDate " +
                ",[DeliveryExpense].NetUID " +
                ",[DeliveryExpense].Updated " +
                ",[DeliveryExpense].UserID " +
                ",[DeliveryExpense].SupplyOrganizationID " +
                ",[DeliveryExpense].VatPercent " +
                ",[DeliveryExpense].AccountingVatPercent " +
                ",[DeliveryExpense].InvoiceNumber " +
                ",[DeliveryExpense].SupplyOrganizationAgreementID " +
                ",[DeliveryExpense].ConsumableProductID " +
                ", [dbo].[GetExchangedToEuroValue]( " +
                "[DeliveryExpense].[GrossAmount], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [GrossAmount] " +
                ", [dbo].[GetExchangedToEuroValue]( " +
                "[DeliveryExpense].AccountingGrossAmount, " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "GETUTCDATE() " +
                ") AS [AccountingGrossAmount] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[User].* " +
                ",[SupplyOrderUkraine].* " +
                ",[OrganizationTranslation].* " +
                ",[ConsumableProduct].* " +
                ",[ActProvidingService].* " +
                "FROM [DeliveryExpense] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [DeliveryExpense].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [DeliveryExpense].UserID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[CultureCode] = @Culture " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [DeliveryExpense].SupplyOrderUkraineID " +
                "LEFT JOIN [ConsumableProduct] " +
                "ON [ConsumableProduct].[ID] = [DeliveryExpense].[ConsumableProductID] " +
                "LEFT JOIN [ActProvidingService] " +
                "ON [ActProvidingService].[ID] = [DeliveryExpense].[AccountingActProvidingServiceID] " +
                "WHERE [DeliveryExpense].ID IN @Ids ",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingDeliveryExpense)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                });
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.VehicleService))) {
            Type[] joinTypes = {
                typeof(VehicleService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderVehicleService),
                typeof(SupplyOrder),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], VehicleService> joinMapper = objects => {
                VehicleService vehicleService = (VehicleService)objects[0];
                SupplyOrganization vehicleOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                SupplyOrderVehicleService junction = (SupplyOrderVehicleService)objects[7];
                SupplyOrder supplyOrder = (SupplyOrder)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow
                        .AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.VehicleService) && i.Id.Equals(vehicleService.Id));
                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.VehicleService != null) {
                    if (invoiceDocument != null && !itemFromList.VehicleService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.VehicleService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction == null || itemFromList.VehicleService.SupplyOrderVehicleServices.Any(j => j.Id.Equals(junction.Id)))
                        return vehicleService;

                    junction.SupplyOrder = supplyOrder;

                    itemFromList.VehicleService.SupplyOrderVehicleServices.Add(junction);
                } else {
                    itemFromList.Number = vehicleService.ServiceNumber;

                    if (invoiceDocument != null) vehicleService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction != null) {
                        junction.SupplyOrder = supplyOrder;

                        vehicleService.SupplyOrderVehicleServices.Add(junction);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    vehicleService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    vehicleService.VehicleOrganization = vehicleOrganization;
                    vehicleService.BillOfLadingDocument = billOfLadingDocument;
                    vehicleService.User = user;

                    itemFromList.VehicleService = vehicleService;
                }

                return vehicleService;
            };

            _connection.Query(
                "SELECT [VehicleService].[ID] " +
                ", [dbo].[GetGovExchangedToEuroValue](" +
                "[VehicleService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "[BillOfLadingDocument].[Date]" +
                ") AS [NetPrice] " +
                ",[VehicleService].[BillOfLadingDocumentID] " +
                ",[VehicleService].[Created] " +
                ",[VehicleService].[Deleted] " +
                ",[VehicleService].[IsActive] " +
                ",[VehicleService].[LoadDate] " +
                ",[VehicleService].[NetUID] " +
                ",[VehicleService].[TermDeliveryInDays] " +
                ",[VehicleService].[Updated] " +
                ",[VehicleService].[SupplyPaymentTaskID] " +
                ",[VehicleService].[UserID] " +
                ",[VehicleService].[VehicleOrganizationID] " +
                ",[VehicleService].[FromDate] " +
                ",[VehicleService].[GrossWeight] " +
                ", [dbo].[GetGovExchangedToEuroValue](" +
                "[VehicleService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "[BillOfLadingDocument].[Date]" +
                ") AS [GrossPrice] " +
                ",[VehicleService].[Vat] " +
                ",[VehicleService].[Number] " +
                ",[VehicleService].[Name] " +
                ",[VehicleService].[VatPercent] " +
                ",[VehicleService].[IsCalculatedExtraCharge] " +
                ",[VehicleService].[SupplyExtraChargeType] " +
                ",[VehicleService].[VehicleNumber] " +
                ",[VehicleService].[ServiceNumber] " +
                ",[VehicleService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[BillOfLadingDocument].* " +
                ",[InvoiceDocument].* " +
                ",[SupplyOrderVehicleService].* " +
                ",[SupplyOrder].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [VehicleService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [VehicleService].VehicleOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [VehicleService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].VehicleServiceID = [VehicleService].ID " +
                "LEFT JOIN [SupplyOrderVehicleService] " +
                "ON [SupplyOrderVehicleService].VehicleServiceID = [VehicleService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderVehicleService].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [VehicleService].UserID " +
                "WHERE [VehicleService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.VehicleService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingContainerPaymentTask))) {
            Type[] joinTypes = {
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyPaymentTaskDocument),
                typeof(ContainerService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderContainerService),
                typeof(SupplyOrder),
                typeof(User),
                typeof(PortWorkService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], SupplyPaymentTask> joinMapper = objects => {
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[0];
                User user = (User)objects[1];
                SupplyPaymentTaskDocument supplyPaymentTaskDocument = (SupplyPaymentTaskDocument)objects[2];
                ContainerService containerService = (ContainerService)objects[3];
                SupplyOrganization containerOrganization = (SupplyOrganization)objects[4];
                SupplyOrganizationAgreement containerSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[5];
                Organization containerOrganizationOrganization = (Organization)objects[6];
                Currency containerOrganizationCurrency = (Currency)objects[7];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[8];
                InvoiceDocument containerInvoiceDocument = (InvoiceDocument)objects[9];
                SupplyOrderContainerService junction = (SupplyOrderContainerService)objects[10];
                SupplyOrder containerSupplyOrder = (SupplyOrder)objects[11];
                User containerUser = (User)objects[12];
                PortWorkService portWorkService = (PortWorkService)objects[13];
                SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[14];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[15];
                Organization organization = (Organization)objects[16];
                Currency currency = (Currency)objects[17];
                SupplyOrder portWorkSupplyOrder = (SupplyOrder)objects[18];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[19];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[20];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[21];
                User portWorkUser = (User)objects[22];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.AccountingContainerPaymentTask) && i.Id.Equals(supplyPaymentTask.Id));

                itemFromList.OrganizationName = portWorkOrganization?.Name ?? "";

                itemFromList.IsAccounting = true;

                if (itemFromList.SupplyPaymentTask != null) {
                    if (containerSupplyOrder != null)
                        itemFromList.Number = containerSupplyOrder.SupplyOrderNumberId.ToString();
                    else if (portWorkSupplyOrder != null)
                        itemFromList.Number = portWorkSupplyOrder.SupplyOrderNumberId.ToString();

                    if (containerService != null) {
                        if (!itemFromList.SupplyPaymentTask.ContainerServices.Any(s => s.Id.Equals(containerService.Id))) {
                            if (junction != null) {
                                junction.SupplyOrder = containerSupplyOrder;

                                containerService.SupplyOrderContainerServices.Add(junction);
                            }

                            if (containerInvoiceDocument != null) containerService.InvoiceDocuments.Add(containerInvoiceDocument);

                            if (containerSupplyOrganizationAgreement != null) {
                                containerSupplyOrganizationAgreement.Currency = containerOrganizationCurrency;

                                containerSupplyOrganizationAgreement.Organization = containerOrganizationOrganization;
                            }

                            containerService.SupplyOrganizationAgreement = containerSupplyOrganizationAgreement;
                            containerService.BillOfLadingDocument = billOfLadingDocument;
                            containerService.User = containerUser;
                            containerService.ContainerOrganization = containerOrganization;

                            itemFromList.SupplyPaymentTask.ContainerServices.Add(containerService);
                        } else {
                            ContainerService fromList = itemFromList.SupplyPaymentTask.ContainerServices.First(s => s.Id.Equals(containerService.Id));

                            if (junction != null && !fromList.SupplyOrderContainerServices.Any(j => j.Id.Equals(junction.Id))) {
                                junction.SupplyOrder = containerSupplyOrder;

                                fromList.SupplyOrderContainerServices.Add(junction);
                            }

                            if (containerInvoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(containerInvoiceDocument.Id)))
                                fromList.InvoiceDocuments.Add(containerInvoiceDocument);
                        }
                    }

                    if (portWorkService == null) return supplyPaymentTask;

                    if (!itemFromList.SupplyPaymentTask.PortWorkServices.Any(s => s.Id.Equals(portWorkService.Id))) {
                        if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (portWorkSupplyOrder != null) portWorkService.SupplyOrders.Add(portWorkSupplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        portWorkService.PortWorkOrganization = portWorkOrganization;
                        portWorkService.User = portWorkUser;

                        supplyPaymentTask.PortWorkServices.Add(portWorkService);
                    } else {
                        PortWorkService fromList = itemFromList.SupplyPaymentTask.PortWorkServices.First(s => s.Id.Equals(portWorkService.Id));

                        if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null && !fromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            fromList.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (portWorkSupplyOrder != null && !fromList.SupplyOrders.Any(o => o.Id.Equals(portWorkSupplyOrder.Id))) fromList.SupplyOrders.Add(portWorkSupplyOrder);
                    }
                } else {
                    if (containerSupplyOrder != null)
                        itemFromList.Number = containerSupplyOrder.SupplyOrderNumberId.ToString();
                    else if (portWorkSupplyOrder != null)
                        itemFromList.Number = portWorkSupplyOrder.SupplyOrderNumberId.ToString();

                    if (containerService != null) {
                        if (junction != null) {
                            junction.SupplyOrder = containerSupplyOrder;

                            containerService.SupplyOrderContainerServices.Add(junction);
                        }

                        if (containerInvoiceDocument != null) containerService.InvoiceDocuments.Add(containerInvoiceDocument);

                        if (containerSupplyOrganizationAgreement != null) {
                            containerSupplyOrganizationAgreement.Currency = containerOrganizationCurrency;

                            containerSupplyOrganizationAgreement.Organization = containerOrganizationOrganization;
                        }

                        containerService.SupplyOrganizationAgreement = containerSupplyOrganizationAgreement;
                        containerService.BillOfLadingDocument = billOfLadingDocument;
                        containerService.User = containerUser;
                        containerService.ContainerOrganization = containerOrganization;

                        supplyPaymentTask.ContainerServices.Add(containerService);
                    }

                    if (portWorkService != null) {
                        if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (portWorkSupplyOrder != null) portWorkService.SupplyOrders.Add(portWorkSupplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        portWorkService.PortWorkOrganization = portWorkOrganization;
                        portWorkService.User = portWorkUser;

                        supplyPaymentTask.PortWorkServices.Add(portWorkService);
                    }

                    if (supplyPaymentTaskDocument != null) supplyPaymentTask.SupplyPaymentTaskDocuments.Add(supplyPaymentTaskDocument);

                    supplyPaymentTask.User = user;

                    itemFromList.SupplyPaymentTask = supplyPaymentTask;

                    itemFromList.Comment = supplyPaymentTask.Comment;
                }

                return supplyPaymentTask;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [SupplyPaymentTaskDocument] " +
                "ON [SupplyPaymentTaskDocument].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [ContainerService] " +
                "ON [ContainerService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
                "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ContainerSupplyOrganizationAgreement] " +
                "ON [ContainerSupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] AS [ContainerOrganizationOrganization] " +
                "ON [ContainerOrganizationOrganization].ID = [ContainerSupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [views].[CurrencyView] AS [ContainerOrganizationCurrency] " +
                "ON [ContainerOrganizationCurrency].ID = [ContainerSupplyOrganizationAgreement].CurrencyID " +
                "AND [ContainerOrganizationCurrency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] AS [ContainerInvoiceDocument] " +
                "ON [ContainerInvoiceDocument].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrderContainerService] " +
                "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                "LEFT JOIN [User] AS [ContainerUser] " +
                "ON [ContainerUser].ID = [ContainerService].UserID " +
                "LEFT JOIN [PortWorkService] " +
                "ON [PortWorkService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [PortWorkOrganization] " +
                "ON [PortWorkOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [PortWorkOrganizationOrganization] " +
                "ON [PortWorkOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [PortWorkOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [PortWorkCurrency] " +
                "ON [PortWorkCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [PortWorkCurrency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] AS [PortWorkSupplyOrder] " +
                "ON [PortWorkSupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] AS [PortWorkInvoiceDocument] " +
                "ON [PortWorkInvoiceDocument].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [User] AS [PortWorkUser] " +
                "ON [PortWorkUser].ID = [PortWorkService].UserID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingContainerPaymentTask)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingVehicleService))) {
            Type[] joinTypes = {
                typeof(VehicleService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderVehicleService),
                typeof(SupplyOrder),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], VehicleService> joinMapper = objects => {
                VehicleService vehicleService = (VehicleService)objects[0];
                SupplyOrganization vehicleOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                SupplyOrderVehicleService junction = (SupplyOrderVehicleService)objects[7];
                SupplyOrder supplyOrder = (SupplyOrder)objects[8];
                User user = (User)objects[9];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow
                        .AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.AccountingVehicleService) && i.Id.Equals(vehicleService.Id));
                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.VehicleService != null) {
                    if (invoiceDocument != null && !itemFromList.VehicleService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.VehicleService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction == null || itemFromList.VehicleService.SupplyOrderVehicleServices.Any(j => j.Id.Equals(junction.Id)))
                        return vehicleService;

                    junction.SupplyOrder = supplyOrder;

                    itemFromList.VehicleService.SupplyOrderVehicleServices.Add(junction);
                } else {
                    itemFromList.Number = vehicleService.ServiceNumber;

                    if (invoiceDocument != null) vehicleService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction != null) {
                        junction.SupplyOrder = supplyOrder;

                        vehicleService.SupplyOrderVehicleServices.Add(junction);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = organization;
                    }

                    vehicleService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    vehicleService.VehicleOrganization = vehicleOrganization;
                    vehicleService.BillOfLadingDocument = billOfLadingDocument;
                    vehicleService.User = user;

                    itemFromList.VehicleService = vehicleService;
                }

                return vehicleService;
            };

            _connection.Query(
                "SELECT [VehicleService].[ID] " +
                ", [dbo].[GetGovExchangedToEuroValue](" +
                "[VehicleService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "[BillOfLadingDocument].[Date]" +
                ") AS [NetPrice] " +
                ",[VehicleService].[BillOfLadingDocumentID] " +
                ",[VehicleService].[Created] " +
                ",[VehicleService].[Deleted] " +
                ",[VehicleService].[IsActive] " +
                ",[VehicleService].[LoadDate] " +
                ",[VehicleService].[NetUID] " +
                ",[VehicleService].[TermDeliveryInDays] " +
                ",[VehicleService].[Updated] " +
                ",[VehicleService].[SupplyPaymentTaskID] " +
                ",[VehicleService].[UserID] " +
                ",[VehicleService].[VehicleOrganizationID] " +
                ",[VehicleService].[FromDate] " +
                ",[VehicleService].[GrossWeight] " +
                ", [dbo].[GetGovExchangedToEuroValue](" +
                "[VehicleService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "[BillOfLadingDocument].[Date]" +
                ") AS [GrossPrice] " +
                ",[VehicleService].[Vat] " +
                ",[VehicleService].[Number] " +
                ",[VehicleService].[Name] " +
                ",[VehicleService].[VatPercent] " +
                ",[VehicleService].[IsCalculatedExtraCharge] " +
                ",[VehicleService].[SupplyExtraChargeType] " +
                ",[VehicleService].[VehicleNumber] " +
                ",[VehicleService].[ServiceNumber] " +
                ",[VehicleService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[BillOfLadingDocument].* " +
                ",[InvoiceDocument].* " +
                ",[SupplyOrderVehicleService].* " +
                ",[SupplyOrder].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [VehicleService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [VehicleService].VehicleOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [VehicleService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].VehicleServiceID = [VehicleService].ID " +
                "LEFT JOIN [SupplyOrderVehicleService] " +
                "ON [SupplyOrderVehicleService].VehicleServiceID = [VehicleService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderVehicleService].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [VehicleService].UserID " +
                "WHERE [VehicleService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingVehicleService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingCustomService))) {
            Type[] joinTypes = {
                typeof(CustomService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganization),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency)
            };

            Func<object[], CustomService> joinMapper = objects => {
                CustomService customService = (CustomService)objects[0];
                SupplyOrganization customOrganization = (SupplyOrganization)objects[1];
                SupplyOrganization exciseDutyOrganization = (SupplyOrganization)objects[2];
                SupplyOrder supplyOrder = (SupplyOrder)objects[3];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[4];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                User user = (User)objects[7];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[8];
                Organization organization = (Organization)objects[9];
                Currency currency = (Currency)objects[10];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.AccountingCustomService) && i.Id.Equals(customService.Id));

                itemFromList.IsAccounting = true;

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.CustomService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.CustomService.ServiceNumber))
                        itemFromList.Number = itemFromList.CustomService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.CustomService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.CustomService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem == null || itemFromList.CustomService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id)))
                        return customService;

                    serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                    itemFromList.CustomService.ServiceDetailItems.Add(serviceDetailItem);
                } else {
                    if (customService != null && !string.IsNullOrEmpty(customService.ServiceNumber)) {
                        itemFromList.Number = customService.ServiceNumber;

                        if (invoiceDocument != null) customService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            customService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        customService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        customService.CustomOrganization = customOrganization;
                        customService.ExciseDutyOrganization = exciseDutyOrganization;
                        customService.SupplyOrder = supplyOrder;
                        customService.User = user;
                    }

                    itemFromList.CustomService = customService;
                }

                return customService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [CustomService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [CustomService].CustomOrganizationID " +
                "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
                "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [CustomService].SupplyOrderID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].CustomServiceID = [CustomService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].CustomServiceID = [CustomService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [CustomService].UserID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "WHERE [CustomService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingCustomService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingTransportationService))) {
            Type[] joinTypes = {
                typeof(TransportationService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], TransportationService> joinMapper = objects => {
                TransportationService transportationService = (TransportationService)objects[0];
                SupplyOrganization transportationOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.AccountingTransportationService) && i.Id.Equals(transportationService.Id));

                itemFromList.IsAccounting = true;

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.TransportationService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.TransportationService.ServiceNumber))
                        itemFromList.Number = itemFromList.TransportationService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.TransportationService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.TransportationService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.TransportationService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.TransportationService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.TransportationService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.TransportationService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (transportationService != null && !string.IsNullOrEmpty(transportationService.ServiceNumber)) {
                        itemFromList.Number = transportationService.ServiceNumber;

                        if (invoiceDocument != null) transportationService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            transportationService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) transportationService.SupplyOrders.Add(supplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        transportationService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        transportationService.TransportationOrganization = transportationOrganization;
                        transportationService.User = user;
                    }

                    itemFromList.TransportationService = transportationService;
                }

                return transportationService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [TransportationService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [TransportationService].TransportationOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [TransportationService].UserID " +
                "WHERE [TransportationService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingTransportationService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingPortCustomAgencyService))) {
            Type[] joinTypes = {
                typeof(PortCustomAgencyService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], PortCustomAgencyService> joinMapper = objects => {
                PortCustomAgencyService portCustomAgencyService = (PortCustomAgencyService)objects[0];
                SupplyOrganization portCustomAgencyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.AccountingPortCustomAgencyService) && i.Id.Equals(portCustomAgencyService.Id));

                itemFromList.IsAccounting = true;

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.PortCustomAgencyService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.PortCustomAgencyService.ServiceNumber))
                        itemFromList.Number = itemFromList.PortCustomAgencyService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.PortCustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PortCustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PortCustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PortCustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PortCustomAgencyService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PortCustomAgencyService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (portCustomAgencyService != null && !string.IsNullOrEmpty(portCustomAgencyService.ServiceNumber)) {
                        itemFromList.Number = portCustomAgencyService.ServiceNumber;

                        if (invoiceDocument != null) portCustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            portCustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) portCustomAgencyService.SupplyOrders.Add(supplyOrder);

                        if (portCustomAgencyOrganization != null) { }

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        portCustomAgencyService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        portCustomAgencyService.PortCustomAgencyOrganization = portCustomAgencyOrganization;
                        portCustomAgencyService.User = user;
                    }

                    itemFromList.PortCustomAgencyService = portCustomAgencyService;
                }

                return portCustomAgencyService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [PortCustomAgencyService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PortCustomAgencyService].UserID " +
                "WHERE [PortCustomAgencyService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingPortCustomAgencyService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingCustomAgencyService))) {
            Type[] joinTypes = {
                typeof(CustomAgencyService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], CustomAgencyService> joinMapper = objects => {
                CustomAgencyService customAgencyService = (CustomAgencyService)objects[0];
                SupplyOrganization customAgencyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.AccountingCustomAgencyService) && i.Id.Equals(customAgencyService.Id));

                itemFromList.IsAccounting = true;

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.CustomAgencyService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.CustomAgencyService.ServiceNumber))
                        itemFromList.Number = itemFromList.CustomAgencyService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.CustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.CustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.CustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.CustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.CustomAgencyService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.CustomAgencyService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (customAgencyService != null && !string.IsNullOrEmpty(customAgencyService.ServiceNumber)) {
                        itemFromList.Number = customAgencyService.ServiceNumber;

                        if (invoiceDocument != null) customAgencyService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            customAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) customAgencyService.SupplyOrders.Add(supplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        customAgencyService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        customAgencyService.CustomAgencyOrganization = customAgencyOrganization;
                        customAgencyService.User = user;
                    }

                    itemFromList.CustomAgencyService = customAgencyService;
                }

                return customAgencyService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [CustomAgencyService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [CustomAgencyService].UserID " +
                "WHERE [CustomAgencyService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingCustomAgencyService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingPlaneDeliveryService))) {
            Type[] joinTypes = {
                typeof(PlaneDeliveryService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], PlaneDeliveryService> joinMapper = objects => {
                PlaneDeliveryService planeDeliveryService = (PlaneDeliveryService)objects[0];
                SupplyOrganization planeDeliveryOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.AccountingPlaneDeliveryService) && i.Id.Equals(planeDeliveryService.Id));

                itemFromList.IsAccounting = true;

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.PlaneDeliveryService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.PlaneDeliveryService.ServiceNumber))
                        itemFromList.Number = itemFromList.PlaneDeliveryService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.PlaneDeliveryService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PlaneDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PlaneDeliveryService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PlaneDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PlaneDeliveryService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PlaneDeliveryService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (planeDeliveryService != null && !string.IsNullOrEmpty(planeDeliveryService.ServiceNumber)) {
                        itemFromList.Number = planeDeliveryService.ServiceNumber;

                        if (invoiceDocument != null) planeDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            planeDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) planeDeliveryService.SupplyOrders.Add(supplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        planeDeliveryService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        planeDeliveryService.PlaneDeliveryOrganization = planeDeliveryOrganization;
                        planeDeliveryService.User = user;
                    }

                    itemFromList.PlaneDeliveryService = planeDeliveryService;
                }

                return planeDeliveryService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [PlaneDeliveryService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PlaneDeliveryService].PlaneDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PlaneDeliveryService].UserID " +
                "WHERE [PlaneDeliveryService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingPlaneDeliveryService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingVehicleDeliveryService))) {
            Type[] joinTypes = {
                typeof(VehicleDeliveryService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], VehicleDeliveryService> joinMapper = objects => {
                VehicleDeliveryService vehicleDeliveryService = (VehicleDeliveryService)objects[0];
                SupplyOrganization vehicleDeliveryOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i =>
                        i.Type.Equals(JoinServiceType.AccountingVehicleDeliveryService) && i.Id.Equals(vehicleDeliveryService.Id));

                itemFromList.IsAccounting = true;

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.VehicleDeliveryService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.VehicleDeliveryService.ServiceNumber))
                        itemFromList.Number = itemFromList.VehicleDeliveryService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.VehicleDeliveryService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.VehicleDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.VehicleDeliveryService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.VehicleDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.VehicleDeliveryService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.VehicleDeliveryService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (vehicleDeliveryService != null && !string.IsNullOrEmpty(vehicleDeliveryService.ServiceNumber)) {
                        itemFromList.Number = vehicleDeliveryService.ServiceNumber;

                        if (invoiceDocument != null) vehicleDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            vehicleDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) vehicleDeliveryService.SupplyOrders.Add(supplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        vehicleDeliveryService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        vehicleDeliveryService.VehicleDeliveryOrganization = vehicleDeliveryOrganization;
                        vehicleDeliveryService.User = user;
                    }

                    itemFromList.VehicleDeliveryService = vehicleDeliveryService;
                }

                return vehicleDeliveryService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [VehicleDeliveryService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [VehicleDeliveryService].UserID " +
                "WHERE [VehicleDeliveryService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingVehicleDeliveryService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingMergedService))) {
            Type[] joinTypes = {
                typeof(MergedService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User),
                typeof(SupplyOrderUkraine),
                typeof(ConsumableProduct),
                typeof(ActProvidingService)
            };

            Func<object[], MergedService> joinMapper = objects => {
                MergedService mergedService = (MergedService)objects[0];
                SupplyOrganization mergedSupplyOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[10];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[11];
                ActProvidingService actProvidingService = (ActProvidingService)objects[12];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.AccountingMergedService) && i.Id.Equals(mergedService.Id));

                itemFromList.IsAccounting = true;

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.MergedService != null) {
                    itemFromList.Number = itemFromList.MergedService.ActProvidingService.Number;

                    itemFromList.MergedService.ConsumableProduct = consumableProduct;
                    itemFromList.MergedService.AccountingActProvidingService = actProvidingService;

                    if (invoiceDocument != null && !itemFromList.MergedService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.MergedService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem == null || itemFromList.MergedService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id)))
                        return mergedService;

                    serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                    itemFromList.MergedService.ServiceDetailItems.Add(serviceDetailItem);
                } else {
                    if (mergedService != null) {
                        if (actProvidingService != null)
                            itemFromList.Number = actProvidingService.Number;

                        mergedService.ConsumableProduct = consumableProduct;
                        mergedService.AccountingActProvidingService = actProvidingService;

                        if (invoiceDocument != null) mergedService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            mergedService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        mergedService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        mergedService.SupplyOrganization = mergedSupplyOrganization;
                        mergedService.SupplyOrderUkraine = supplyOrderUkraine;
                        mergedService.SupplyOrder = supplyOrder;
                        mergedService.User = user;
                    }

                    itemFromList.MergedService = mergedService;
                }

                return mergedService;
            };

            _connection.Query(
                "SELECT [MergedService].[ID] " +
                ",[MergedService].[Created] " +
                ",[MergedService].[Deleted] " +
                ",[MergedService].[FromDate] " +
                ",[MergedService].[IsActive] " +
                ",[MergedService].[NetUID] " +
                ",[MergedService].[SupplyPaymentTaskID] " +
                ",[MergedService].[Updated] " +
                ",[MergedService].[UserID] " +
                ",[MergedService].[SupplyOrganizationID] " +
                ",[MergedService].[Vat] " +
                ",[MergedService].[AccountingVat] " +
                ",[MergedService].[Number] " +
                ",[MergedService].[DeliveryProductProtocolID] " +
                ",[MergedService].[Name] " +
                ",[MergedService].[VatPercent] " +
                ",[MergedService].[AccountingVatPercent] " +
                ",[MergedService].[ServiceNumber] " +
                ",[MergedService].[SupplyOrganizationAgreementID] " +
                ",[MergedService].[ConsumableProductID] " +
                ", [MergedService].[GrossPrice] " +
                ", [MergedService].[AccountingGrossPrice] " +
                ", [MergedService].[NetPrice] " +
                ", [MergedService].[AccountingNetPrice] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[SupplyOrder].* " +
                ",[ServiceDetailItem].* " +
                ",[ServiceDetailItemKey].* " +
                ",[InvoiceDocument].* " +
                ",[User].* " +
                ",[SupplyOrderUkraine].* " +
                ",[ConsumableProduct].* " +
                ",[ActProvidingService].* " +
                "FROM [MergedService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [MergedService].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [MergedService].SupplyOrderID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].MergedServiceID = [MergedService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].MergedServiceID = [MergedService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [MergedService].UserID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [MergedService].SupplyOrderUkraineID " +
                "LEFT JOIN [ConsumableProduct] " +
                "ON [ConsumableProduct].[ID] = [MergedService].[ConsumableProductID] " +
                "LEFT JOIN [ActProvidingService] " +
                "ON [ActProvidingService].[ID] = [MergedService].[AccountingActProvidingServiceID] " +
                "WHERE [MergedService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingMergedService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingContainerService))) {
            Type[] joinTypes = {
                typeof(ContainerService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(InvoiceDocument),
                typeof(SupplyOrderContainerService),
                typeof(SupplyOrder),
                typeof(User)
            };

            Func<object[], ContainerService> joinMapper = objects => {
                ContainerService containerService = (ContainerService)objects[0];
                SupplyOrganization containerOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                SupplyOrderContainerService junction = (SupplyOrderContainerService)objects[7];
                SupplyOrder supplyOrder = (SupplyOrder)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.AccountingContainerService) && i.Id.Equals(containerService.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.ContainerService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.ContainerService.ContainerNumber))
                        itemFromList.Number = itemFromList.ContainerService.ContainerNumber;

                    if (invoiceDocument != null && !itemFromList.ContainerService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.ContainerService.InvoiceDocuments.Add(invoiceDocument);

                    if (junction == null || itemFromList.ContainerService.SupplyOrderContainerServices.Any(j => j.Id.Equals(junction.Id)))
                        return containerService;

                    junction.SupplyOrder = supplyOrder;

                    itemFromList.ContainerService.SupplyOrderContainerServices.Add(junction);
                } else {
                    if (containerService != null && !string.IsNullOrEmpty(containerService.ContainerNumber)) {
                        itemFromList.Number = containerService.ContainerNumber;

                        if (invoiceDocument != null) containerService.InvoiceDocuments.Add(invoiceDocument);

                        if (junction != null) {
                            junction.SupplyOrder = supplyOrder;

                            containerService.SupplyOrderContainerServices.Add(junction);
                        }

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        containerService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        containerService.ContainerOrganization = containerOrganization;
                        containerService.BillOfLadingDocument = billOfLadingDocument;
                        containerService.User = user;
                    }

                    itemFromList.ContainerService = containerService;
                }

                return containerService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ContainerService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrderContainerService] " +
                "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ContainerService].UserID " +
                "WHERE [ContainerService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingContainerService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingPortWorkService))) {
            Type[] joinTypes = {
                typeof(PortWorkService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(SupplyOrder),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(InvoiceDocument),
                typeof(User)
            };

            Func<object[], PortWorkService> joinMapper = objects => {
                PortWorkService portWorkService = (PortWorkService)objects[0];
                SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[6];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                User user = (User)objects[9];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow.AccountingCashFlowHeadItems.First(i => i.Type.Equals(JoinServiceType.AccountingPortWorkService) && i.Id.Equals(portWorkService.Id));

                itemFromList.OrganizationName = organization?.Name ?? "";

                if (itemFromList.PortWorkService != null) {
                    if (!string.IsNullOrEmpty(itemFromList.PortWorkService.ServiceNumber))
                        itemFromList.Number = itemFromList.PortWorkService.ServiceNumber;

                    if (invoiceDocument != null && !itemFromList.PortWorkService.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        itemFromList.PortWorkService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !itemFromList.PortWorkService.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        itemFromList.PortWorkService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder != null && !itemFromList.PortWorkService.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id)))
                        itemFromList.PortWorkService.SupplyOrders.Add(supplyOrder);
                } else {
                    if (portWorkService != null && !string.IsNullOrEmpty(portWorkService.ServiceNumber)) {
                        itemFromList.Number = portWorkService.ServiceNumber;

                        if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder != null) portWorkService.SupplyOrders.Add(supplyOrder);

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = organization;
                        }

                        portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        portWorkService.PortWorkOrganization = portWorkOrganization;
                        portWorkService.User = user;
                    }

                    itemFromList.PortWorkService = portWorkService;
                }

                return portWorkService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [PortWorkService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PortWorkService].UserID " +
                "WHERE [PortWorkService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingPortWorkService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.BillOfLadingService))) {
            Type[] joinTypes = {
                typeof(BillOfLadingService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(SupplyInvoiceBillOfLadingService),
                typeof(SupplyInvoice),
                typeof(InvoiceDocument),
                typeof(SupplyOrder),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], BillOfLadingService> joinMapper = objects => {
                BillOfLadingService service = (BillOfLadingService)objects[0];
                SupplyOrganization supplyOrganizationService = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                SupplyInvoiceBillOfLadingService supplyInvoiceBillOfLadingService = (SupplyInvoiceBillOfLadingService)objects[6];
                SupplyInvoice invoice = (SupplyInvoice)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                User user = (User)objects[10];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[11];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow
                        .AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.BillOfLadingService) && i.Id.Equals(service.Id));
                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                itemFromList.Number = service.ServiceNumber;

                if (itemFromList.BillOfLadingService == null)
                    itemFromList.BillOfLadingService = service;
                else
                    service = itemFromList.BillOfLadingService;

                supplyOrganizationAgreement.Organization = organization;
                supplyOrganizationAgreement.Currency = currency;
                service.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                service.SupplyOrganization = supplyOrganizationService;
                service.User = user;

                if (supplyInvoiceBillOfLadingService != null) {
                    if (!service.SupplyInvoiceBillOfLadingServices.Any(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id)))
                        service.SupplyInvoiceBillOfLadingServices.Add(supplyInvoiceBillOfLadingService);
                    else
                        supplyInvoiceBillOfLadingService = service.SupplyInvoiceBillOfLadingServices.First(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id));

                    invoice.SupplyOrder = supplyOrder;

                    supplyInvoiceBillOfLadingService.SupplyInvoice = invoice;

                    if (invoiceDocument != null)
                        invoice.InvoiceDocuments.Add(invoiceDocument);
                }

                if (billOfLadingDocument == null) return service;

                if (!service.BillOfLadingDocuments.Any(x => x.Id.Equals(billOfLadingDocument.Id)))
                    service.BillOfLadingDocuments.Add(billOfLadingDocument);
                else
                    billOfLadingDocument = service.BillOfLadingDocuments.First(x => x.Id.Equals(billOfLadingDocument.Id));

                return service;
            };

            _connection.Query(
                "SELECT [BillOfLadingService].[ID] " +
                ", [dbo].[GetGovExchangedToEuroValue](" +
                "[BillOfLadingService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "[BillOfLadingDocument].[Date]" +
                ") AS [NetPrice] " +
                ",[BillOfLadingService].[Created] " +
                ",[BillOfLadingService].[Deleted] " +
                ",[BillOfLadingService].[IsActive] " +
                ",[BillOfLadingService].[LoadDate] " +
                ",[BillOfLadingService].[NetUID] " +
                ",[BillOfLadingService].[TermDeliveryInDays] " +
                ",[BillOfLadingService].[Updated] " +
                ",[BillOfLadingService].[SupplyPaymentTaskID] " +
                ",[BillOfLadingService].[AccountingPaymentTaskID] " +
                ",[BillOfLadingService].[UserID] " +
                ",[BillOfLadingService].[SupplyOrganizationID] " +
                ",[BillOfLadingService].[FromDate] " +
                ", [dbo].[GetGovExchangedToEuroValue](" +
                "[BillOfLadingService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "[BillOfLadingDocument].[Date]" +
                ") AS [GrossPrice] " +
                ",[BillOfLadingService].[Vat] " +
                ",[BillOfLadingService].[Number] " +
                ",[BillOfLadingService].[Name] " +
                ",[BillOfLadingService].[VatPercent] " +
                ",[BillOfLadingService].[BillOfLadingNumber] " +
                ",[BillOfLadingService].[ServiceNumber] " +
                ",[BillOfLadingService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[BillOfLadingDocument].* " +
                ",[SupplyInvoiceBillOfLadingService].* " +
                ",[SupplyInvoice].* " +
                ",[InvoiceDocument].* " +
                ",[SupplyOrder].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [BillOfLadingService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [BillOfLadingService].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [BillOfLadingService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].[BillOfLadingServiceID] = [BillOfLadingService].[ID] " +
                "LEFT JOIN [SupplyInvoiceBillOfLadingService] " +
                "ON [SupplyInvoiceBillOfLadingService].BillOfLadingServiceID = [BillOfLadingService].ID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyInvoiceBillOfLadingService].SupplyInvoiceID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [BillOfLadingService].UserID " +
                "WHERE [BillOfLadingService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.BillOfLadingService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.AccountingBillOfLadingService))) {
            Type[] joinTypes = {
                typeof(BillOfLadingService),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(SupplyInvoiceBillOfLadingService),
                typeof(SupplyInvoice),
                typeof(InvoiceDocument),
                typeof(SupplyOrder),
                typeof(User),
                typeof(OrganizationTranslation)
            };

            Func<object[], BillOfLadingService> joinMapper = objects => {
                BillOfLadingService service = (BillOfLadingService)objects[0];
                SupplyOrganization supplyOrganizationService = (SupplyOrganization)objects[1];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
                Organization organization = (Organization)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                SupplyInvoiceBillOfLadingService supplyInvoiceBillOfLadingService = (SupplyInvoiceBillOfLadingService)objects[6];
                SupplyInvoice invoice = (SupplyInvoice)objects[7];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[8];
                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                User user = (User)objects[10];
                OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[11];

                AccountingCashFlowHeadItem itemFromList =
                    accountingCashFlow
                        .AccountingCashFlowHeadItems
                        .First(i => i.Type.Equals(JoinServiceType.AccountingBillOfLadingService) && i.Id.Equals(service.Id));

                itemFromList.IsAccounting = true;

                itemFromList.Number = service.ServiceNumber;

                if (organizationTranslation != null)
                    itemFromList.OrganizationName = organizationTranslation.Name;

                if (itemFromList.BillOfLadingService == null)
                    itemFromList.BillOfLadingService = service;
                else
                    service = itemFromList.BillOfLadingService;

                supplyOrganizationAgreement.Organization = organization;
                supplyOrganizationAgreement.Currency = currency;
                service.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                service.SupplyOrganization = supplyOrganizationService;
                service.User = user;

                if (supplyInvoiceBillOfLadingService != null) {
                    if (!service.SupplyInvoiceBillOfLadingServices.Any(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id)))
                        service.SupplyInvoiceBillOfLadingServices.Add(supplyInvoiceBillOfLadingService);
                    else
                        supplyInvoiceBillOfLadingService = service.SupplyInvoiceBillOfLadingServices.First(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id));

                    invoice.SupplyOrder = supplyOrder;

                    supplyInvoiceBillOfLadingService.SupplyInvoice = invoice;

                    if (invoiceDocument != null)
                        invoice.InvoiceDocuments.Add(invoiceDocument);
                }

                if (billOfLadingDocument == null) return service;

                if (!service.BillOfLadingDocuments.Any(x => x.Id.Equals(billOfLadingDocument.Id)))
                    service.BillOfLadingDocuments.Add(billOfLadingDocument);
                else
                    billOfLadingDocument = service.BillOfLadingDocuments.First(x => x.Id.Equals(billOfLadingDocument.Id));

                return service;
            };

            _connection.Query(
                "SELECT [BillOfLadingService].[ID] " +
                ", [dbo].[GetGovExchangedToEuroValue](" +
                "[BillOfLadingService].[NetPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "[BillOfLadingDocument].[Date]" +
                ") AS [NetPrice] " +
                ",[BillOfLadingService].[Created] " +
                ",[BillOfLadingService].[Deleted] " +
                ",[BillOfLadingService].[IsActive] " +
                ",[BillOfLadingService].[LoadDate] " +
                ",[BillOfLadingService].[NetUID] " +
                ",[BillOfLadingService].[TermDeliveryInDays] " +
                ",[BillOfLadingService].[Updated] " +
                ",[BillOfLadingService].[SupplyPaymentTaskID] " +
                ",[BillOfLadingService].[AccountingPaymentTaskID] " +
                ",[BillOfLadingService].[UserID] " +
                ",[BillOfLadingService].[SupplyOrganizationID] " +
                ",[BillOfLadingService].[FromDate] " +
                ", [dbo].[GetGovExchangedToEuroValue](" +
                "[BillOfLadingService].[GrossPrice], " +
                "[SupplyOrganizationAgreement].CurrencyID, " +
                "[BillOfLadingDocument].[Date]" +
                ") AS [GrossPrice] " +
                ",[BillOfLadingService].[Vat] " +
                ",[BillOfLadingService].[Number] " +
                ",[BillOfLadingService].[Name] " +
                ",[BillOfLadingService].[VatPercent] " +
                ",[BillOfLadingService].[BillOfLadingNumber] " +
                ",[BillOfLadingService].[ServiceNumber] " +
                ",[BillOfLadingService].[SupplyOrganizationAgreementID] " +
                ",[SupplyOrganization].* " +
                ",[SupplyOrganizationAgreement].* " +
                ",[Organization].* " +
                ",[Currency].* " +
                ",[BillOfLadingDocument].* " +
                ",[SupplyInvoiceBillOfLadingService].* " +
                ",[SupplyInvoice].* " +
                ",[InvoiceDocument].* " +
                ",[SupplyOrder].* " +
                ",[User].* " +
                ",[OrganizationTranslation].* " +
                "FROM [BillOfLadingService] " +
                "LEFT JOIN [SupplyOrganization] " +
                "ON [SupplyOrganization].ID = [BillOfLadingService].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [BillOfLadingService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "LEFT JOIN [OrganizationTranslation] " +
                "ON [OrganizationTranslation].[OrganizationID] = [Organization].[ID] " +
                "AND [OrganizationTranslation].[Deleted] = 0 " +
                "AND [OrganizationTranslation].[CultureCode] = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].[BillOfLadingServiceID] = [BillOfLadingService].[ID] " +
                "LEFT JOIN [SupplyInvoiceBillOfLadingService] " +
                "ON [SupplyInvoiceBillOfLadingService].BillOfLadingServiceID = [BillOfLadingService].ID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyInvoiceBillOfLadingService].SupplyInvoiceID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [BillOfLadingService].UserID " +
                "WHERE [BillOfLadingService].ID IN @Ids",
                joinTypes,
                joinMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.AccountingBillOfLadingService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        return accountingCashFlow;
    }
}