using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Supplies;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class ActReconciliationRepository : IActReconciliationRepository {
    private readonly IDbConnection _connection;

    public ActReconciliationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ActReconciliation actReconciliation) {
        return _connection.Query<long>(
                "INSERT INTO [ActReconciliation] (Number, Comment, FromDate, ResponsibleId, SupplyOrderUkraineId, SupplyInvoiceId, Updated) " +
                "VALUES (@Number, @Comment, @FromDate, @ResponsibleId, @SupplyOrderUkraineId, @SupplyInvoiceId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                actReconciliation
            )
            .Single();
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [ActReconciliation] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ActReconciliation].ID = @Id",
            new { Id = id }
        );
    }

    public ActReconciliation GetLastRecord() {
        return _connection.Query<ActReconciliation>(
                "SELECT TOP(1) * " +
                "FROM [ActReconciliation] " +
                "WHERE [ActReconciliation].Deleted = 0 " +
                "ORDER BY [ActReconciliation].ID DESC"
            )
            .SingleOrDefault();
    }

    public ActReconciliation GetBySupplyOrderUkraineId(long id) {
        return _connection.Query<ActReconciliation>(
                "SELECT TOP(1) * " +
                "FROM [ActReconciliation] " +
                "WHERE [ActReconciliation].Deleted = 0 " +
                "AND [ActReconciliation].SupplyOrderUkraineID = @Id " +
                "ORDER BY [ActReconciliation].ID DESC",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public ActReconciliation GetBySupplyInvoiceId(long id) {
        return _connection.Query<ActReconciliation>(
                "SELECT TOP(1) * " +
                "FROM [ActReconciliation] " +
                "WHERE [ActReconciliation].Deleted = 0 " +
                "AND [ActReconciliation].SupplyInvoiceID = @Id " +
                "ORDER BY [ActReconciliation].ID DESC",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public ActReconciliation GetByIdIfExists(long id) {
        return _connection.Query<ActReconciliation>(
            "SELECT * " +
            "FROM [ActReconciliation] " +
            "WHERE [ActReconciliation].ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public ActReconciliation GetById(long id) {
        ActReconciliation toReturn =
            _connection.Query<ActReconciliation, User, SupplyOrderUkraine, ActReconciliation>(
                    "SELECT * " +
                    "FROM [ActReconciliation] " +
                    "LEFT JOIN [User] AS [Responsible] " +
                    "ON [ActReconciliation].ResponsibleID = [Responsible].ID " +
                    "LEFT JOIN [SupplyOrderUkraine] " +
                    "ON [ActReconciliation].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
                    "WHERE [ActReconciliation].ID = @Id",
                    (act, responsible, orderUkraine) => {
                        act.Responsible = responsible;
                        act.SupplyOrderUkraine = orderUkraine;

                        return act;
                    },
                    new { Id = id }
                )
                .SingleOrDefault();

        if (toReturn != null)
            toReturn.ActReconciliationItems =
                _connection.Query<ActReconciliationItem, Product, ActReconciliationItem>(
                    "SELECT * " +
                    "FROM [ActReconciliationItem] " +
                    "LEFT JOIN [Product] " +
                    "ON [ActReconciliationItem].ProductID = [Product].ID " +
                    "WHERE [ActReconciliationItem].Deleted = 0 " +
                    "AND [ActReconciliationItem].ActReconciliationID = @Id",
                    (item, product) => {
                        item.Product = product;

                        return item;
                    },
                    new { toReturn.Id }
                ).ToList();

        return toReturn;
    }

    public ActReconciliation GetByNetId(Guid netId) {
        ActReconciliation toReturn =
            _connection.Query<ActReconciliation, User, SupplyOrderUkraine, Organization, SupplyInvoice, SupplyOrder, Organization, ActReconciliation>(
                    "SELECT * " +
                    "FROM [ActReconciliation] " +
                    "LEFT JOIN [User] AS [Responsible] " +
                    "ON [ActReconciliation].ResponsibleID = [Responsible].ID " +
                    "LEFT JOIN [SupplyOrderUkraine] " +
                    "ON [ActReconciliation].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
                    "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                    "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
                    "AND [Organization].CultureCode = @Culture " +
                    "LEFT JOIN [SupplyInvoice] " +
                    "ON [SupplyInvoice].ID = [ActReconciliation].SupplyInvoiceID " +
                    "LEFT JOIN [SupplyOrder] " +
                    "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                    "LEFT JOIN [views].[OrganizationView] AS [SupplyOrderOrganization] " +
                    "ON [SupplyOrderOrganization].ID = [SupplyOrder].OrganizationID " +
                    "AND [SupplyOrderOrganization].CultureCode = @Culture " +
                    "WHERE [ActReconciliation].NetUID = @NetId",
                    (act, responsible, orderUkraine, organization, supplyInvoice, supplyOrder, supplyOrderOrganization) => {
                        if (orderUkraine != null) orderUkraine.Organization = organization;

                        if (supplyInvoice != null) {
                            supplyOrder.Organization = supplyOrderOrganization;

                            supplyInvoice.SupplyOrder = supplyOrder;
                        }

                        act.Responsible = responsible;
                        act.SupplyOrderUkraine = orderUkraine;
                        act.SupplyInvoice = supplyInvoice;

                        return act;
                    },
                    new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
                )
                .SingleOrDefault();

        if (toReturn != null)
            _connection.Query<ActReconciliationItem, Product, Storage, ProductAvailability, ActReconciliationItem>(
                "SELECT * " +
                "FROM [ActReconciliationItem] " +
                "LEFT JOIN [Product] " +
                "ON [ActReconciliationItem].ProductID = [Product].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].Deleted = 0 " +
                "AND [Storage].ForDefective = 1 " +
                "LEFT JOIN [ProductAvailability] " +
                "ON [ProductAvailability].StorageID = [Storage].ID " +
                "AND [ProductAvailability].ProductID = [Product].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "AND [ProductAvailability].Amount <> 0 " +
                "WHERE [ActReconciliationItem].Deleted = 0 " +
                "AND [ActReconciliationItem].ActReconciliationID = @Id",
                (item, product, strorage, availability) => {
                    if (!toReturn.ActReconciliationItems.Any(i => i.Id.Equals(item.Id))) {
                        item.Product = product;

                        if (strorage != null)
                            item.Availabilities.Add(new ActReconciliationItemStorageAvailability {
                                Storage = strorage,
                                Qty = availability?.Amount ?? 0d
                            });

                        toReturn.ActReconciliationItems.Add(item);
                    } else {
                        if (strorage != null)
                            toReturn
                                .ActReconciliationItems
                                .First(i => i.Id.Equals(item.Id))
                                .Availabilities.Add(new ActReconciliationItemStorageAvailability {
                                    Storage = strorage,
                                    Qty = availability?.Amount ?? 0d
                                });
                    }

                    return item;
                },
                new { toReturn.Id }
            );

        return toReturn;
    }

    public List<ActReconciliation> GetAll() {
        List<ActReconciliation> acts =
            _connection.Query<ActReconciliation, User, SupplyOrderUkraine, ActReconciliation>(
                "SELECT * " +
                "FROM [ActReconciliation] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [ActReconciliation].ResponsibleID = [Responsible].ID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [ActReconciliation].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
                "WHERE [ActReconciliation].Deleted = 0 " +
                "ORDER BY [ActReconciliation].ID DESC",
                (act, responsible, orderUkraine) => {
                    act.Responsible = responsible;
                    act.SupplyOrderUkraine = orderUkraine;

                    return act;
                }
            ).ToList();

        if (acts.Any())
            _connection.Query<ActReconciliationItem, Product, ActReconciliationItem>(
                "SELECT * " +
                "FROM [ActReconciliationItem] " +
                "LEFT JOIN [Product] " +
                "ON [ActReconciliationItem].ProductID = [Product].ID " +
                "WHERE [ActReconciliationItem].Deleted = 0 " +
                "AND [ActReconciliationItem].ActReconciliationID IN @Ids",
                (item, product) => {
                    ActReconciliation act = acts.First(a => a.Id.Equals(item.ActReconciliationId));

                    if (!act.ActReconciliationItems.Any(i => i.Id.Equals(item.Id))) {
                        item.Product = product;

                        act.ActReconciliationItems.Add(item);
                    }

                    return item;
                },
                new { Ids = acts.Select(a => a.Id) }
            );

        return acts;
    }

    public List<ActReconciliation> GetAllFiltered(DateTime from, DateTime to) {
        List<ActReconciliation> acts =
            _connection.Query<ActReconciliation, User, SupplyOrderUkraine, Organization, SupplyInvoice, SupplyOrder, Organization, ActReconciliation>(
                "SELECT * " +
                "FROM [ActReconciliation] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [ActReconciliation].ResponsibleID = [Responsible].ID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [ActReconciliation].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [ActReconciliation].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [views].[OrganizationView] AS [SupplyOrderOrganization] " +
                "ON [SupplyOrderOrganization].ID = [SupplyOrder].OrganizationID " +
                "AND [SupplyOrderOrganization].CultureCode = @Culture " +
                "WHERE [ActReconciliation].Deleted = 0 " +
                "AND [ActReconciliation].FromDate >= @From " +
                "AND [ActReconciliation].FromDate <= @To " +
                "ORDER BY [ActReconciliation].ID DESC",
                (act, responsible, orderUkraine, organization, supplyInvoice, supplyOrder, supplyOrderOrganization) => {
                    if (orderUkraine != null) orderUkraine.Organization = organization;

                    if (supplyInvoice != null) {
                        supplyOrder.Organization = supplyOrderOrganization;

                        supplyInvoice.SupplyOrder = supplyOrder;
                    }

                    act.Responsible = responsible;
                    act.SupplyOrderUkraine = orderUkraine;
                    act.SupplyInvoice = supplyInvoice;

                    return act;
                },
                new { From = from, To = to, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            ).ToList();

        if (acts.Any())
            _connection.Query<ActReconciliationItem, Product, ActReconciliationItem>(
                "SELECT * " +
                "FROM [ActReconciliationItem] " +
                "LEFT JOIN [Product] " +
                "ON [ActReconciliationItem].ProductID = [Product].ID " +
                "WHERE [ActReconciliationItem].Deleted = 0 " +
                "AND [ActReconciliationItem].ActReconciliationID IN @Ids",
                (item, product) => {
                    ActReconciliation act = acts.First(a => a.Id.Equals(item.ActReconciliationId));

                    if (!act.ActReconciliationItems.Any(i => i.Id.Equals(item.Id))) {
                        item.Product = product;

                        act.ActReconciliationItems.Add(item);
                    }

                    return item;
                },
                new { Ids = acts.Select(a => a.Id) }
            );

        return acts;
    }
}