using System;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Repositories.Ecommerce.Contracts;

namespace GBA.Domain.Repositories.Ecommerce;

public sealed class EcommerceContactInfoRepository : IEcommerceContactInfoRepository {
    private readonly IDbConnection _connection;

    public EcommerceContactInfoRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(EcommerceContactInfo info) {
        return _connection.Query<long>(
                "INSERT INTO [EcommerceContactInfo] (Address, Phone, Email, SiteUrl, Locale, Updated) " +
                "VALUES (@Address, @Phone, @Email, @SiteUrl, @Locale, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                info)
            .SingleOrDefault();
    }

    public void Update(EcommerceContactInfo info) {
        if (GetByNetId(info.NetUid) == null)
            Add(info);
        else
            _connection.Execute(
                "UPDATE [EcommerceContactInfo] " +
                "SET [Address] = @Address, [Phone] = @Phone, [Email] = @Email, [SiteUrl] = @SiteUrl, [Locale] = @Locale, [PixelId] = @PixelId, [Updated] = GETUTCDATE() " +
                "WHERE [NetUID] = @NetUid",
                info);
    }

    public void UpdateWithLocale(EcommerceContactInfo info) {
        if (GetByNetId(info.NetUid) == null)
            Add(info);
        else
            _connection.Execute(
                "UPDATE [EcommerceContactInfo] " +
                "SET [Address] = @Address, [Phone] = @Phone, [Email] = @Email, [SiteUrl] = @SiteUrl, [PixelId] = @PixelId, [Updated] = GETUTCDATE() " +
                "WHERE [NetUID] = @NetUid " +
                "AND [Locale] = @Locale",
                info);
    }

    public EcommerceContactInfo GetById(long id) {
        return _connection.Query<EcommerceContactInfo>(
                "SELECT * FROM [EcommerceContactInfo] " +
                "WHERE [ID] = @Id",
                new { Id = id })
            .SingleOrDefault();
    }

    public EcommerceContactInfo GetByNetId(Guid netId) {
        return _connection.Query<EcommerceContactInfo>(
                "SELECT * FROM [EcommerceContactInfo] " +
                "WHERE [NetUID] = @NetId",
                new { NetId = netId })
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [EcommerceContactInfo] " +
            "SET [Deleted] = 1 " +
            "WHERE [NetUID] = @NetId");
    }

    public EcommerceContactInfo GetLast() {
        return _connection.Query<EcommerceContactInfo>(
                "SELECT TOP(1) * FROM [EcommerceContactInfo] " +
                "WHERE [Deleted] = 0")
            .SingleOrDefault();
    }

    public EcommerceContactInfo GetLast(string locale) {
        return _connection.Query<EcommerceContactInfo>(
                "SELECT TOP(1) * FROM [EcommerceContactInfo] " +
                "WHERE [Deleted] = 0 " +
                "AND [Locale] = @Locale",
                new { Locale = locale })
            .SingleOrDefault();
    }
}