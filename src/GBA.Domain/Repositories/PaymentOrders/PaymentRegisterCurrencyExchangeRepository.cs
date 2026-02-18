using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class PaymentRegisterCurrencyExchangeRepository : IPaymentRegisterCurrencyExchangeRepository {
    private readonly IDbConnection _connection;

    public PaymentRegisterCurrencyExchangeRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PaymentRegisterCurrencyExchange paymentRegisterCurrencyExchange) {
        return _connection.Query<long>(
                "INSERT INTO [PaymentRegisterCurrencyExchange] " +
                "(IncomeNumber, Number, Amount, ExchangeRate, FromPaymentCurrencyRegisterId, ToPaymentCurrencyRegisterId, UserId, CurrencyTraderId, Comment, FromDate, Updated) " +
                "VALUES (@IncomeNumber, @Number, @Amount, @ExchangeRate, @FromPaymentCurrencyRegisterId, @ToPaymentCurrencyRegisterId, @UserId, @CurrencyTraderId, @Comment, @FromDate, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                paymentRegisterCurrencyExchange
            )
            .Single();
    }

    public void Update(PaymentRegisterCurrencyExchange paymentRegisterCurrencyExchange) {
        _connection.Execute(
            "UPDATE [PaymentRegisterCurrencyExchange] " +
            "SET UserId = @UserId, CurrencyTraderId = @CurrencyTraderId, Comment = @Comment, FromDate = @FromDate, Updated = getutcdate() " +
            "WHERE [PaymentRegisterCurrencyExchange].ID = @Id",
            paymentRegisterCurrencyExchange
        );
    }

    public void SetCanceled(Guid netId) {
        _connection.Execute(
            "UPDATE [PaymentRegisterCurrencyExchange] " +
            "SET IsCanceled = 1, Updated = getutcdate() " +
            "WHERE [PaymentRegisterCurrencyExchange].NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public PaymentRegisterCurrencyExchange GetLastRecord() {
        return _connection.Query<PaymentRegisterCurrencyExchange>(
                "SELECT TOP(1) * " +
                "FROM [PaymentRegisterCurrencyExchange] " +
                "WHERE [PaymentRegisterCurrencyExchange].Deleted = 0 " +
                "ORDER BY [PaymentRegisterCurrencyExchange].ID DESC"
            )
            .SingleOrDefault();
    }

    public PaymentRegisterCurrencyExchange GetById(long id) {
        Type[] types = {
            typeof(PaymentRegisterCurrencyExchange),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(User),
            typeof(CurrencyTrader),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement)
        };

        Func<object[], PaymentRegisterCurrencyExchange> mapper = objects => {
            PaymentRegisterCurrencyExchange paymentRegisterCurrencyExchange = (PaymentRegisterCurrencyExchange)objects[0];
            PaymentCurrencyRegister fromPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[1];
            Currency fromCurrency = (Currency)objects[2];
            PaymentRegister fromPaymentRegister = (PaymentRegister)objects[3];
            Organization fromOrganization = (Organization)objects[4];
            PaymentCurrencyRegister toPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency toCurrency = (Currency)objects[6];
            PaymentRegister toPaymentRegister = (PaymentRegister)objects[7];
            Organization toOrganization = (Organization)objects[8];
            User user = (User)objects[9];
            CurrencyTrader currencyTrader = (CurrencyTrader)objects[10];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[11];
            PaymentMovement paymentMovement = (PaymentMovement)objects[12];

            if (paymentMovementOperation != null) {
                paymentMovementOperation.PaymentMovement = paymentMovement;

                paymentRegisterCurrencyExchange.PaymentMovementOperation = paymentMovementOperation;
            }

            fromPaymentRegister.Organization = fromOrganization;

            fromPaymentCurrencyRegister.Currency = fromCurrency;
            fromPaymentCurrencyRegister.PaymentRegister = fromPaymentRegister;

            toPaymentRegister.Organization = toOrganization;

            toPaymentCurrencyRegister.Currency = toCurrency;
            toPaymentCurrencyRegister.PaymentRegister = toPaymentRegister;

            paymentRegisterCurrencyExchange.FromPaymentCurrencyRegister = fromPaymentCurrencyRegister;
            paymentRegisterCurrencyExchange.ToPaymentCurrencyRegister = toPaymentCurrencyRegister;
            paymentRegisterCurrencyExchange.User = user;
            paymentRegisterCurrencyExchange.CurrencyTrader = currencyTrader;
            paymentRegisterCurrencyExchange.Type = PaymentRegisterTransferType.Income;

            return paymentRegisterCurrencyExchange;
        };

        return _connection.Query(
                "SELECT * " +
                "FROM [PaymentRegisterCurrencyExchange] " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [FromPaymentCurrencyRegister] " +
                "ON [FromPaymentCurrencyRegister].ID = [PaymentRegisterCurrencyExchange].FromPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [FromPaymentCurrencyRegisterCurrency] " +
                "ON [FromPaymentCurrencyRegisterCurrency].ID = [FromPaymentCurrencyRegister].CurrencyID " +
                "AND [FromPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [FromPaymentCurrencyRegisterPaymentRegister] " +
                "ON [FromPaymentCurrencyRegisterPaymentRegister].ID = [FromPaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [FromPaymentCurrencyRegisterPaymentRegisterOrganization] " +
                "ON [FromPaymentCurrencyRegisterPaymentRegisterOrganization].ID = [FromPaymentCurrencyRegisterPaymentRegister].OrganizationID " +
                "AND [FromPaymentCurrencyRegisterPaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [ToPaymentCurrencyRegister] " +
                "ON [ToPaymentCurrencyRegister].ID = [PaymentRegisterCurrencyExchange].ToPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [ToPaymentCurrencyRegisterCurrency] " +
                "ON [ToPaymentCurrencyRegisterCurrency].ID = [ToPaymentCurrencyRegister].CurrencyID " +
                "AND [ToPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [ToPaymentCurrencyRegisterPaymentRegister] " +
                "ON [ToPaymentCurrencyRegisterPaymentRegister].ID = [ToPaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [ToPaymentCurrencyRegisterPaymentRegisterOrganization] " +
                "ON [ToPaymentCurrencyRegisterPaymentRegisterOrganization].ID = [ToPaymentCurrencyRegisterPaymentRegister].OrganizationID " +
                "AND [ToPaymentCurrencyRegisterPaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PaymentRegisterCurrencyExchange].UserID " +
                "LEFT JOIN [CurrencyTrader] " +
                "ON [PaymentRegisterCurrencyExchange].CurrencyTraderID = [CurrencyTrader].ID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentRegisterCurrencyExchange].ID = [PaymentMovementOperation].PaymentRegisterCurrencyExchangeID " +
                "LEFT JOIN (" +
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
                "WHERE [PaymentRegisterCurrencyExchange].ID = @Id",
                types,
                mapper,
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .Single();
    }

    public PaymentRegisterCurrencyExchange GetByNetId(Guid netId) {
        Type[] types = {
            typeof(PaymentRegisterCurrencyExchange),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(User),
            typeof(CurrencyTrader),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement)
        };

        Func<object[], PaymentRegisterCurrencyExchange> mapper = objects => {
            PaymentRegisterCurrencyExchange paymentRegisterCurrencyExchange = (PaymentRegisterCurrencyExchange)objects[0];
            PaymentCurrencyRegister fromPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[1];
            Currency fromCurrency = (Currency)objects[2];
            PaymentRegister fromPaymentRegister = (PaymentRegister)objects[3];
            Organization fromOrganization = (Organization)objects[4];
            PaymentCurrencyRegister toPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency toCurrency = (Currency)objects[6];
            PaymentRegister toPaymentRegister = (PaymentRegister)objects[7];
            Organization toOrganization = (Organization)objects[8];
            User user = (User)objects[9];
            CurrencyTrader currencyTrader = (CurrencyTrader)objects[10];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[11];
            PaymentMovement paymentMovement = (PaymentMovement)objects[12];

            if (paymentMovementOperation != null) {
                paymentMovementOperation.PaymentMovement = paymentMovement;

                paymentRegisterCurrencyExchange.PaymentMovementOperation = paymentMovementOperation;
            }

            fromPaymentRegister.Organization = fromOrganization;

            fromPaymentCurrencyRegister.Currency = fromCurrency;
            fromPaymentCurrencyRegister.PaymentRegister = fromPaymentRegister;

            toPaymentRegister.Organization = toOrganization;

            toPaymentCurrencyRegister.Currency = toCurrency;
            toPaymentCurrencyRegister.PaymentRegister = toPaymentRegister;

            paymentRegisterCurrencyExchange.FromPaymentCurrencyRegister = fromPaymentCurrencyRegister;
            paymentRegisterCurrencyExchange.ToPaymentCurrencyRegister = toPaymentCurrencyRegister;
            paymentRegisterCurrencyExchange.User = user;
            paymentRegisterCurrencyExchange.CurrencyTrader = currencyTrader;
            paymentRegisterCurrencyExchange.Type = PaymentRegisterTransferType.Income;

            return paymentRegisterCurrencyExchange;
        };

        return _connection.Query(
                "SELECT * " +
                "FROM [PaymentRegisterCurrencyExchange] " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [FromPaymentCurrencyRegister] " +
                "ON [FromPaymentCurrencyRegister].ID = [PaymentRegisterCurrencyExchange].FromPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [FromPaymentCurrencyRegisterCurrency] " +
                "ON [FromPaymentCurrencyRegisterCurrency].ID = [FromPaymentCurrencyRegister].CurrencyID " +
                "AND [FromPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [FromPaymentCurrencyRegisterPaymentRegister] " +
                "ON [FromPaymentCurrencyRegisterPaymentRegister].ID = [FromPaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [FromPaymentCurrencyRegisterPaymentRegisterOrganization] " +
                "ON [FromPaymentCurrencyRegisterPaymentRegisterOrganization].ID = [FromPaymentCurrencyRegisterPaymentRegister].OrganizationID " +
                "AND [FromPaymentCurrencyRegisterPaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [ToPaymentCurrencyRegister] " +
                "ON [ToPaymentCurrencyRegister].ID = [PaymentRegisterCurrencyExchange].ToPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [ToPaymentCurrencyRegisterCurrency] " +
                "ON [ToPaymentCurrencyRegisterCurrency].ID = [ToPaymentCurrencyRegister].CurrencyID " +
                "AND [ToPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [ToPaymentCurrencyRegisterPaymentRegister] " +
                "ON [ToPaymentCurrencyRegisterPaymentRegister].ID = [ToPaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [ToPaymentCurrencyRegisterPaymentRegisterOrganization] " +
                "ON [ToPaymentCurrencyRegisterPaymentRegisterOrganization].ID = [ToPaymentCurrencyRegisterPaymentRegister].OrganizationID " +
                "AND [ToPaymentCurrencyRegisterPaymentRegisterOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [PaymentRegisterCurrencyExchange].UserID " +
                "LEFT JOIN [CurrencyTrader] " +
                "ON [PaymentRegisterCurrencyExchange].CurrencyTraderID = [CurrencyTrader].ID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentRegisterCurrencyExchange].ID = [PaymentMovementOperation].PaymentRegisterCurrencyExchangeID " +
                "LEFT JOIN (" +
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
                "WHERE [PaymentRegisterCurrencyExchange].NetUID = @NetId",
                types,
                mapper,
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public List<PaymentRegisterCurrencyExchange> GetAllByPaymentRegisterNetId(DateTime from, DateTime to, Guid? paymentRegisterNetId, Guid? fromCurrencyNetId,
        Guid? toCurrencyNetId) {
        List<PaymentRegisterCurrencyExchange> toReturn = new();

        Type[] types = {
            typeof(PaymentRegister),
            typeof(PaymentCurrencyRegister),
            typeof(PaymentRegisterCurrencyExchange),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(User),
            typeof(CurrencyTrader),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement)
        };

        Func<object[], PaymentRegister> mapper = objects => {
            PaymentRegister paymentRegister = (PaymentRegister)objects[0];
            PaymentRegisterCurrencyExchange paymentRegisterCurrencyExchange = (PaymentRegisterCurrencyExchange)objects[2];
            PaymentCurrencyRegister fromPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[3];
            Currency fromCurrency = (Currency)objects[4];
            PaymentRegister fromPaymentRegister = (PaymentRegister)objects[5];
            Organization fromOrganization = (Organization)objects[6];
            PaymentCurrencyRegister toPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[7];
            Currency toCurrency = (Currency)objects[8];
            PaymentRegister toPaymentRegister = (PaymentRegister)objects[9];
            Organization toOrganization = (Organization)objects[10];
            User user = (User)objects[11];
            CurrencyTrader currencyTrader = (CurrencyTrader)objects[12];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[13];
            PaymentMovement paymentMovement = (PaymentMovement)objects[14];

            if (paymentRegisterCurrencyExchange != null) {
                if (paymentMovementOperation != null) {
                    paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentRegisterCurrencyExchange.PaymentMovementOperation = paymentMovementOperation;
                }

                fromPaymentRegister.Organization = fromOrganization;

                fromPaymentCurrencyRegister.Currency = fromCurrency;
                fromPaymentCurrencyRegister.PaymentRegister = fromPaymentRegister;

                toPaymentRegister.Organization = toOrganization;

                toPaymentCurrencyRegister.Currency = toCurrency;
                toPaymentCurrencyRegister.PaymentRegister = toPaymentRegister;

                paymentRegisterCurrencyExchange.FromPaymentCurrencyRegister = fromPaymentCurrencyRegister;
                paymentRegisterCurrencyExchange.ToPaymentCurrencyRegister = toPaymentCurrencyRegister;
                paymentRegisterCurrencyExchange.User = user;
                paymentRegisterCurrencyExchange.CurrencyTrader = currencyTrader;

                if (toReturn.Any(e => e.Id.Equals(paymentRegisterCurrencyExchange.Id))) {
                    paymentRegisterCurrencyExchange.Type = PaymentRegisterTransferType.Income;

                    if (fromCurrency.Code.ToLower().Equals("uah"))
                        paymentRegisterCurrencyExchange.Amount =
                            Math.Round(
                                Math.Round(paymentRegisterCurrencyExchange.Amount / paymentRegisterCurrencyExchange.ExchangeRate, 2)
                                , 2);
                    else
                        paymentRegisterCurrencyExchange.Amount =
                            Math.Round(
                                Math.Round(paymentRegisterCurrencyExchange.Amount * paymentRegisterCurrencyExchange.ExchangeRate, 2)
                                , 2);

                    toReturn.Add(paymentRegisterCurrencyExchange);
                } else {
                    paymentRegisterCurrencyExchange.Type = PaymentRegisterTransferType.Outcome;

                    toReturn.Add(paymentRegisterCurrencyExchange);
                }
            }

            return paymentRegister;
        };

        string sqlExpression =
            "SELECT * " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN  [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID =  [PaymentRegister].ID " +
            "LEFT JOIN [PaymentRegisterCurrencyExchange] " +
            "ON [PaymentRegisterCurrencyExchange].FromPaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
            "OR [PaymentRegisterCurrencyExchange].ToPaymentCurrencyRegisterID = [PaymentCurrencyRegister].ID " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [FromPaymentCurrencyRegister] " +
            "ON [FromPaymentCurrencyRegister].ID = [PaymentRegisterCurrencyExchange].FromPaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [FromPaymentCurrencyRegisterCurrency] " +
            "ON [FromPaymentCurrencyRegisterCurrency].ID = [FromPaymentCurrencyRegister].CurrencyID " +
            "AND [FromPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [FromPaymentCurrencyRegisterPaymentRegister] " +
            "ON [FromPaymentCurrencyRegisterPaymentRegister].ID = [FromPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [FromPaymentCurrencyRegisterPaymentRegisterOrganization] " +
            "ON [FromPaymentCurrencyRegisterPaymentRegisterOrganization].ID = [FromPaymentCurrencyRegisterPaymentRegister].OrganizationID " +
            "AND [FromPaymentCurrencyRegisterPaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [ToPaymentCurrencyRegister] " +
            "ON [ToPaymentCurrencyRegister].ID = [PaymentRegisterCurrencyExchange].ToPaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [ToPaymentCurrencyRegisterCurrency] " +
            "ON [ToPaymentCurrencyRegisterCurrency].ID = [ToPaymentCurrencyRegister].CurrencyID " +
            "AND [ToPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [ToPaymentCurrencyRegisterPaymentRegister] " +
            "ON [ToPaymentCurrencyRegisterPaymentRegister].ID = [ToPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [ToPaymentCurrencyRegisterPaymentRegisterOrganization] " +
            "ON [ToPaymentCurrencyRegisterPaymentRegisterOrganization].ID = [ToPaymentCurrencyRegisterPaymentRegister].OrganizationID " +
            "AND [ToPaymentCurrencyRegisterPaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [PaymentRegisterCurrencyExchange].UserID " +
            "LEFT JOIN [CurrencyTrader] " +
            "ON [PaymentRegisterCurrencyExchange].CurrencyTraderID = [CurrencyTrader].ID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [PaymentRegisterCurrencyExchange].ID = [PaymentMovementOperation].PaymentRegisterCurrencyExchangeID " +
            "LEFT JOIN (" +
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
            "WHERE [PaymentRegisterCurrencyExchange].ID IS NOT NULL " +
            "AND [PaymentRegisterCurrencyExchange].Created > @From " +
            "AND [PaymentRegisterCurrencyExchange].Created < @To";

        if (paymentRegisterNetId.HasValue)
            sqlExpression += " AND [PaymentRegister].NetUID = @PaymentRegisterNetId";
        else
            sqlExpression += " AND [PaymentRegister].Deleted = 0";

        if (fromCurrencyNetId.HasValue) sqlExpression += " AND [FromPaymentCurrencyRegisterCurrency].NetUID = @FromCurrencyNetId";

        if (toCurrencyNetId.HasValue) sqlExpression += " AND [ToPaymentCurrencyRegisterCurrency].NetUID = @ToCurrencyNetId";

        sqlExpression += " ORDER BY [PaymentRegisterCurrencyExchange].ID DESC";

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                PaymentRegisterNetId = paymentRegisterNetId ?? Guid.Empty,
                ToCurrencyNetId = toCurrencyNetId ?? Guid.Empty,
                FromCurrencyNetId = fromCurrencyNetId ?? Guid.Empty,
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return toReturn;
    }
}