using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Repositories.Ecommerce.Contracts;

namespace GBA.Domain.Repositories.Ecommerce;

public sealed class EcommercePageRepository : IEcommercePageRepository {
    private readonly IDbConnection _connection;

    public EcommercePageRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(EcommercePage page) {
        return _connection.Query<long>(
                "INSERT INTO [EcommercePage] (PageName, TitleUa, TitleRu, DescriptionUa, DescriptionRu, KeyWords, LdJson, UrlUa, UrlRu, Updated) " +
                "VALUES (@PageName, @TitleUa, @TitleRu, @DescriptionUa, @DescriptionRu, @KeyWords, @LdJson, @UrlUa, @UrlRu, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                page)
            .SingleOrDefault();
    }

    public void Update(EcommercePage page) {
        _connection.Execute(
            "UPDATE [EcommercePage] " +
            "SET [PageName] = @PageName, [TitleUa] = @TitleUa, [TitleRu] = @TitleRu, [DescriptionUa] = @DescriptionUa, [DescriptionRu] = @DescriptionRu, [KeyWords] = @KeyWords, " +
            "[LdJson] = @LdJson, [UrlUa] = @UrlUa, [UrlRu] = @UrlRu, [Updated] = GETUTCDATE() " +
            "WHERE [NetUID] = @NetUid",
            page);
    }

    public EcommercePage GetById(long id) {
        return _connection.Query<EcommercePage>(
                "SELECT * FROM [EcommercePage] " +
                "WHERE [ID] = @Id",
                new { Id = id })
            .SingleOrDefault();
    }

    public EcommercePage GetByNetId(Guid netId) {
        return _connection.Query<EcommercePage>(
                "SELECT * FROM [EcommercePage] " +
                "WHERE [NetUID] = @NetId",
                new { NetId = netId })
            .SingleOrDefault();
    }

    public List<EcommercePage> GetAll() {
        return _connection.Query<EcommercePage>(
                "SELECT * FROM [EcommercePage] " +
                "WHERE [Deleted] = 0")
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [EcommercePage] " +
            "SET [Deleted] = 1 " +
            "WHERE [NetUID] = @NetId",
            new { NetId = netId });
    }

    public void AddList(List<EcommercePage> ecommercePages) {
        _connection.Execute(
            "INSERT INTO [EcommercePage] (PageName, TitleUa, TitleRu, DescriptionUa, DescriptionRu, KeyWords, LdJson, UrlUa, UrlRu, Updated) " +
            "VALUES (@PageName, @TitleUa, @TitleRu, @DescriptionUa, @DescriptionRu, @KeyWords, @LdJson, @UrlUa, @UrlRu, GETUTCDATE()); ",
            ecommercePages);
    }

    public List<SeoPage> GetAll(string locale) {
        return _connection.Query<SeoPage>(
                "SELECT * FROM [SeoPage] " +
                "WHERE [Deleted] = 0 " +
                "AND [Locale] = @Locale",
                new { Locale = locale })
            .ToList();
    }

    public void AddList(List<SeoPage> seoPages) {
        _connection.Execute(
            "INSERT INTO [SeoPage] (PageName, Title, Description, KeyWords, LdJson, Url, Locale, Updated) " +
            "VALUES (@PageName, @Title, @Description, @KeyWords, @LdJson, @Url, @Locale, GETUTCDATE()); ",
            seoPages);
    }

    public void Update(SeoPage page) {
        _connection.Execute(
            "UPDATE [SeoPage] " +
            "SET [PageName] = @PageName, [Title] = @Title, [Description] = @Description, [KeyWords] = @KeyWords, " +
            "[LdJson] = @LdJson, [Url] = @Url, [Updated] = GETUTCDATE() " +
            "WHERE [NetUID] = @NetUid " +
            "AND [Locale] = @Locale",
            page);
    }
}