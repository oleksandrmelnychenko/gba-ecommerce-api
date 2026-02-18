using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.Storages.Contracts;

namespace GBA.Domain.Repositories.Storages;

public sealed class StorageRepository : IStorageRepository {
    private readonly IDbConnection _connection;

    public StorageRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Storage storage) {
        return _connection.Query<long>(
            "INSERT INTO Storage (Name, Locale, ForDefective, ForVatProducts, OrganizationID, ForEcommerce, Updated) " +
            "VALUES(@Name, @Locale, @ForDefective, @ForVatProducts, @OrganizationId, @ForEcommerce, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            storage
        ).Single();
    }

    public List<Storage> GetAll() {
        List<Storage> storages =
            _connection.Query<Storage, Organization, Storage>(
                "SELECT * " +
                "FROM [Storage] " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [Storage].OrganizationID " +
                "WHERE [Storage].Deleted = 0",
                (storage, organization) => {
                    storage.Organization = organization;

                    return storage;
                }
            ).ToList();

        return CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")
            ? storages.OrderBy(s => s.Locale).ToList()
            : storages.OrderByDescending(s => s.Locale).ToList();
    }

    public IEnumerable<Storage> GetAllNonDefectiveByCurrentLocale() {
        return _connection.Query<Storage>(
            "SELECT * " +
            "FROM [Storage] " +
            "WHERE [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Deleted = 0 " +
            "ORDER BY [Storage].[Name]",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<Storage> GetAllDefectiveByCurrentLocale() {
        return _connection.Query<Storage>(
            "SELECT * " +
            "FROM [Storage] " +
            "WHERE [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 1 " +
            "AND [Storage].Deleted = 0 " +
            "ORDER BY [Storage].[Name]",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<Storage> GetAllWithOrganizations() {
        return _connection.Query<Storage, Organization, Storage>(
            "SELECT * " +
            "FROM [Storage] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [Storage].Deleted = 0 " +
            "ORDER BY (CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END) " +
            ", (CASE WHEN [Storage].ForDefective = 0 THEN 0 ELSE 1 END) " +
            ", (CASE WHEN [Storage].ForVatProducts = 1 THEN 0 ELSE 1 END) " +
            ", [Storage].[Name]",
            (storage, organization) => {
                storage.Organization = organization;

                return storage;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<Storage> GetAllFilteredByOrganizationNetId(Guid organizationNetId, bool skipDefective) {
        return _connection.Query<Storage, Organization, Storage>(
            "SELECT * " +
            "FROM [Storage] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [Storage].Deleted = 0 " +
            "AND [Organization].NetUID = @OrganizationNetId " +
            (
                skipDefective
                    ? "AND [Storage].ForDefective = 0 "
                    : string.Empty
            ) +
            "ORDER BY (CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END) " +
            ", (CASE WHEN [Storage].ForDefective = 0 THEN 0 ELSE 1 END) " +
            ", (CASE WHEN [Storage].ForVatProducts = 1 THEN 0 ELSE 1 END) " +
            ", [Storage].[Name]",
            (storage, organization) => {
                storage.Organization = organization;

                return storage;
            },
            new { OrganizationNetId = organizationNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<Storage> GetAllForEcommerce() {
        return _connection.Query<Storage, Organization, Storage>(
            "SELECT * " +
            "FROM [Storage] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "WHERE [Storage].Deleted = 0 " +
            "AND [Storage].ForEcommerce = 1",
            (storage, organization) => {
                storage.Organization = organization;

                return storage;
            });
    }

    public Storage GetById(long id) {
        return _connection.Query<Storage>(
            "SELECT * FROM Storage WHERE ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public Storage GetReSale() {
        return _connection.Query<Storage, Organization, Storage>(
            "SELECT * FROM [Storage] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "WHERE IsResale = 1",
            (storage, organization) => {
                storage.Organization = organization;
                return storage;
            }
        ).SingleOrDefault();
    }

    public Storage GetWithHighestPriority() {
        return _connection.Query<Storage, Organization, Storage>(
            "SELECT TOP 1 * " +
            "FROM [Storage] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "WHERE [Storage].Deleted = 0 " +
            "AND [Storage].ForEcommerce = 1 " +
            "ORDER BY [Storage].RetailPriority ASC ",
            (storage, organization) => {
                storage.Organization = organization;
                return storage;
            }).FirstOrDefault();
    }

    public long GetTotalProductsCountByStorageNetId(Guid netId) {
        return _connection.Query<long>(
            "SELECT SUM(ProductAvailability.Amount) FROM Storage " +
            "LEFT JOIN ProductAvailability " +
            "ON ProductAvailability.StorageID = Storage.ID " +
            "AND ProductAvailability.Deleted = 0 " +
            "WHERE Storage.NetUID = @NetId",
            new { NetId = netId }
        ).Single();
    }

    public IEnumerable<Storage> GetAllForReturns(bool onlyDefective = false) {
        string sqlExpression =
            "SELECT * " +
            "FROM [Storage] " +
            "WHERE [Storage].Deleted = 0 ";

        sqlExpression +=
            onlyDefective
                ? "AND [Storage].ForDefective = 1 " +
                  "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END, [Storage].[Name]"
                : "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END, [Storage].[Name]";

        return _connection.Query<Storage>(
            sqlExpression,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<Storage> GetAllForReturnsFiltered(
        Guid organizationNetId,
        Guid? orderItemNetId,
        bool onlyDefective = false) {
        Storage fromOrderItem = new();

        if (orderItemNetId.HasValue) {
            fromOrderItem =
                _connection.Query<Storage, Organization, Storage>(
                        "SELECT TOP 1 [Storage].*, [Organization].* FROM [OrderItem] " +
                        "LEFT JOIN [ProductReservation] " +
                        "ON [ProductReservation].[OrderItemID] = [OrderItem].[ID] " +
                        "LEFT JOIN [ConsignmentItem] " +
                        "ON [ConsignmentItem].[ID] = [ProductReservation].[ConsignmentItemID] " +
                        "LEFT JOIN [Consignment] " +
                        "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID] " +
                        "LEFT JOIN [Storage] " +
                        "ON [Storage].[ID] = [Consignment].[StorageID] " +
                        "LEFT JOIN [Organization] " +
                        "ON [Organization].ID = [Storage].OrganizationID " +
                        "WHERE [OrderItem].[NetUID] = @NetId; ",
                        (storage, organization) => {
                            storage.Organization = organization;
                            return storage;
                        },
                        new { NetId = orderItemNetId.Value })
                    .FirstOrDefault();

            if (fromOrderItem != null) organizationNetId = fromOrderItem.Organization.NetUid;
        }

        string fromOrderItemExist = fromOrderItem == null || fromOrderItem.IsNew()
            ? "AND [Storage].ForDefective = 1 "
            : "AND ([Storage].ForDefective = 1 OR [Storage].[ID] = @FromItemId) ";

        string sqlExpression =
            "SELECT * " +
            "FROM [Storage] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "WHERE [Storage].Deleted = 0 " +
            "AND [Organization].NetUID = @OrganizationNetId ";

        sqlExpression +=
            onlyDefective
                ? fromOrderItemExist +
                  "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END, [Storage].[Name]"
                : "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END, [Storage].[Name]";

        IEnumerable<Storage> storages = _connection.Query<Storage, Organization, Storage>(
            sqlExpression,
            (storage, organization) => {
                storage.Organization = organization;

                return storage;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, OrganizationNetId = organizationNetId,
                FromItemId = fromOrderItem?.Id ?? default
            }
        );

        if (!storages.Any()) {
            sqlExpression =
                "SELECT * " +
                "FROM [Storage] " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].StorageID = [Storage].ID " +
                "WHERE [Storage].Deleted = 0 " +
                "AND [Organization].NetUID = @OrganizationNetId ";
            storages = _connection.Query<Storage, Organization, Storage>(
                sqlExpression,
                (storage, organization) => {
                    storage.Organization = organization;

                    return storage;
                },
                new {
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    OrganizationNetId = organizationNetId,
                    FromItemId = fromOrderItem?.Id ?? default
                }
            );
        }

        return storages;
    }

    public Storage GetByLocale(string locale, bool withDefective = false) {
        string sqlExpression =
            "SELECT TOP(1) * " +
            "FROM [Storage] " +
            "WHERE [Storage].Locale = @Locale " +
            "AND [Storage].Deleted = 0 ";

        sqlExpression +=
            !withDefective
                ? "AND [Storage].ForDefective = 0 "
                : string.Empty;

        return _connection.Query<Storage>(
            sqlExpression,
            new { Locale = locale }
        ).SingleOrDefault();
    }

    public Storage GetByNetId(Guid netId) {
        return _connection.Query<Storage, Organization, Storage>(
            "SELECT * " +
            "FROM [Storage] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "WHERE [Storage].NetUID = @NetId",
            (storage, organization) => {
                storage.Organization = organization;

                return storage;
            },
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public long GetIdByLocale(string locale) {
        return _connection.Query<long>(
            "SELECT TOP(1) ID " +
            "FROM [Storage] " +
            "WHERE [Storage].Locale = @Locale " +
            "AND [Storage].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 ",
            new { Locale = locale }
        ).SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Storage SET Deleted = 1 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void SetStorageForEcommerce(Guid netId) {
        _connection.Execute(
            "UPDATE Storage SET ForEcommerce = 1 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void SetStoragePriority(long storageId, int priority) {
        _connection.Execute(
            "UPDATE Storage SET RetailPriority = @Priority WHERE ID = @Id",
            new { Id = storageId, Priority = priority }
        );
    }

    public void UnselectStorageForEcommerce(Guid netId) {
        _connection.Execute(
            "UPDATE Storage SET ForEcommerce = 0 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }


    public void Update(Storage storage) {
        _connection.Execute(
            "UPDATE [Storage] SET " +
            "[Name] = @Name, " +
            "[ForDefective] = @ForDefective, " +
            "[ForVatProducts] = @ForVatProducts, " +
            "[OrganizationId] = @OrganizationId, " +
            "[IsResale] = @IsResale, " +
            "[Updated] = getutcdate(), " +
            "[AvailableForReSale] = @AvailableForReSale, " +
            "[ForEcommerce] = @ForEcommerce, " +
            "[RetailPriority] = @RetailPriority " +
            "WHERE NetUID = @NetUid",
            storage
        );
    }

    public IEnumerable<Storage> GetAllForReSaleAvailabilities() {
        return _connection.Query<Storage>(
            ";WITH [FILTERED_STORAGES_CTE] AS ( " +
            "SELECT [Storage].[ID] FROM [ReSaleAvailability] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID]" +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [Consignment].[StorageID] " +
            "WHERE [ReSaleAvailability].[Deleted] = 0 " +
            "AND [ReSaleAvailability].[RemainingQty] > 0 " +
            "GROUP BY [Storage].[ID] " +
            ") " +
            "SELECT * FROM [Storage] " +
            "WHERE [Storage].[ID] IN ( " +
            "SELECT [FILTERED_STORAGES_CTE].[ID] " +
            "FROM [FILTERED_STORAGES_CTE] " +
            ") ");
    }
}