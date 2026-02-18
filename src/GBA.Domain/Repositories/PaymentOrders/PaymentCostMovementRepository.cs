using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class PaymentCostMovementRepository : IPaymentCostMovementRepository {
    private readonly IDbConnection _connection;

    public PaymentCostMovementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PaymentCostMovement paymentCostMovement) {
        return _connection.Query<long>(
                "INSERT INTO [PaymentCostMovement] (OperationName, Updated) " +
                "VALUES (@OperationName, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                paymentCostMovement
            )
            .Single();
    }

    public void Update(PaymentCostMovement paymentCostMovement) {
        _connection.Execute(
            "UPDATE [PaymentCostMovement] " +
            "SET OperationName = @OperationName, Updated = getutcdate() " +
            "WHERE [PaymentCostMovement].ID = @Id",
            paymentCostMovement
        );
    }

    public PaymentCostMovement GetById(long id) {
        return _connection.Query<PaymentCostMovement>(
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
                "WHERE [PaymentCostMovement].ID = @Id",
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public PaymentCostMovement GetByNetId(Guid netId) {
        return _connection.Query<PaymentCostMovement>(
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
                "WHERE [PaymentCostMovement].NetUID = @NetId",
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public IEnumerable<PaymentCostMovement> GetAllFromSearch(string value) {
        return _connection.Query<PaymentCostMovement>(
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
            "WHERE [PaymentCostMovement].Deleted = 0 " +
            "AND (" +
            "[PaymentCostMovement].OperationName like '%' + @Value + '%' " +
            "OR " +
            "[PaymentCostMovementTranslation].OperationName like '%' + @Value + '%'" +
            ") " +
            "ORDER BY [PaymentCostMovement].[OperationName], [PaymentCostMovementTranslation].[OperationName]",
            new { Value = value, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<PaymentCostMovement> GetAll() {
        return _connection.Query<PaymentCostMovement>(
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
            "WHERE [PaymentCostMovement].Deleted = 0 " +
            "ORDER BY [PaymentCostMovement].[OperationName], [PaymentCostMovementTranslation].[OperationName]",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [PaymentCostMovement] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [PaymentCostMovement].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}