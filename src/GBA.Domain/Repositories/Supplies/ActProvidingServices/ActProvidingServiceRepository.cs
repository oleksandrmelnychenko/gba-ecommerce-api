using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.ActProvidingServices;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.ActProvidingServices.Contracts;

namespace GBA.Domain.Repositories.Supplies.ActProvidingServices;

public sealed class ActProvidingServiceRepository : IActProvidingServiceRepository {
    private readonly IDbConnection _connection;

    public ActProvidingServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long New(ActProvidingService act) {
        return _connection.Query<long>(
            "INSERT INTO [ActProvidingService] ([Updated], [IsAccounting], [Price], [UserID], [FromDate], [Comment], [Number]) " +
            "VALUES (getutcdate(), @IsAccounting, @Price, @UserId, @FromDate, @Comment, @Number); " +
            "SELECT SCOPE_IDENTITY(); ", act).SingleOrDefault();
    }

    public void Update(ActProvidingService act) {
        _connection.Execute(
            "UPDATE [ActProvidingService] " +
            "SET [Updated] = getutcdate() " +
            ", [IsAccounting] = @IsAccounting " +
            ", [Price] = @Price " +
            ", [UserID] = @UserId " +
            ", [FromDate] = @FromDate " +
            ", [Comment] = @Comment " +
            ", [Deleted] = @Deleted " +
            ", [Number] = @Number " +
            "WHERE [ActProvidingService].[ID] = @Id; ", act);
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [ActProvidingService] " +
            "SET [Updated] = getutcdate() " +
            ", [Deleted] = 1 " +
            "WHERE [ActProvidingService].[ID] = @Id; ",
            new { Id = id });
    }

    public IEnumerable<ActProvidingService> GetAll(
        DateTime from,
        DateTime to,
        int limit,
        int offset) {
        List<ActProvidingService> toReturn =
            _connection.Query<ActProvidingService>(
                ";WITH FILTERED_CTE AS ( " +
                "SELECT " +
                "ROW_NUMBER() OVER (ORDER BY [ActProvidingService].[FromDate] DESC) AS [RowNumber] " +
                ", [ActProvidingService].[ID] " +
                "FROM [ActProvidingService] " +
                "WHERE [ActProvidingService].[Deleted] = 0 " +
                "AND [ActProvidingService].[FromDate] >= @From " +
                "AND [ActProvidingService].[FromDate] <= @To " +
                ") " +
                "SELECT " +
                "[ActProvidingService].* " +
                "FROM [ActProvidingService] " +
                "LEFT JOIN [FILTERED_CTE] " +
                "ON [FILTERED_CTE].[ID] = [ActProvidingService].[ID] " +
                "WHERE [ActProvidingService].[ID] IN ( " +
                "SELECT [FILTERED_CTE].[ID] FROM [FILTERED_CTE] " +
                ") " +
                "AND [FILTERED_CTE].[RowNumber] > @Offset " +
                "AND [FILTERED_CTE].[RowNumber] <= @Limit + @Offset ",
                new { From = from, To = to, Limit = limit, Offset = offset }).AsList();

        object param = new { Ids = toReturn.Select(x => x.Id) };

        Type[] billOfLadingTypes = {
            typeof(BillOfLadingService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization),
            typeof(Currency),
            typeof(User),
            typeof(DeliveryProductProtocol)
        };

        Func<object[], BillOfLadingService> billOfLadingMapper = objects => {
            BillOfLadingService service = (BillOfLadingService)objects[0];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
            SupplyOrganizationAgreement agreement = (SupplyOrganizationAgreement)objects[2];
            Organization organization = (Organization)objects[3];
            Currency currency = (Currency)objects[4];
            User user = (User)objects[5];
            DeliveryProductProtocol protocol = (DeliveryProductProtocol)objects[6];

            if (service.ActProvidingServiceId.HasValue) {
                ActProvidingService existEl = toReturn.FirstOrDefault(x => x.Id.Equals(service.ActProvidingServiceId.Value));

                if (existEl != null)
                    existEl.BillOfLadingService = service;
            }

            if (service.AccountingActProvidingServiceId.HasValue) {
                ActProvidingService existEl = toReturn.FirstOrDefault(x => x.Id.Equals(service.AccountingActProvidingServiceId.Value));

                if (existEl != null)
                    existEl.AccountingBillOfLadingService = service;
            }

            agreement.Currency = currency;
            agreement.Organization = organization;
            service.SupplyOrganizationAgreement = agreement;
            service.User = user;
            service.SupplyOrganization = supplyOrganization;
            service.DeliveryProductProtocol = protocol;

            return service;
        };

        _connection.Query(
            "SELECT * FROM [BillOfLadingService] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].[ID] = [BillOfLadingService].[SupplyOrganizationID] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [BillOfLadingService].[SupplyOrganizationAgreementID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrganizationAgreement].[OrganizationID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [BillOfLadingService].[UserID] " +
            "LEFT JOIN [DeliveryProductProtocol] " +
            "ON [DeliveryProductProtocol].[ID] = [BillOfLadingService].[DeliveryProductProtocolID] " +
            "WHERE [BillOfLadingService].[Deleted] = 0 " +
            "AND [DeliveryProductProtocol].[Deleted] = 0 " +
            "AND ([BillOfLadingService].[ActProvidingServiceID] IN @Ids OR " +
            "[BillOfLadingService].[AccountingActProvidingServiceID] IN @Ids) ",
            billOfLadingTypes, billOfLadingMapper,
            param);

        Type[] mergedTypes = {
            typeof(MergedService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization),
            typeof(Currency),
            typeof(User),
            typeof(DeliveryProductProtocol)
        };

        Func<object[], MergedService> mergedMapper = objects => {
            MergedService service = (MergedService)objects[0];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
            SupplyOrganizationAgreement agreement = (SupplyOrganizationAgreement)objects[2];
            Organization organization = (Organization)objects[3];
            Currency currency = (Currency)objects[4];
            User user = (User)objects[5];
            DeliveryProductProtocol protocol = (DeliveryProductProtocol)objects[6];

            if (service.ActProvidingServiceId.HasValue) {
                ActProvidingService existEl = toReturn.FirstOrDefault(x => x.Id.Equals(service.ActProvidingServiceId.Value));

                if (existEl != null)
                    existEl.MergedService = service;
            }

            if (service.AccountingActProvidingServiceId.HasValue) {
                ActProvidingService existEl = toReturn.FirstOrDefault(x => x.Id.Equals(service.AccountingActProvidingServiceId.Value));

                if (existEl != null)
                    existEl.AccountingMergedService = service;
            }

            agreement.Currency = currency;
            agreement.Organization = organization;
            service.SupplyOrganizationAgreement = agreement;
            service.User = user;
            service.SupplyOrganization = supplyOrganization;
            service.DeliveryProductProtocol = protocol;

            return service;
        };

        _connection.Query(
            "SELECT * FROM [MergedService] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].[ID] = [MergedService].[SupplyOrganizationID] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [MergedService].[SupplyOrganizationAgreementID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrganizationAgreement].[OrganizationID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [MergedService].[UserID] " +
            "LEFT JOIN [DeliveryProductProtocol] " +
            "ON [DeliveryProductProtocol].[ID] = [MergedService].[DeliveryProductProtocolID] " +
            "WHERE [MergedService].[Deleted] = 0 " +
            "AND [DeliveryProductProtocol].[Deleted] = 0 " +
            "AND ([MergedService].[ActProvidingServiceID] IN @Ids OR " +
            "[MergedService].[AccountingActProvidingServiceID] IN @Ids) ",
            mergedTypes, mergedMapper,
            param);

        Type[] deliveryExpenseTypesActProvidingServiceID = {
            typeof(DeliveryExpense),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(User),
            typeof(SupplyOrderUkraine),
            typeof(Organization)
        };

        Func<object[], DeliveryExpense> deliveryExpenseMapperActProvidingServiceID = objects => {
            DeliveryExpense deliveryExpense = (DeliveryExpense)objects[0];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
            SupplyOrganizationAgreement agreement = (SupplyOrganizationAgreement)objects[2];
            Currency currency = (Currency)objects[3];
            User user = (User)objects[4];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[5];
            Organization organization = (Organization)objects[6];

            if (deliveryExpense.ActProvidingServiceId.HasValue) {
                ActProvidingService existEl = toReturn.FirstOrDefault(x => x.Id.Equals(deliveryExpense.ActProvidingServiceId.Value));

                if (existEl != null)
                    existEl.DeliveryExpense = deliveryExpense;
            }

            //if (deliveryExpense.AccountingActProvidingServiceId.HasValue) {
            //    ActProvidingService existEl = toReturn.FirstOrDefault(x => x.Id.Equals(deliveryExpense.AccountingActProvidingServiceId.Value));

            //    if (existEl != null)
            //        existEl.DeliveryExpense = deliveryExpense;
            //}

            supplyOrderUkraine.Organization = organization;
            agreement.Currency = currency;
            agreement.Organization = organization;
            deliveryExpense.SupplyOrganizationAgreement = agreement;
            deliveryExpense.User = user;
            deliveryExpense.SupplyOrganization = supplyOrganization;
            deliveryExpense.SupplyOrderUkraine = supplyOrderUkraine;

            return deliveryExpense;
        };

        _connection.Query(
            "SELECT " +
            "[DeliveryExpense].* " +
            ", [SupplyOrganization].* " +
            ", [SupplyOrganizationAgreement].* " +
            ", [Currency].* " +
            ", [User].* " +
            ", [SupplyOrderUkraine].* " +
            ", [Organization].* " +
            "FROM [DeliveryExpense] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [DeliveryExpense].SupplyOrganizationID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [DeliveryExpense].UserID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [DeliveryExpense].SupplyOrderUkraineID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "LEFT JOIN [ActProvidingService] " +
            "ON [ActProvidingService].ID = [DeliveryExpense].ActProvidingServiceID " +
            "WHERE [DeliveryExpense].Deleted = 0 " +
            "AND [DeliveryExpense].[ActProvidingServiceID] IN @Ids ",
            deliveryExpenseTypesActProvidingServiceID,
            deliveryExpenseMapperActProvidingServiceID,
            param);

        Type[] deliveryExpenseTypesAccountingActProvidingServiceID = {
            typeof(DeliveryExpense),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(User),
            typeof(SupplyOrderUkraine),
            typeof(Organization)
        };

        Func<object[], DeliveryExpense> deliveryExpenseMapperAccountingActProvidingServiceID = objects => {
            DeliveryExpense deliveryExpense = (DeliveryExpense)objects[0];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
            SupplyOrganizationAgreement agreement = (SupplyOrganizationAgreement)objects[2];
            Currency currency = (Currency)objects[3];
            User user = (User)objects[4];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[5];
            Organization organization = (Organization)objects[6];


            if (deliveryExpense.AccountingActProvidingServiceId.HasValue) {
                ActProvidingService existEl = toReturn.FirstOrDefault(x => x.Id.Equals(deliveryExpense.AccountingActProvidingServiceId.Value));

                if (existEl != null)
                    existEl.DeliveryExpense = deliveryExpense;
            }

            supplyOrderUkraine.Organization = organization;
            agreement.Currency = currency;
            agreement.Organization = organization;
            deliveryExpense.SupplyOrganizationAgreement = agreement;
            deliveryExpense.User = user;
            deliveryExpense.SupplyOrganization = supplyOrganization;
            deliveryExpense.SupplyOrderUkraine = supplyOrderUkraine;

            return deliveryExpense;
        };

        _connection.Query(
            "SELECT " +
            "[DeliveryExpense].* " +
            ", [SupplyOrganization].* " +
            ", [SupplyOrganizationAgreement].* " +
            ", [Currency].* " +
            ", [User].* " +
            ", [SupplyOrderUkraine].* " +
            ", [Organization].* " +
            "FROM [DeliveryExpense] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [DeliveryExpense].SupplyOrganizationID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [DeliveryExpense].UserID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [DeliveryExpense].SupplyOrderUkraineID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "LEFT JOIN [ActProvidingService] " +
            "ON [ActProvidingService].ID = [DeliveryExpense].ActProvidingServiceID " +
            "WHERE [DeliveryExpense].Deleted = 0 " +
            "AND [DeliveryExpense].[AccountingActProvidingServiceID] IN @Ids ",
            deliveryExpenseTypesAccountingActProvidingServiceID,
            deliveryExpenseMapperAccountingActProvidingServiceID,
            param);


        return toReturn;
    }

    public ActProvidingService GetByNetId(Guid netId) {
        ActProvidingService toReturn =
            _connection.Query<ActProvidingService, User, ActProvidingService>(
                "SELECT * FROM [ActProvidingService] " +
                "LEFT JOIN [User] " +
                "ON [User].[ID] = [ActProvidingService].[UserID] " +
                "WHERE [ActProvidingService].[NetUID] = @NetId "
                , (act, user) => {
                    act.User = user;
                    return act;
                }, new { NetId = netId }).FirstOrDefault();

        if (toReturn == null) throw new Exception("Act is not null");

        Type[] billOfLadingTypes = {
            typeof(BillOfLadingService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization),
            typeof(Currency),
            typeof(User),
            typeof(DeliveryProductProtocol)
        };

        Func<object[], BillOfLadingService> billOfLadingMapper = objects => {
            BillOfLadingService service = (BillOfLadingService)objects[0];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
            SupplyOrganizationAgreement agreement = (SupplyOrganizationAgreement)objects[2];
            Organization organization = (Organization)objects[3];
            Currency currency = (Currency)objects[4];
            User user = (User)objects[5];
            DeliveryProductProtocol protocol = (DeliveryProductProtocol)objects[6];

            if (toReturn.IsAccounting)
                toReturn.AccountingBillOfLadingService = service;
            else
                toReturn.BillOfLadingService = service;

            agreement.Currency = currency;
            agreement.Organization = organization;
            service.SupplyOrganizationAgreement = agreement;
            service.User = user;
            service.SupplyOrganization = supplyOrganization;
            service.DeliveryProductProtocol = protocol;

            return service;
        };

        _connection.Query(
            "SELECT * FROM [BillOfLadingService] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].[ID] = [BillOfLadingService].[SupplyOrganizationID] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [BillOfLadingService].[SupplyOrganizationAgreementID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrganizationAgreement].[OrganizationID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [BillOfLadingService].[UserID] " +
            "LEFT JOIN [DeliveryProductProtocol] " +
            "ON [DeliveryProductProtocol].[ID] = [BillOfLadingService].[DeliveryProductProtocolID] " +
            "WHERE [BillOfLadingService].[Deleted] = 0 " +
            "AND [DeliveryProductProtocol].[Deleted] = 0 " +
            "AND ([BillOfLadingService].[ActProvidingServiceID] = @Id OR " +
            "[BillOfLadingService].[AccountingActProvidingServiceID] = @Id) ",
            billOfLadingTypes, billOfLadingMapper,
            new { toReturn.Id });

        Type[] mergedTypes = {
            typeof(MergedService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization),
            typeof(Currency),
            typeof(User),
            typeof(DeliveryProductProtocol),
            typeof(ConsumableProduct)
        };

        Func<object[], MergedService> mergedMapper = objects => {
            MergedService service = (MergedService)objects[0];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
            SupplyOrganizationAgreement agreement = (SupplyOrganizationAgreement)objects[2];
            Organization organization = (Organization)objects[3];
            Currency currency = (Currency)objects[4];
            User user = (User)objects[5];
            DeliveryProductProtocol protocol = (DeliveryProductProtocol)objects[6];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[7];

            if (toReturn.IsAccounting)
                toReturn.AccountingMergedService = service;
            else
                toReturn.MergedService = service;

            agreement.Currency = currency;
            agreement.Organization = organization;
            service.SupplyOrganizationAgreement = agreement;
            service.User = user;
            service.SupplyOrganization = supplyOrganization;
            service.DeliveryProductProtocol = protocol;
            service.ConsumableProduct = consumableProduct;

            return service;
        };

        _connection.Query(
            "SELECT * FROM [MergedService] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].[ID] = [MergedService].[SupplyOrganizationID] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [MergedService].[SupplyOrganizationAgreementID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrganizationAgreement].[OrganizationID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [MergedService].[UserID] " +
            "LEFT JOIN [DeliveryProductProtocol] " +
            "ON [DeliveryProductProtocol].[ID] = [MergedService].[DeliveryProductProtocolID] " +
            "LEFT JOIN [ConsumableProduct] " +
            "ON [ConsumableProduct].[ID] = [MergedService].[ConsumableProductID] " +
            "WHERE [MergedService].[Deleted] = 0 " +
            "AND [DeliveryProductProtocol].[Deleted] = 0 " +
            "AND ([MergedService].[ActProvidingServiceID] = @Id OR " +
            "[MergedService].[AccountingActProvidingServiceID] = @Id) ",
            mergedTypes, mergedMapper,
            new { toReturn.Id });

        Type[] deliveryExpenseTypes = {
            typeof(DeliveryExpense),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(User),
            typeof(SupplyOrderUkraine),
            typeof(Organization),
            typeof(ConsumableProduct)
        };

        Func<object[], DeliveryExpense> deliveryExpenseMapper = objects => {
            DeliveryExpense deliveryExpense = (DeliveryExpense)objects[0];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
            SupplyOrganizationAgreement agreement = (SupplyOrganizationAgreement)objects[2];
            Currency currency = (Currency)objects[3];
            User user = (User)objects[4];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[5];
            Organization organization = (Organization)objects[6];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[7];

            toReturn.DeliveryExpense = deliveryExpense;

            supplyOrderUkraine.Organization = organization;
            agreement.Currency = currency;
            agreement.Organization = organization;
            deliveryExpense.SupplyOrganizationAgreement = agreement;
            deliveryExpense.User = user;
            deliveryExpense.SupplyOrganization = supplyOrganization;
            deliveryExpense.SupplyOrderUkraine = supplyOrderUkraine;
            deliveryExpense.ConsumableProduct = consumableProduct;
            if (toReturn.IsAccounting) {
                deliveryExpense.VatAmount = decimal.Round(
                    deliveryExpense.AccountingGrossAmount * (deliveryExpense.AccountingVatPercent / (100 + deliveryExpense.AccountingVatPercent)),
                    2,
                    MidpointRounding.AwayFromZero);
            } else {
                deliveryExpense.AccountingGrossAmount = deliveryExpense.GrossAmount;
                deliveryExpense.AccountingVatPercent = deliveryExpense.VatPercent;
                deliveryExpense.VatAmount = decimal.Round(
                    deliveryExpense.GrossAmount * (deliveryExpense.VatPercent / (100 + deliveryExpense.VatPercent)),
                    2,
                    MidpointRounding.AwayFromZero);
            }

            return deliveryExpense;
        };

        _connection.Query(
            "SELECT " +
            "[DeliveryExpense].* " +
            ", [SupplyOrganization].* " +
            ", [SupplyOrganizationAgreement].* " +
            ", [Currency].* " +
            ", [User].* " +
            ", [SupplyOrderUkraine].* " +
            ", [Organization].*" +
            ", [ConsumableProduct].* " +
            "FROM [DeliveryExpense] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [DeliveryExpense].SupplyOrganizationID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [DeliveryExpense].SupplyOrganizationAgreementID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [DeliveryExpense].UserID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [DeliveryExpense].SupplyOrderUkraineID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
            "LEFT JOIN [ActProvidingService] " +
            "ON [ActProvidingService].ID = [DeliveryExpense].ActProvidingServiceID " +
            "LEFT JOIN [ConsumableProduct] " +
            "ON [ConsumableProduct].ID = DeliveryExpense.ConsumableProductID " +
            "WHERE [DeliveryExpense].Deleted = 0 " +
            "AND [DeliveryExpense].[ActProvidingServiceID] = @Id " +
            "OR [DeliveryExpense].[AccountingActProvidingServiceID] = @Id ",
            deliveryExpenseTypes,
            deliveryExpenseMapper,
            new { toReturn.Id });

        return toReturn;
    }

    public ActProvidingService GetLastRecord(
        string defaultComment) {
        return _connection.Query<ActProvidingService>(
            "SELECT TOP 1 * " +
            "FROM [ActProvidingService] " +
            "WHERE [ActProvidingService].[Number] != @Comment " +
            "ORDER BY [ActProvidingService].[Created] DESC ",
            new { Comment = defaultComment }).FirstOrDefault();
    }
}