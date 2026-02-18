using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;

namespace GBA.Domain.Repositories.Clients.RetailClients;

public sealed class RetailClientPaymentImageItemRepository : IRetailClientPaymentImageItemRepository {
    private readonly IDbConnection _connection;

    public RetailClientPaymentImageItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(RetailClientPaymentImageItem paymentImageItem) {
        return _connection.Query<long>(
            "INSERT INTO [RetailClientPaymentImageItem] " +
            "([ImgUrl], [Amount], [UserID], [RetailClientPaymentImageID], [Updated], [PaymentType], [Comment], [IsLocked]) " +
            "VALUES (@ImgUrl, @Amount, @UserId, @RetailClientPaymentImageId, GETUTCDATE(), @PaymentType, @Comment, @IsLocked); " +
            "SELECT SCOPE_IDENTITY()",
            paymentImageItem).First();
    }

    public void Update(RetailClientPaymentImageItem paymentImageItem) {
        _connection.Execute(
            "UPDATE [RetailClientPaymentImageItem] " +
            "SET [ImgUrl] = @ImgUrl " +
            ", [Amount] = @Amount " +
            ", [UserID] = @UserId " +
            ", [RetailClientPaymentImageID] = @RetailClientPaymentImageId " +
            ", [Updated] = GETUTCDATE() " +
            ", [PaymentType] = @PaymentType " +
            ", [Comment] = @Comment " +
            ", [IsLocked] = @IsLocked " +
            "WHERE [RetailClientPaymentImageItem].[ID] = @Id ",
            paymentImageItem);
    }

    public void Update(IEnumerable<RetailClientPaymentImageItem> paymentImageItems) {
        _connection.Execute(
            "UPDATE [RetailClientPaymentImageItem] " +
            "SET [ImgUrl] = @ImgUrl " +
            ", [Amount] = @Amount " +
            ", [UserID] = @UserId " +
            ", [RetailClientPaymentImageID] = @RetailClientPaymentImageId " +
            ", [Updated] = GETUTCDATE() " +
            ", [PaymentType] = @PaymentType " +
            ", [Comment] = @Comment " +
            ", [IsLocked] = @IsLocked " +
            "WHERE [RetailClientPaymentImageItem].[ID] = @Id ",
            paymentImageItems);
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [RetailClientPaymentImageItem] SET " +
            "[Deleted] = 1 " +
            "WHERE ID = @Id ",
            new { Id = id });
    }

    public RetailClientPaymentImageItem GetById(long id) {
        return _connection.Query<RetailClientPaymentImageItem>(
            "SELECT * FROM [RetailClientPaymentImageItem] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [RetailClientPaymentImageItem].UserID " +
            "WHERE [RetailClientPaymentImageItem].ID = @Id ",
            new { Id = id }).FirstOrDefault();
    }


    public IEnumerable<RetailClientPaymentImageItem> GetAllByRetailClientPaymentImageId(long id) {
        return _connection.Query<RetailClientPaymentImageItem>(
            "SELECT * FROM [RetailClientPaymentImageItem] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [RetailClientPaymentImageItem].UserID " +
            "WHERE [RetailClientPaymentImageItem].RetailClientPaymentImageID = @Id ",
            new { Id = id });
    }
}