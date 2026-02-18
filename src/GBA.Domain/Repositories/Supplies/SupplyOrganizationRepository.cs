using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyOrganizationRepository : ISupplyOrganizationRepository {
    private readonly IDbConnection _connection;

    public SupplyOrganizationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyOrganization supplyOrganization) {
        return _connection.Query<long>(
                "INSERT INTO [SupplyOrganization] " +
                "(Name, Address, PhoneNumber, EmailAddress, Requisites, Swift, SwiftBic, IntermediaryBank, BeneficiaryBank, AccountNumber, " +
                "Beneficiary, Bank, BankAccount, NIP, BankAccountPLN, BankAccountEUR, ContactPersonName, ContactPersonPhone, ContactPersonEmail, ContactPersonViber, " +
                "ContactPersonSkype, ContactPersonComment, IsAgreementReceived, IsBillReceived, AgreementReceiveDate, BillReceiveDate, Updated, [IsNotResident], " +
                "TIN, USREOU, SROI) " +
                "VALUES (@Name, @Address, @PhoneNumber, @EmailAddress, @Requisites, @Swift, @SwiftBic, @IntermediaryBank, @BeneficiaryBank, @AccountNumber, " +
                "@Beneficiary, @Bank, @BankAccount, @NIP, @BankAccountPLN, @BankAccountEUR, @ContactPersonName, @ContactPersonPhone, @ContactPersonEmail, " +
                "@ContactPersonViber, @ContactPersonSkype, @ContactPersonComment, @IsAgreementReceived, @IsBillReceived, @AgreementReceiveDate, " +
                "@BillReceiveDate, GETUTCDATE(), @IsNotResident, @TIN, @USREOU, @SROI); " +
                "SELECT SCOPE_IDENTITY()",
                supplyOrganization
            )
            .Single();
    }

    public void Update(SupplyOrganization supplyOrganization) {
        _connection.Execute(
            "UPDATE [SupplyOrganization] " +
            "SET Name = @Name, Address = @Address, PhoneNumber = @PhoneNumber, EmailAddress = @EmailAddress, Requisites = @Requisites, Swift = @Swift, " +
            "SwiftBic = @SwiftBic, IntermediaryBank = @IntermediaryBank, BeneficiaryBank = @BeneficiaryBank, AccountNumber = @AccountNumber, Beneficiary = @Beneficiary, " +
            "Bank = @Bank, BankAccount = @BankAccount, NIP = @NIP, BankAccountPLN = @BankAccountPLN, BankAccountEUR = @BankAccountEUR, " +
            "ContactPersonName = @ContactPersonName, ContactPersonPhone = @ContactPersonPhone, ContactPersonEmail = @ContactPersonEmail, " +
            "ContactPersonViber = @ContactPersonViber, ContactPersonSkype = @ContactPersonSkype, ContactPersonComment = @ContactPersonComment, " +
            "IsAgreementReceived = @IsAgreementReceived, IsBillReceived = @IsBillReceived, " +
            "AgreementReceiveDate = @AgreementReceiveDate, BillReceiveDate = @BillReceiveDate, Updated = GETUTCDATE(), [IsNotResident] = @IsNotResident " +
            "SROI = @SROI, TIN = @TIN, SROI = @SROI " +
            "WHERE [SupplyOrganization].ID = @Id",
            supplyOrganization
        );
    }

    public SupplyOrganization GetById(long id) {
        SupplyOrganization toReturn = null;

        _connection.Query<SupplyOrganization, SupplyOrganizationAgreement, Organization, Currency, SupplyOrganizationDocument, SupplyOrganization>(
            "SELECT * " +
            "FROM [SupplyOrganization] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].SupplyOrganizationID = [SupplyOrganization].ID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
            "ON [AgreementCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [AgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrganizationDocument] " +
            "ON [SupplyOrganizationDocument].SupplyOrganizationAgreementID = [SupplyOrganizationAgreement].ID " +
            "AND [SupplyOrganizationDocument].Deleted = 0 " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [SupplyOrganization].ID = @Id",
            (supplyOrganization, agreement, organization, agreementCurrency, document) => {
                if (toReturn != null) {
                    if (agreement != null) {
                        if (!toReturn.SupplyOrganizationAgreements.Any(a => a.Id.Equals(agreement.Id)))
                            toReturn.SupplyOrganizationAgreements.Add(agreement);
                        else
                            agreement = toReturn.SupplyOrganizationAgreements.First(a => a.Id.Equals(agreement.Id));

                        agreement.Currency = agreementCurrency;

                        agreement.Organization = organization;

                        if (document != null)
                            agreement.SupplyOrganizationDocuments.Add(document);
                    }
                } else {
                    if (agreement != null) {
                        agreement.Currency = agreementCurrency;

                        agreement.Organization = organization;

                        if (document != null)
                            agreement.SupplyOrganizationDocuments.Add(document);

                        supplyOrganization.SupplyOrganizationAgreements.Add(agreement);
                    }

                    toReturn = supplyOrganization;
                }

                return supplyOrganization;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Id = id }
        );

        return toReturn;
    }

    public SupplyOrganization GetByNetId(Guid netId) {
        SupplyOrganization toReturn = null;

        _connection.Query<SupplyOrganization, SupplyOrganizationAgreement, Organization, Currency, SupplyOrganizationDocument, SupplyOrganization>(
            "SELECT * " +
            "FROM [SupplyOrganization] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].SupplyOrganizationID = [SupplyOrganization].ID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
            "ON [AgreementCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [AgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrganizationDocument] " +
            "ON [SupplyOrganizationDocument].SupplyOrganizationAgreementID = [SupplyOrganizationAgreement].ID " +
            "AND [SupplyOrganizationDocument].Deleted = 0 " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [SupplyOrganization].NetUID = @NetId",
            (supplyOrganization, agreement, organization, agreementCurrency, document) => {
                if (toReturn != null) {
                    if (agreement != null) {
                        if (!toReturn.SupplyOrganizationAgreements.Any(a => a.Id.Equals(agreement.Id)))
                            toReturn.SupplyOrganizationAgreements.Add(agreement);
                        else
                            agreement = toReturn.SupplyOrganizationAgreements.First(a => a.Id.Equals(agreement.Id));

                        agreement.Currency = agreementCurrency;

                        agreement.Organization = organization;

                        if (document != null)
                            agreement.SupplyOrganizationDocuments.Add(document);
                    }
                } else {
                    if (agreement != null) {
                        agreement.Currency = agreementCurrency;

                        agreement.Organization = organization;

                        if (document != null)
                            agreement.SupplyOrganizationDocuments.Add(document);

                        supplyOrganization.SupplyOrganizationAgreements.Add(agreement);
                    }

                    toReturn = supplyOrganization;
                }

                return supplyOrganization;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, NetId = netId }
        );

        return toReturn;
    }

    public List<SupplyOrganization> GetAll(Guid? organizationNetId) {
        List<SupplyOrganization> supplyOrganizations = new();


        string sqlExpression =
            ";WITH [AccountingCashFlow_CTE] " +
            "AS " +
            "( " +
            "SELECT [ContainerService].ContainerOrganizationID AS ID " +
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
            "WHERE [ContainerService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortWorkService].PortWorkOrganizationID AS ID " +
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
            "WHERE [PortWorkService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleService].VehicleOrganizationID AS ID " +
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
            "WHERE [VehicleService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [BillOfLadingService].SupplyOrganizationID AS ID " +
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
            "WHERE [BillOfLadingService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT ISNULL([CustomService].CustomOrganizationID, [CustomService].ExciseDutyOrganizationID) AS ID " +
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
            "WHERE [CustomService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [TransportationService].TransportationOrganizationID AS ID " +
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
            "WHERE [TransportationService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortCustomAgencyService].PortCustomAgencyOrganizationID AS ID " +
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
            "WHERE [PortCustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [CustomAgencyService].CustomAgencyOrganizationID AS ID " +
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
            "WHERE [CustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PlaneDeliveryService].PlaneDeliveryOrganizationID AS ID " +
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
            "WHERE [PlaneDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleDeliveryService].VehicleDeliveryOrganizationID AS ID " +
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
            "WHERE [VehicleDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [MergedService].SupplyOrganizationID AS ID " +
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
            "WHERE [MergedService].Deleted = 0 " +
            "AND [MergedService].[ActProvidingServiceID] IS NOT NULL " +
            "UNION ALL " +
            "SELECT [DeliveryExpense].SupplyOrganizationID AS ID " +
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
            "WHERE [DeliveryExpense].Deleted = 0 " +
            "AND [DeliveryExpense].ActProvidingServiceID IS NOT NULL " +
            "UNION ALL " +
            "SELECT [ConsumablesOrderItem].ConsumableProductOrganizationID AS ID " +
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
            "WHERE [ConsumablesOrderItem].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [OutcomePaymentOrder].ConsumableProductOrganizationID AS [ID] " +
            ", -[dbo].[GetExchangedToEuroValue] " +
            "( " +
            "[OutcomePaymentOrder].AfterExchangeAmount, " +
            "[SupplyOrganizationAgreement].CurrencyID, " +
            "GETUTCDATE() " +
            ") AS [GrossPrice] " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
            "WHERE [OutcomePaymentOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrder].IsCanceled = 0 " +
            "AND ([OutcomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
            "OR [OutcomePaymentOrder].ClientAgreementID IS NOT NULL) " +
            "UNION ALL " +
            "SELECT [ContainerService].ContainerOrganizationID AS ID " +
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
            "WHERE [ContainerService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortWorkService].PortWorkOrganizationID AS ID " +
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
            "WHERE [PortWorkService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleService].VehicleOrganizationID AS ID " +
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
            "WHERE [VehicleService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [BillOfLadingService].SupplyOrganizationID AS ID " +
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
            "WHERE [BillOfLadingService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT ISNULL([CustomService].CustomOrganizationID, [CustomService].ExciseDutyOrganizationID) AS ID " +
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
            "WHERE [CustomService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [TransportationService].TransportationOrganizationID AS ID " +
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
            "WHERE [TransportationService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortCustomAgencyService].PortCustomAgencyOrganizationID AS ID " +
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
            "WHERE [PortCustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [CustomAgencyService].CustomAgencyOrganizationID AS ID " +
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
            "WHERE [CustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PlaneDeliveryService].PlaneDeliveryOrganizationID AS ID " +
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
            "WHERE [PlaneDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleDeliveryService].VehicleDeliveryOrganizationID AS ID " +
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
            "WHERE [VehicleDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [MergedService].SupplyOrganizationID AS ID " +
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
            "WHERE [MergedService].Deleted = 0 " +
            "AND [MergedService].[AccountingActProvidingServiceID] IS NOT NULL " +
            "UNION ALL " +
            "SELECT [DeliveryExpense].SupplyOrganizationID AS ID " +
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
            "WHERE [DeliveryExpense].Deleted = 0 " +
            "AND [DeliveryExpense].AccountingActProvidingServiceID IS NOT NULL " +
            "UNION ALL " +
            "SELECT [SupplyOrganizationAgreement].SupplyOrganizationID AS [ID], " +
            "[IncomePaymentOrder].EuroAmount AS [GrossPrice] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [IncomePaymentOrder].SupplyOrganizationAgreementID " +
            "WHERE [IncomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
            "AND [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 " +
            "), " +
            "[AccountingCashFlow_CTE_R] AS ( " +
            "SELECT " +
            "[AccountingCashFlow_CTE].[ID] " +
            ", SUM([AccountingCashFlow_CTE].[GrossPrice]) [TotalCurrentAmount] " +
            "FROM [AccountingCashFlow_CTE] " +
            "GROUP BY [AccountingCashFlow_CTE].[ID] " +
            ") " +
            "SELECT " +
            "[SupplyOrganization].* " +
            ", IIF([AccountingCashFlow_CTE_R].[TotalCurrentAmount] IS NULL, 0, [AccountingCashFlow_CTE_R].[TotalCurrentAmount]) [TotalAgreementsCurrentEuroAmount] " +
            ", [SupplyOrganizationAgreement].* " +
            ", [SupplyOrganizationDocument].* " +
            ", [Organization].* " +
            ", [AgreementCurrency].* " +
            "FROM [SupplyOrganization] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].SupplyOrganizationID = [SupplyOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationDocument] " +
            "ON [SupplyOrganizationDocument].SupplyOrganizationAgreementID = [SupplyOrganizationAgreement].ID " +
            "AND [SupplyOrganizationDocument].Deleted = 0 " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
            "ON [AgreementCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [AgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [AccountingCashFlow_CTE_R] " +
            "ON [AccountingCashFlow_CTE_R].[ID] = [SupplyOrganization].[ID] " +
            "WHERE [SupplyOrganization].Deleted = 0 ";

        if (organizationNetId.HasValue) sqlExpression += "AND [Organization].NetUID = @OrganizationNetId ";

        sqlExpression += "ORDER BY [SupplyOrganization].[Name]";

        _connection.Query<SupplyOrganization, SupplyOrganizationAgreement, SupplyOrganizationDocument, Organization, Currency, SupplyOrganization>(
            sqlExpression,
            (supplyOrganization, agreement, document, organization, agreementCurrency) => {
                if (supplyOrganizations.Any(o => o.Id.Equals(supplyOrganization.Id))) {
                    SupplyOrganization fromList = supplyOrganizations.First(o => o.Id.Equals(supplyOrganization.Id));

                    if (agreement != null) {
                        if (!fromList.SupplyOrganizationAgreements.Any(a => a.Id.Equals(agreement.Id)))
                            fromList.SupplyOrganizationAgreements.Add(agreement);
                        else
                            agreement = fromList.SupplyOrganizationAgreements.First(a => a.Id.Equals(agreement.Id));

                        agreement.Currency = agreementCurrency;

                        agreement.Organization = organization;

                        if (document != null) agreement.SupplyOrganizationDocuments.Add(document);
                    }
                } else {
                    if (agreement != null) {
                        agreement.Currency = agreementCurrency;

                        agreement.Organization = organization;

                        if (document != null)
                            agreement.SupplyOrganizationDocuments.Add(document);

                        supplyOrganization.SupplyOrganizationAgreements.Add(agreement);
                    }

                    supplyOrganizations.Add(supplyOrganization);
                }

                return supplyOrganization;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, OrganizationNetId = organizationNetId
            }
        );

        return supplyOrganizations;
    }

    public List<SupplyOrganization> GetAllFromSearchFiltered(string value, long limit, long offset) {
        List<SupplyOrganization> supplyOrganizations = new();

        IEnumerable<long> ids = _connection.Query<long>(
            ";WITH [SEARCHED_CTE] AS ( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Name]) AS RowNumber, " +
            "[SupplyOrganization].[ID] " +
            "FROM [SupplyOrganization] " +
            "WHERE [SupplyOrganization].Deleted = 0 " +
            "AND [SupplyOrganization].[Name] like '%' + @Value + '%' " +
            ") " +
            "SELECT " +
            "[SEARCHED_CTE].ID " +
            "FROM [SEARCHED_CTE] " +
            "WHERE [SEARCHED_CTE].RowNumber > @Offset " +
            "AND [SEARCHED_CTE].RowNumber <= @Limit + @Offset ",
            new {
                Value = value,
                Offset = offset,
                Limit = limit
            });

        string sqlExpression =
            ";WITH [AccountingCashFlow_CTE] " +
            "AS " +
            "( " +
            "SELECT [ContainerService].ContainerOrganizationID AS ID " +
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
            "WHERE [ContainerService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortWorkService].PortWorkOrganizationID AS ID " +
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
            "WHERE [PortWorkService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleService].VehicleOrganizationID AS ID " +
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
            "WHERE [VehicleService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [BillOfLadingService].SupplyOrganizationID AS ID " +
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
            "WHERE [BillOfLadingService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT ISNULL([CustomService].CustomOrganizationID, [CustomService].ExciseDutyOrganizationID) AS ID " +
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
            "WHERE [CustomService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [TransportationService].TransportationOrganizationID AS ID " +
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
            "WHERE [TransportationService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortCustomAgencyService].PortCustomAgencyOrganizationID AS ID " +
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
            "WHERE [PortCustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [CustomAgencyService].CustomAgencyOrganizationID AS ID " +
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
            "WHERE [CustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PlaneDeliveryService].PlaneDeliveryOrganizationID AS ID " +
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
            "WHERE [PlaneDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleDeliveryService].VehicleDeliveryOrganizationID AS ID " +
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
            "WHERE [VehicleDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [MergedService].SupplyOrganizationID AS ID " +
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
            "WHERE [MergedService].Deleted = 0 " +
            "AND [MergedService].[ActProvidingServiceID] IS NOT NULL " +
            "UNION ALL " +
            "SELECT [DeliveryExpense].SupplyOrganizationID AS ID " +
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
            "WHERE [DeliveryExpense].Deleted = 0 " +
            "AND [DeliveryExpense].ActProvidingServiceID IS NOT NULL " +
            "UNION ALL " +
            "SELECT [ConsumablesOrderItem].ConsumableProductOrganizationID AS ID " +
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
            "WHERE [ConsumablesOrderItem].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [OutcomePaymentOrder].ConsumableProductOrganizationID AS [ID] " +
            ", -[dbo].[GetExchangedToEuroValue] " +
            "( " +
            "[OutcomePaymentOrder].AfterExchangeAmount, " +
            "[SupplyOrganizationAgreement].CurrencyID, " +
            "GETUTCDATE() " +
            ") AS [GrossPrice] " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
            "WHERE [OutcomePaymentOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrder].IsCanceled = 0 " +
            "AND ([OutcomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
            "OR [OutcomePaymentOrder].ClientAgreementID IS NOT NULL) " +
            "UNION ALL " +
            "SELECT [ContainerService].ContainerOrganizationID AS ID " +
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
            "WHERE [ContainerService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortWorkService].PortWorkOrganizationID AS ID " +
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
            "WHERE [PortWorkService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleService].VehicleOrganizationID AS ID " +
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
            "WHERE [VehicleService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [BillOfLadingService].SupplyOrganizationID AS ID " +
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
            "WHERE [BillOfLadingService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT ISNULL([CustomService].CustomOrganizationID, [CustomService].ExciseDutyOrganizationID) AS ID " +
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
            "WHERE [CustomService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [TransportationService].TransportationOrganizationID AS ID " +
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
            "WHERE [TransportationService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortCustomAgencyService].PortCustomAgencyOrganizationID AS ID " +
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
            "WHERE [PortCustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [CustomAgencyService].CustomAgencyOrganizationID AS ID " +
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
            "WHERE [CustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PlaneDeliveryService].PlaneDeliveryOrganizationID AS ID " +
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
            "WHERE [PlaneDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleDeliveryService].VehicleDeliveryOrganizationID AS ID " +
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
            "WHERE [VehicleDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [MergedService].SupplyOrganizationID AS ID " +
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
            "WHERE [MergedService].Deleted = 0 " +
            "AND [MergedService].[AccountingActProvidingServiceID] IS NOT NULL " +
            "UNION ALL " +
            "SELECT [DeliveryExpense].SupplyOrganizationID AS ID " +
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
            "WHERE [DeliveryExpense].Deleted = 0 " +
            "AND [DeliveryExpense].AccountingActProvidingServiceID IS NOT NULL " +
            "UNION ALL " +
            "SELECT [SupplyOrganizationAgreement].SupplyOrganizationID, " +
            "[IncomePaymentOrder].EuroAmount AS [GrossPrice] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [IncomePaymentOrder].SupplyOrganizationAgreementID " +
            "WHERE [IncomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
            "AND [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 " +
            "), " +
            "[AccountingCashFlow_CTE_R] AS ( " +
            "SELECT " +
            "[AccountingCashFlow_CTE].[ID] " +
            ", SUM([AccountingCashFlow_CTE].[GrossPrice]) [TotalCurrentAmount] " +
            "FROM [AccountingCashFlow_CTE] " +
            "GROUP BY [AccountingCashFlow_CTE].[ID] " +
            ") " +
            "SELECT " +
            "[SupplyOrganization].* " +
            ", IIF([AccountingCashFlow_CTE_R].[TotalCurrentAmount] IS NULL, 0, [AccountingCashFlow_CTE_R].[TotalCurrentAmount]) [TotalAgreementsCurrentEuroAmount] " +
            ", [SupplyOrganizationAgreement].* " +
            ", [SupplyOrganizationDocument].* " +
            ", [Organization].* " +
            ", [AgreementCurrency].* " +
            "FROM [SupplyOrganization] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].SupplyOrganizationID = [SupplyOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationDocument] " +
            "ON [SupplyOrganizationDocument].SupplyOrganizationAgreementID = [SupplyOrganizationAgreement].ID " +
            "AND [SupplyOrganizationDocument].Deleted = 0 " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
            "ON [AgreementCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [AgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [AccountingCashFlow_CTE_R] " +
            "ON [AccountingCashFlow_CTE_R].[ID] = [SupplyOrganization].[ID] " +
            "WHERE [SupplyOrganization].Deleted = 0 " +
            "AND [SupplyOrganization].ID IN @Ids ";

        _connection.Query<SupplyOrganization, SupplyOrganizationAgreement, SupplyOrganizationDocument, Organization, Currency, SupplyOrganization>(
            sqlExpression,
            (supplyOrganization, agreement, document, organization, agreementCurrency) => {
                if (supplyOrganizations.Any(o => o.Id.Equals(supplyOrganization.Id)))
                    supplyOrganization = supplyOrganizations.First(o => o.Id.Equals(supplyOrganization.Id));
                else
                    supplyOrganizations.Add(supplyOrganization);

                if (agreement == null) return supplyOrganization;

                if (!supplyOrganization.SupplyOrganizationAgreements.Any(a => a.Id.Equals(agreement.Id)))
                    supplyOrganization.SupplyOrganizationAgreements.Add(agreement);
                else
                    agreement = supplyOrganization.SupplyOrganizationAgreements.First(a => a.Id.Equals(agreement.Id));

                agreement.Currency = agreementCurrency;

                agreement.Organization = organization;

                if (document != null && !agreement.SupplyOrganizationDocuments.Any(x => x.Id.Equals(document.Id)))
                    agreement.SupplyOrganizationDocuments.Add(document);

                return supplyOrganization;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Ids = ids
            }
        );

        return supplyOrganizations;
    }

    public List<SupplyOrganization> GetAllFromSearch(string value, Guid? organizationNetId) {
        List<SupplyOrganization> supplyOrganizations = new();

        string sqlExpression =
            ";WITH [AccountingCashFlow_CTE] " +
            "AS " +
            "( " +
            "SELECT [ContainerService].ContainerOrganizationID AS ID " +
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
            "WHERE [ContainerService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortWorkService].PortWorkOrganizationID AS ID " +
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
            "WHERE [PortWorkService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleService].VehicleOrganizationID AS ID " +
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
            "WHERE [VehicleService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [BillOfLadingService].SupplyOrganizationID AS ID " +
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
            "WHERE [BillOfLadingService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT ISNULL([CustomService].CustomOrganizationID, [CustomService].ExciseDutyOrganizationID) AS ID " +
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
            "WHERE [CustomService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [TransportationService].TransportationOrganizationID AS ID " +
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
            "WHERE [TransportationService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortCustomAgencyService].PortCustomAgencyOrganizationID AS ID " +
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
            "WHERE [PortCustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [CustomAgencyService].CustomAgencyOrganizationID AS ID " +
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
            "WHERE [CustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PlaneDeliveryService].PlaneDeliveryOrganizationID AS ID " +
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
            "WHERE [PlaneDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleDeliveryService].VehicleDeliveryOrganizationID AS ID " +
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
            "WHERE [VehicleDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [MergedService].SupplyOrganizationID AS ID " +
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
            "WHERE [MergedService].Deleted = 0 " +
            "AND [MergedService].[ActProvidingServiceID] IS NOT NULL " +
            "UNION ALL " +
            "SELECT [DeliveryExpense].SupplyOrganizationID AS ID " +
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
            "WHERE [DeliveryExpense].Deleted = 0 " +
            "AND [DeliveryExpense].ActProvidingServiceID IS NOT NULL " +
            "UNION ALL " +
            "SELECT [ConsumablesOrderItem].ConsumableProductOrganizationID AS ID " +
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
            "WHERE [ConsumablesOrderItem].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [OutcomePaymentOrder].ConsumableProductOrganizationID AS [ID] " +
            ", -[dbo].[GetExchangedToEuroValue] " +
            "( " +
            "[OutcomePaymentOrder].AfterExchangeAmount, " +
            "[SupplyOrganizationAgreement].CurrencyID, " +
            "GETUTCDATE() " +
            ") AS [GrossPrice] " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
            "WHERE [OutcomePaymentOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrder].IsCanceled = 0 " +
            "AND ([OutcomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
            "OR [OutcomePaymentOrder].ClientAgreementID IS NOT NULL) " +
            "UNION ALL " +
            "SELECT [ContainerService].ContainerOrganizationID AS ID " +
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
            "WHERE [ContainerService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortWorkService].PortWorkOrganizationID AS ID " +
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
            "WHERE [PortWorkService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleService].VehicleOrganizationID AS ID " +
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
            "WHERE [VehicleService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [BillOfLadingService].SupplyOrganizationID AS ID " +
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
            "WHERE [BillOfLadingService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT ISNULL([CustomService].CustomOrganizationID, [CustomService].ExciseDutyOrganizationID) AS ID " +
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
            "WHERE [CustomService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [TransportationService].TransportationOrganizationID AS ID " +
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
            "WHERE [TransportationService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortCustomAgencyService].PortCustomAgencyOrganizationID AS ID " +
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
            "WHERE [PortCustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [CustomAgencyService].CustomAgencyOrganizationID AS ID " +
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
            "WHERE [CustomAgencyService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PlaneDeliveryService].PlaneDeliveryOrganizationID AS ID " +
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
            "WHERE [PlaneDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleDeliveryService].VehicleDeliveryOrganizationID AS ID " +
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
            "WHERE [VehicleDeliveryService].Deleted = 0 " +
            "AND [SupplyPaymentTask].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [MergedService].SupplyOrganizationID AS ID " +
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
            "WHERE [MergedService].Deleted = 0 " +
            "AND [MergedService].[AccountingActProvidingServiceID] IS NOT NULL " +
            "UNION ALL " +
            "SELECT [DeliveryExpense].SupplyOrganizationID AS ID " +
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
            "WHERE [DeliveryExpense].Deleted = 0 " +
            "AND [DeliveryExpense].AccountingActProvidingServiceID IS NOT NULL " +
            "UNION ALL " +
            "SELECT [SupplyOrganizationAgreement].SupplyOrganizationID AS [ID], " +
            "[IncomePaymentOrder].EuroAmount AS [GrossPrice] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [IncomePaymentOrder].SupplyOrganizationAgreementID " +
            "WHERE [IncomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
            "AND [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 " +
            "), " +
            "[AccountingCashFlow_CTE_R] AS ( " +
            "SELECT " +
            "[AccountingCashFlow_CTE].[ID] " +
            ", SUM([AccountingCashFlow_CTE].[GrossPrice]) [TotalCurrentAmount] " +
            "FROM [AccountingCashFlow_CTE] " +
            "GROUP BY [AccountingCashFlow_CTE].[ID] " +
            ") " +
            "SELECT " +
            "[SupplyOrganization].* " +
            ", IIF([AccountingCashFlow_CTE_R].[TotalCurrentAmount] IS NULL, 0, [AccountingCashFlow_CTE_R].[TotalCurrentAmount]) [TotalAgreementsCurrentEuroAmount] " +
            ", [SupplyOrganizationAgreement].* " +
            ", [SupplyOrganizationDocument].* " +
            ", [Organization].* " +
            ", [AgreementCurrency].* " +
            "FROM [SupplyOrganization] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].SupplyOrganizationID = [SupplyOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationDocument] " +
            "ON [SupplyOrganizationDocument].SupplyOrganizationAgreementID = [SupplyOrganizationAgreement].ID " +
            "AND [SupplyOrganizationDocument].Deleted = 0 " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
            "ON [AgreementCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [AgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [AccountingCashFlow_CTE_R] " +
            "ON [AccountingCashFlow_CTE_R].[ID] = [SupplyOrganization].[ID] " +
            "WHERE [SupplyOrganization].Deleted = 0 ";

        if (organizationNetId.HasValue) sqlExpression += "AND [Organization].NetUID = @OrganizationNetId ";

        sqlExpression +=
            "AND (" +
            "[SupplyOrganization].[Name] like '%' + @Value + '%' " +
            "OR " +
            "[SupplyOrganization].[USREOU] like '%' + @Value + '%' )" +
            "ORDER BY [SupplyOrganization].[Name]";

        _connection.Query<SupplyOrganization, SupplyOrganizationAgreement, SupplyOrganizationDocument, Organization, Currency, SupplyOrganization>(
            sqlExpression,
            (supplyOrganization, agreement, document, organization, agreementCurrency) => {
                if (supplyOrganizations.Any(o => o.Id.Equals(supplyOrganization.Id)))
                    supplyOrganization = supplyOrganizations.First(o => o.Id.Equals(supplyOrganization.Id));
                else
                    supplyOrganizations.Add(supplyOrganization);

                if (agreement == null) return supplyOrganization;

                if (!supplyOrganization.SupplyOrganizationAgreements.Any(a => a.Id.Equals(agreement.Id)))
                    supplyOrganization.SupplyOrganizationAgreements.Add(agreement);
                else
                    agreement = supplyOrganization.SupplyOrganizationAgreements.First(a => a.Id.Equals(agreement.Id));

                agreement.Currency = agreementCurrency;

                agreement.Organization = organization;

                if (document != null && !agreement.SupplyOrganizationDocuments.Any(x => x.Id.Equals(document.Id)))
                    agreement.SupplyOrganizationDocuments.Add(document);

                return supplyOrganization;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Value = value, OrganizationNetId = organizationNetId
            }
        );

        return supplyOrganizations;
    }


    public List<SupplyOrganization> GetAll() {
        List<SupplyOrganization> supplyOrganizations = new();

        string sqlExpression =
            "SELECT " +
            "[SupplyOrganization].* " +
            ", [SupplyOrganizationAgreement].* " +
            ", [SupplyOrganizationDocument].* " +
            ", [Organization].* " +
            ", [AgreementCurrency].* " +
            "FROM [SupplyOrganization] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].SupplyOrganizationID = [SupplyOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationDocument] " +
            "ON [SupplyOrganizationDocument].SupplyOrganizationAgreementID = [SupplyOrganizationAgreement].ID " +
            "AND [SupplyOrganizationDocument].Deleted = 0 " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
            "ON [AgreementCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [AgreementCurrency].CultureCode = @Culture " +
            "WHERE [SupplyOrganization].Deleted = 0 " +
            "ORDER BY [SupplyOrganization].[Name]";

        _connection.Query<SupplyOrganization, SupplyOrganizationAgreement, SupplyOrganizationDocument, Organization, Currency, SupplyOrganization>(
            sqlExpression,
            (supplyOrganization, agreement, document, organization, agreementCurrency) => {
                if (supplyOrganizations.Any(o => o.Id.Equals(supplyOrganization.Id)))
                    supplyOrganization = supplyOrganizations.First(o => o.Id.Equals(supplyOrganization.Id));
                else
                    supplyOrganizations.Add(supplyOrganization);

                if (agreement == null) return supplyOrganization;

                if (!supplyOrganization.SupplyOrganizationAgreements.Any(a => a.Id.Equals(agreement.Id)))
                    supplyOrganization.SupplyOrganizationAgreements.Add(agreement);
                else
                    agreement = supplyOrganization.SupplyOrganizationAgreements.First(a => a.Id.Equals(agreement.Id));

                agreement.Currency = agreementCurrency;

                agreement.Organization = organization;

                if (document != null && !agreement.SupplyOrganizationDocuments.Any(x => x.Id.Equals(document.Id)))
                    agreement.SupplyOrganizationDocuments.Add(document);

                return supplyOrganization;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return supplyOrganizations;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [SupplyOrganization] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [SupplyOrganization].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}