using System.Data;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class BaseSalePaymentStatusRepository : IBaseSalePaymentStatusRepository {
    private readonly IDbConnection _connection;

    public BaseSalePaymentStatusRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(BaseSalePaymentStatus baseSalePaymentStatus) {
        return _connection.Query<long>(
                "INSERT INTO BaseSalePaymentStatus (SalePaymentStatusType, Amount, Updated) " +
                "VALUES(@SalePaymentStatusType, @Amount, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                baseSalePaymentStatus
            )
            .Single();
    }

    public void Update(BaseSalePaymentStatus baseSalePaymentStatus) {
        _connection.Query<long>(
                "UPDATE BaseSalePaymentStatus " +
                "SET SalePaymentStatusType = @SalePaymentStatusType, Amount = @Amount, Updated = getutcdate() " +
                "WHERE NetUid = @NetUid",
                baseSalePaymentStatus
            )
            .SingleOrDefault();
    }

    public void SetSalePaymentStatusTypeById(SalePaymentStatusType type, long id) {
        _connection.Execute(
            "UPDATE [BaseSalePaymentStatus] " +
            "SET SalePaymentStatusType = @Type, Updated = getutcdate() " +
            "WHERE [BaseSalePaymentStatus].ID = @Id",
            new { Type = type, Id = id }
        );
    }
}