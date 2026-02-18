using System.Data;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;

namespace GBA.Domain.Repositories.Clients.RetailClients;

public sealed class RetailPaymentStatusRepository : IRetailPaymentStatusRepository {
    private readonly IDbConnection _connection;

    public RetailPaymentStatusRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(RetailPaymentStatus retailPaymentStatus) {
        return _connection.Query<long>(
            "INSERT INTO [RetailPaymentStatus] " +
            "([RetailPaymentStatusType], [Amount], [PaidAmount], [Updated]) " +
            "VALUES (@RetailPaymentStatusType, @Amount, @PaidAmount, GETUTCDATE()) " +
            "SELECT SCOPE_IDENTITY(); ",
            retailPaymentStatus
        ).FirstOrDefault();
    }

    public void Update(RetailPaymentStatus retailPaymentStatus) {
        _connection.Execute(
            "UPDATE [RetailPaymentStatus] SET " +
            "[RetailPaymentStatusType] = @RetailPaymentStatusType, " +
            "[Amount] = @Amount, " +
            "[PaidAmount] = @PaidAmount, " +
            "[Updated] = GETUTCDATE() " +
            "WHERE [RetailPaymentStatus].ID = @Id ",
            retailPaymentStatus);
    }

    public void SetRetailPaymentStatusTypeById(RetailPaymentStatusType type, long id) {
        _connection.Execute(
            "UPDATE [RetailPaymentStatus] SET " +
            "[RetailPaymentStatusType] = @Type, " +
            "[Updated] = GETUTCDATE() " +
            "WHERE [RetailPaymentStatus].ID = @Id ",
            new { Type = type, Id = id });
    }

    public RetailPaymentStatus GetById(long id) {
        return _connection.Query<RetailPaymentStatus>(
            "SELECT " +
            "[RetailPaymentStatus].* " +
            ", [RetailPaymentStatus].Amount - [RetailPaymentStatus].PaidAmount AS AmountToPay " +
            "FROM [RetailPaymentStatus] " +
            "WHERE ID = @Id " +
            "AND Deleted = 0 ",
            new { Id = id }
        ).FirstOrDefault();
    }

    public RetailPaymentStatus GetBySaleId(long id) {
        return _connection.Query<RetailPaymentStatus>(
            "SELECT " +
            "[RetailPaymentStatus].* " +
            ", [RetailPaymentStatus].Amount - [RetailPaymentStatus].PaidAmount AS AmountToPay " +
            "FROM [RetailPaymentStatus] " +
            "LEFT JOIN [RetailClientPaymentImage] " +
            "ON [RetailClientPaymentImage].RetailPaymentStatusId = [RetailPaymentStatus].ID " +
            "WHERE [RetailClientPaymentImage].SaleId = @SaleId " +
            "AND [RetailClientPaymentImage].Deleted = 0 ",
            new { SaleId = id }
        ).FirstOrDefault();
    }
}