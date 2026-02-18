using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Repositories.Ecommerce.Contracts;

namespace GBA.Domain.Repositories.Ecommerce;

public sealed class EcommerceContactsRepository : IEcommerceContactsRepository {
    private readonly IDbConnection _connection;

    public EcommerceContactsRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(EcommerceContacts contacts) {
        return _connection.Query<long>(
                "INSERT INTO [EcommerceContacts] ([Name], [Phone], [Skype], [Email], [Icq], [ImgUrl], [Updated]) " +
                "VALUES (@Name, @Phone, @Skype, @Email, @Icq, @ImgUrl, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                contacts)
            .SingleOrDefault();
    }

    public void Update(EcommerceContacts contacts) {
        _connection.Execute(
            "UPDATE [EcommerceContacts] " +
            "SET [Name] = @Name, [Phone] = @Phone, [Skype] = @Skype, [Email] = @Email, [Icq] = @Icq, [ImgUrl] = @ImgUrl, [Updated] = GETUTCDATE() " +
            "WHERE [NetUID] = @NetUid",
            contacts);
    }

    public EcommerceContacts GetById(long id) {
        return _connection.Query<EcommerceContacts>(
                "SELECT * FROM [EcommerceContacts] " +
                "WHERE [ID] = @Id",
                new { Id = id })
            .SingleOrDefault();
    }

    public EcommerceContacts GetByNetId(Guid netId) {
        return _connection.Query<EcommerceContacts>(
                "SELECT * FROM [EcommerceContacts] " +
                "WHERE [NetUID] = @NetId " +
                "AND [Deleted] = 0",
                new { NetId = netId })
            .SingleOrDefault();
    }

    public List<EcommerceContacts> GetAll() {
        return _connection.Query<EcommerceContacts>(
                "SELECT * FROM [EcommerceContacts] " +
                "WHERE [Deleted] = 0")
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [EcommerceContacts] " +
            "SET [Deleted] = 1 " +
            "WHERE [NetUID] = @NetId",
            new { NetId = netId });
    }
}