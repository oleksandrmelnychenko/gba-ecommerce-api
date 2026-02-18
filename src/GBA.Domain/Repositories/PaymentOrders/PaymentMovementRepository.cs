using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class PaymentMovementRepository : IPaymentMovementRepository {
    private readonly IDbConnection _connection;

    public PaymentMovementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PaymentMovement paymentMovement) {
        return _connection.Query<long>(
                "INSERT INTO [PaymentMovement] (OperationName, Updated) " +
                "VALUES (@OperationName, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                paymentMovement
            )
            .Single();
    }

    public void Update(PaymentMovement paymentMovement) {
        _connection.Execute(
            "UPDATE [PaymentMovement] " +
            "SET OperationName = @OperationName, Updated = getutcdate() " +
            "WHERE [PaymentMovement].ID = @Id",
            paymentMovement
        );
    }

    public PaymentMovement GetById(long id) {
        return _connection.Query<PaymentMovement>(
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
                "WHERE [PaymentMovement].ID = @Id",
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public PaymentMovement GetByNetId(Guid netId) {
        return _connection.Query<PaymentMovement>(
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
                "WHERE [PaymentMovement].NetUID = @NetId",
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public IEnumerable<PaymentMovement> GetAllFromSearch(string value) {
        return _connection.Query<PaymentMovement>(
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
            "WHERE [PaymentMovement].Deleted = 0 " +
            "AND (" +
            "[PaymentMovement].OperationName like '%' + @Value + '%' " +
            "OR " +
            "[PaymentMovementTranslation].Name like '%' + @Value + '%'" +
            ") " +
            "ORDER BY [PaymentMovement].[OperationName], [PaymentMovementTranslation].[Name]",
            new { Value = value, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<PaymentMovement> GetAll() {
        return _connection.Query<PaymentMovement>(
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
            "WHERE [PaymentMovement].Deleted = 0 " +
            "ORDER BY [PaymentMovement].[OperationName], [PaymentMovementTranslation].[Name]",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [PaymentMovement] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [PaymentMovement].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}