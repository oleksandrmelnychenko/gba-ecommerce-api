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

public sealed class PaymentRegisterTransferRepository : IPaymentRegisterTransferRepository {
    private readonly IDbConnection _connection;

    public PaymentRegisterTransferRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PaymentRegisterTransfer paymentRegisterTransfer) {
        return _connection.Query<long>(
                "INSERT INTO [PaymentRegisterTransfer] (Number, Amount, FromPaymentCurrencyRegisterId, ToPaymentCurrencyRegisterId, UserId, Comment, FromDate, Updated, TypeOfOperation) " +
                "VALUES (@Number, @Amount, @FromPaymentCurrencyRegisterId, @ToPaymentCurrencyRegisterId, @UserId, @Comment, @FromDate, getutcdate(), @TypeOfOperation); " +
                "SELECT SCOPE_IDENTITY()",
                paymentRegisterTransfer
            )
            .Single();
    }

    public void Update(PaymentRegisterTransfer paymentRegisterTransfer) {
        _connection.Execute(
            "UPDATE [PaymentRegisterTransfer] " +
            "SET UserId = @UserId, Comment = @Comment, FromDate = @FromDate, Updated = getutcdate() " +
            "WHERE [PaymentRegisterTransfer].ID = @Id",
            paymentRegisterTransfer
        );
    }

    public void SetCanceled(Guid netId) {
        _connection.Execute(
            "UPDATE [PaymentRegisterTransfer] " +
            "SET IsCanceled = 1, Updated = getutcdate() " +
            "WHERE [PaymentRegisterTransfer].NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public PaymentRegisterTransfer GetLastRecord() {
        return _connection.Query<PaymentRegisterTransfer>(
                "SELECT TOP(1) * " +
                "FROM [PaymentRegisterTransfer] " +
                "WHERE [PaymentRegisterTransfer].Deleted = 0 " +
                "ORDER BY [PaymentRegisterTransfer].ID DESC"
            )
            .SingleOrDefault();
    }

    public PaymentRegisterTransfer GetById(long id) {
        Type[] types = {
            typeof(PaymentRegisterTransfer),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement)
        };

        Func<object[], PaymentRegisterTransfer> mapper = objects => {
            PaymentRegisterTransfer paymentRegisterTransfer = (PaymentRegisterTransfer)objects[0];
            PaymentCurrencyRegister fromPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[1];
            Currency fromCurrency = (Currency)objects[2];
            PaymentRegister fromPaymentRegister = (PaymentRegister)objects[3];
            Organization fromOrganization = (Organization)objects[4];
            PaymentCurrencyRegister toPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency toCurrency = (Currency)objects[6];
            PaymentRegister toPaymentRegister = (PaymentRegister)objects[7];
            Organization toOrganization = (Organization)objects[8];
            User user = (User)objects[9];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[10];
            PaymentMovement paymentMovement = (PaymentMovement)objects[11];

            if (paymentMovementOperation != null) {
                paymentMovementOperation.PaymentMovement = paymentMovement;

                paymentRegisterTransfer.PaymentMovementOperation = paymentMovementOperation;
            }

            fromPaymentRegister.Organization = fromOrganization;

            fromPaymentCurrencyRegister.Currency = fromCurrency;
            fromPaymentCurrencyRegister.PaymentRegister = fromPaymentRegister;

            toPaymentRegister.Organization = toOrganization;

            toPaymentCurrencyRegister.Currency = toCurrency;
            toPaymentCurrencyRegister.PaymentRegister = toPaymentRegister;

            paymentRegisterTransfer.FromPaymentCurrencyRegister = fromPaymentCurrencyRegister;
            paymentRegisterTransfer.ToPaymentCurrencyRegister = toPaymentCurrencyRegister;
            paymentRegisterTransfer.User = user;
            paymentRegisterTransfer.Type = PaymentRegisterTransferType.Outcome;

            return paymentRegisterTransfer;
        };

        return _connection.Query(
                "SELECT * " +
                "FROM [PaymentRegisterTransfer] " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [FromPaymentCurrencyRegister] " +
                "ON [FromPaymentCurrencyRegister].ID = [PaymentRegisterTransfer].FromPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [FromCurrency] " +
                "ON [FromCurrency].ID = [FromPaymentCurrencyRegister].CurrencyID " +
                "AND [FromCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [FromPaymentRegister] " +
                "ON [FromPaymentRegister].ID = [FromPaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [FromOrganization] " +
                "ON [FromOrganization].ID = [FromPaymentRegister].OrganizationID " +
                "AND [FromOrganization].CultureCode = @Culture " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [ToPaymentCurrencyRegister] " +
                "ON [ToPaymentCurrencyRegister].ID = [PaymentRegisterTransfer].ToPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [ToCurrency] " +
                "ON [ToCurrency].ID = [ToPaymentCurrencyRegister].CurrencyID " +
                "AND [ToCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
                "ON [ToPaymentRegister].ID = [ToPaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [ToOrganization] " +
                "ON [ToOrganization].ID = [ToPaymentRegister].OrganizationID " +
                "AND [ToOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [PaymentRegisterTransfer].UserID = [User].ID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentRegisterTransfer].ID = [PaymentMovementOperation].PaymentRegisterTransferID " +
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
                "WHERE [PaymentRegisterTransfer].ID = @Id",
                types,
                mapper,
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .Single();
    }

    public PaymentRegisterTransfer GetByNetId(Guid netId) {
        Type[] types = {
            typeof(PaymentRegisterTransfer),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement)
        };

        Func<object[], PaymentRegisterTransfer> mapper = objects => {
            PaymentRegisterTransfer paymentRegisterTransfer = (PaymentRegisterTransfer)objects[0];
            PaymentCurrencyRegister fromPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[1];
            Currency fromCurrency = (Currency)objects[2];
            PaymentRegister fromPaymentRegister = (PaymentRegister)objects[3];
            Organization fromOrganization = (Organization)objects[4];
            PaymentCurrencyRegister toPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency toCurrency = (Currency)objects[6];
            PaymentRegister toPaymentRegister = (PaymentRegister)objects[7];
            Organization toOrganization = (Organization)objects[8];
            User user = (User)objects[9];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[10];
            PaymentMovement paymentMovement = (PaymentMovement)objects[11];

            if (paymentMovementOperation != null) {
                paymentMovementOperation.PaymentMovement = paymentMovement;

                paymentRegisterTransfer.PaymentMovementOperation = paymentMovementOperation;
            }

            fromPaymentRegister.Organization = fromOrganization;

            fromPaymentCurrencyRegister.Currency = fromCurrency;
            fromPaymentCurrencyRegister.PaymentRegister = fromPaymentRegister;

            toPaymentRegister.Organization = toOrganization;

            toPaymentCurrencyRegister.Currency = toCurrency;
            toPaymentCurrencyRegister.PaymentRegister = toPaymentRegister;

            paymentRegisterTransfer.FromPaymentCurrencyRegister = fromPaymentCurrencyRegister;
            paymentRegisterTransfer.ToPaymentCurrencyRegister = toPaymentCurrencyRegister;
            paymentRegisterTransfer.User = user;
            paymentRegisterTransfer.Type = PaymentRegisterTransferType.Outcome;

            return paymentRegisterTransfer;
        };

        return _connection.Query(
                "SELECT * " +
                "FROM [PaymentRegisterTransfer] " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [FromPaymentCurrencyRegister] " +
                "ON [FromPaymentCurrencyRegister].ID = [PaymentRegisterTransfer].FromPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [FromCurrency] " +
                "ON [FromCurrency].ID = [FromPaymentCurrencyRegister].CurrencyID " +
                "AND [FromCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [FromPaymentRegister] " +
                "ON [FromPaymentRegister].ID = [FromPaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [FromOrganization] " +
                "ON [FromOrganization].ID = [FromPaymentRegister].OrganizationID " +
                "AND [FromOrganization].CultureCode = @Culture " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [ToPaymentCurrencyRegister] " +
                "ON [ToPaymentCurrencyRegister].ID = [PaymentRegisterTransfer].ToPaymentCurrencyRegisterID " +
                "LEFT JOIN [views].[CurrencyView] AS [ToCurrency] " +
                "ON [ToCurrency].ID = [ToPaymentCurrencyRegister].CurrencyID " +
                "AND [ToCurrency].CultureCode = @Culture " +
                "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
                "ON [ToPaymentRegister].ID = [ToPaymentCurrencyRegister].PaymentRegisterID " +
                "LEFT JOIN [views].[OrganizationView] AS [ToOrganization] " +
                "ON [ToOrganization].ID = [ToPaymentRegister].OrganizationID " +
                "AND [ToOrganization].CultureCode = @Culture " +
                "LEFT JOIN [User] " +
                "ON [PaymentRegisterTransfer].UserID = [User].ID " +
                "LEFT JOIN [PaymentMovementOperation] " +
                "ON [PaymentRegisterTransfer].ID = [PaymentMovementOperation].PaymentRegisterTransferID " +
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
                "WHERE [PaymentRegisterTransfer].NetUID = @NetId",
                types,
                mapper,
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public List<PaymentRegisterTransfer> GetAllFiltered(DateTime from, DateTime to, Guid? paymentRegisterNetId, Guid? currencyNetId) {
        Type[] types = {
            typeof(PaymentRegisterTransfer),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement)
        };

        Func<object[], PaymentRegisterTransfer> mapper = objects => {
            PaymentRegisterTransfer paymentRegisterTransfer = (PaymentRegisterTransfer)objects[0];
            PaymentCurrencyRegister fromPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[1];
            Currency fromCurrency = (Currency)objects[2];
            PaymentRegister fromPaymentRegister = (PaymentRegister)objects[3];
            Organization fromOrganization = (Organization)objects[4];
            PaymentCurrencyRegister toPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency toCurrency = (Currency)objects[6];
            PaymentRegister toPaymentRegister = (PaymentRegister)objects[7];
            Organization toOrganization = (Organization)objects[8];
            User user = (User)objects[9];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[10];
            PaymentMovement paymentMovement = (PaymentMovement)objects[11];

            if (paymentRegisterNetId.HasValue)
                paymentRegisterTransfer.Type = fromPaymentRegister.NetUid.Equals(paymentRegisterNetId.Value)
                    ? PaymentRegisterTransferType.Outcome
                    : PaymentRegisterTransferType.Income;

            if (paymentMovementOperation != null) {
                paymentMovementOperation.PaymentMovement = paymentMovement;

                paymentRegisterTransfer.PaymentMovementOperation = paymentMovementOperation;
            }

            fromPaymentRegister.Organization = fromOrganization;

            fromPaymentCurrencyRegister.Currency = fromCurrency;
            fromPaymentCurrencyRegister.PaymentRegister = fromPaymentRegister;

            toPaymentRegister.Organization = toOrganization;

            toPaymentCurrencyRegister.Currency = toCurrency;
            toPaymentCurrencyRegister.PaymentRegister = toPaymentRegister;

            paymentRegisterTransfer.FromPaymentCurrencyRegister = fromPaymentCurrencyRegister;
            paymentRegisterTransfer.ToPaymentCurrencyRegister = toPaymentCurrencyRegister;
            paymentRegisterTransfer.User = user;

            return paymentRegisterTransfer;
        };

        string sqlExpression =
            "SELECT * " +
            "FROM [PaymentRegisterTransfer] " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [FromPaymentCurrencyRegister] " +
            "ON [FromPaymentCurrencyRegister].ID = [PaymentRegisterTransfer].FromPaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [FromCurrency] " +
            "ON [FromCurrency].ID = [FromPaymentCurrencyRegister].CurrencyID " +
            "AND [FromCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [FromPaymentRegister] " +
            "ON [FromPaymentRegister].ID = [FromPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [FromOrganization] " +
            "ON [FromOrganization].ID = [FromPaymentRegister].OrganizationID " +
            "AND [FromOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [ToPaymentCurrencyRegister] " +
            "ON [ToPaymentCurrencyRegister].ID = [PaymentRegisterTransfer].ToPaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [ToCurrency] " +
            "ON [ToCurrency].ID = [ToPaymentCurrencyRegister].CurrencyID " +
            "AND [ToCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
            "ON [ToPaymentRegister].ID = [ToPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [ToOrganization] " +
            "ON [ToOrganization].ID = [ToPaymentRegister].OrganizationID " +
            "AND [ToOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [PaymentRegisterTransfer].UserID = [User].ID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [PaymentRegisterTransfer].ID = [PaymentMovementOperation].PaymentRegisterTransferID " +
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
            "WHERE [PaymentRegisterTransfer].Created > @From " +
            "AND [PaymentRegisterTransfer].Created < @To";

        if (currencyNetId.HasValue)
            sqlExpression += " AND [FromCurrency].NetUID = @CurrencyNetId" +
                             " AND [ToCurrency].NetUID = @CurrencyNetId";

        if (paymentRegisterNetId.HasValue)
            sqlExpression += " AND ([FromPaymentRegister].NetUID = @PaymentRegisterNetId " +
                             "OR [ToPaymentRegister].NetUID = @PaymentRegisterNetId)";

        sqlExpression += " ORDER BY [PaymentRegisterTransfer].ID DESC";

        return _connection.Query(
                sqlExpression,
                types,
                mapper,
                new {
                    PaymentRegisterNetId = paymentRegisterNetId ?? Guid.Empty,
                    CurrencyNetId = currencyNetId ?? Guid.Empty,
                    From = from,
                    To = to,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            )
            .ToList();
    }

    public List<PaymentRegisterTransfer> GetAllOutcomingByPaymentRegisterNetId(Guid paymentRegisterNetId, DateTime from, DateTime to, Guid? currencyNetId) {
        Type[] types = {
            typeof(PaymentRegisterTransfer),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement)
        };

        Func<object[], PaymentRegisterTransfer> mapper = objects => {
            PaymentRegisterTransfer paymentRegisterTransfer = (PaymentRegisterTransfer)objects[0];
            PaymentCurrencyRegister fromPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[1];
            Currency fromCurrency = (Currency)objects[2];
            PaymentRegister fromPaymentRegister = (PaymentRegister)objects[3];
            Organization fromOrganization = (Organization)objects[4];
            PaymentCurrencyRegister toPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency toCurrency = (Currency)objects[6];
            PaymentRegister toPaymentRegister = (PaymentRegister)objects[7];
            Organization toOrganization = (Organization)objects[8];
            User user = (User)objects[9];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[10];
            PaymentMovement paymentMovement = (PaymentMovement)objects[11];

            if (paymentMovementOperation != null) {
                paymentMovementOperation.PaymentMovement = paymentMovement;

                paymentRegisterTransfer.PaymentMovementOperation = paymentMovementOperation;
            }

            fromPaymentRegister.Organization = fromOrganization;

            fromPaymentCurrencyRegister.Currency = fromCurrency;
            fromPaymentCurrencyRegister.PaymentRegister = fromPaymentRegister;

            toPaymentRegister.Organization = toOrganization;

            toPaymentCurrencyRegister.Currency = toCurrency;
            toPaymentCurrencyRegister.PaymentRegister = toPaymentRegister;

            paymentRegisterTransfer.FromPaymentCurrencyRegister = fromPaymentCurrencyRegister;
            paymentRegisterTransfer.ToPaymentCurrencyRegister = toPaymentCurrencyRegister;
            paymentRegisterTransfer.User = user;
            paymentRegisterTransfer.Type = PaymentRegisterTransferType.Outcome;

            return paymentRegisterTransfer;
        };

        string sqlExpression =
            "SELECT * " +
            "FROM [PaymentRegisterTransfer] " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [FromPaymentCurrencyRegister] " +
            "ON [FromPaymentCurrencyRegister].ID = [PaymentRegisterTransfer].FromPaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [FromCurrency] " +
            "ON [FromCurrency].ID = [FromPaymentCurrencyRegister].CurrencyID " +
            "AND [FromCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [FromPaymentRegister] " +
            "ON [FromPaymentRegister].ID = [FromPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [FromOrganization] " +
            "ON [FromOrganization].ID = [FromPaymentRegister].OrganizationID " +
            "AND [FromOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [ToPaymentCurrencyRegister] " +
            "ON [ToPaymentCurrencyRegister].ID = [PaymentRegisterTransfer].ToPaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [ToCurrency] " +
            "ON [ToCurrency].ID = [ToPaymentCurrencyRegister].CurrencyID " +
            "AND [ToCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
            "ON [ToPaymentRegister].ID = [ToPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [ToOrganization] " +
            "ON [ToOrganization].ID = [ToPaymentRegister].OrganizationID " +
            "AND [ToOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [PaymentRegisterTransfer].UserID = [User].ID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [PaymentRegisterTransfer].ID = [PaymentMovementOperation].PaymentRegisterTransferID " +
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
            "WHERE [FromPaymentRegister].NetUID = @PaymentRegisterNetId " +
            "AND [PaymentRegisterTransfer].Created > @From " +
            "AND [PaymentRegisterTransfer].Created < @To";

        if (currencyNetId.HasValue) {
            sqlExpression += " AND [FromCurrency].NetUID = @CurrencyNetId" +
                             " AND [ToCurrency].NetUID = @CurrencyNetId";

            return _connection.Query(
                    sqlExpression,
                    types,
                    mapper,
                    new {
                        PaymentRegisterNetId = paymentRegisterNetId, CurrencyNetId = currencyNetId, From = from, To = to,
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                    }
                )
                .ToList();
        }

        return _connection.Query(
                sqlExpression,
                types,
                mapper,
                new { PaymentRegisterNetId = paymentRegisterNetId, From = from, To = to, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }

    public List<PaymentRegisterTransfer> GetAllIncomingByPaymentRegisterNetId(Guid paymentRegisterNetId, DateTime from, DateTime to, Guid? currencyNetId) {
        Type[] types = {
            typeof(PaymentRegisterTransfer),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement)
        };

        Func<object[], PaymentRegisterTransfer> mapper = objects => {
            PaymentRegisterTransfer paymentRegisterTransfer = (PaymentRegisterTransfer)objects[0];
            PaymentCurrencyRegister fromPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[1];
            Currency fromCurrency = (Currency)objects[2];
            PaymentRegister fromPaymentRegister = (PaymentRegister)objects[3];
            Organization fromOrganization = (Organization)objects[4];
            PaymentCurrencyRegister toPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency toCurrency = (Currency)objects[6];
            PaymentRegister toPaymentRegister = (PaymentRegister)objects[7];
            Organization toOrganization = (Organization)objects[8];
            User user = (User)objects[9];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[10];
            PaymentMovement paymentMovement = (PaymentMovement)objects[11];

            if (paymentMovementOperation != null) {
                paymentMovementOperation.PaymentMovement = paymentMovement;

                paymentRegisterTransfer.PaymentMovementOperation = paymentMovementOperation;
            }

            fromPaymentRegister.Organization = fromOrganization;

            fromPaymentCurrencyRegister.Currency = fromCurrency;
            fromPaymentCurrencyRegister.PaymentRegister = fromPaymentRegister;

            toPaymentRegister.Organization = toOrganization;

            toPaymentCurrencyRegister.Currency = toCurrency;
            toPaymentCurrencyRegister.PaymentRegister = toPaymentRegister;

            paymentRegisterTransfer.FromPaymentCurrencyRegister = fromPaymentCurrencyRegister;
            paymentRegisterTransfer.ToPaymentCurrencyRegister = toPaymentCurrencyRegister;
            paymentRegisterTransfer.User = user;
            paymentRegisterTransfer.Type = PaymentRegisterTransferType.Income;

            return paymentRegisterTransfer;
        };

        string sqlExpression =
            "SELECT * " +
            "FROM [PaymentRegisterTransfer] " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [FromPaymentCurrencyRegister] " +
            "ON [FromPaymentCurrencyRegister].ID = [PaymentRegisterTransfer].FromPaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [FromCurrency] " +
            "ON [FromCurrency].ID = [FromPaymentCurrencyRegister].CurrencyID " +
            "AND [FromCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [FromPaymentRegister] " +
            "ON [FromPaymentRegister].ID = [FromPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [FromOrganization] " +
            "ON [FromOrganization].ID = [FromPaymentRegister].OrganizationID " +
            "AND [FromOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [ToPaymentCurrencyRegister] " +
            "ON [ToPaymentCurrencyRegister].ID = [PaymentRegisterTransfer].ToPaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [ToCurrency] " +
            "ON [ToCurrency].ID = [ToPaymentCurrencyRegister].CurrencyID " +
            "AND [ToCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
            "ON [ToPaymentRegister].ID = [ToPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [ToOrganization] " +
            "ON [ToOrganization].ID = [ToPaymentRegister].OrganizationID " +
            "AND [ToOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [PaymentRegisterTransfer].UserID = [User].ID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [PaymentRegisterTransfer].ID = [PaymentMovementOperation].PaymentRegisterTransferID " +
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
            "WHERE [ToPaymentRegister].NetUID = @PaymentRegisterNetId " +
            "AND [PaymentRegisterTransfer].Created > @From " +
            "AND [PaymentRegisterTransfer].Created < @To";

        if (currencyNetId.HasValue) {
            sqlExpression += " AND [FromCurrency].NetUID = @CurrencyNetId" +
                             " AND [ToCurrency].NetUID = @CurrencyNetId";

            return _connection.Query(
                    sqlExpression,
                    types,
                    mapper,
                    new {
                        PaymentRegisterNetId = paymentRegisterNetId, CurrencyNetId = currencyNetId, From = from, To = to,
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                    }
                )
                .ToList();
        }

        return _connection.Query(
                sqlExpression,
                types,
                mapper,
                new { PaymentRegisterNetId = paymentRegisterNetId, From = from, To = to, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }
}