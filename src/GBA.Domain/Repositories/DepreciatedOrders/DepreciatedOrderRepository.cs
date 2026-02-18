using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.DepreciatedOrders.Contracts;

namespace GBA.Domain.Repositories.DepreciatedOrders;

public sealed class DepreciatedOrderRepository : IDepreciatedOrderRepository {
    private readonly IDbConnection _connection;

    public DepreciatedOrderRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(DepreciatedOrder depreciatedOrder) {
        return _connection.Query<long>(
            "INSERT INTO [DepreciatedOrder] (Number, Comment, FromDate, StorageId, ResponsibleId, OrganizationId, Updated, " +
            "[IsManagement]) " +
            "VALUES (@Number, @Comment, @FromDate, @StorageId, @ResponsibleId, @OrganizationId, GETUTCDATE(), " +
            "@IsManagement); " +
            "SELECT SCOPE_IDENTITY()",
            depreciatedOrder
        ).Single();
    }

    public void Update(DepreciatedOrder depreciatedOrder) {
        _connection.Execute(
            "UPDATE [DepreciatedOrder] " +
            "SET Comment = @Comment, FromDate = @FromDate, Updated = GETUTCDATE(), " +
            "[IsManagement] = @IsManagement " +
            "WHERE [DepreciatedOrder].ID = @Id",
            depreciatedOrder
        );
    }

    public DepreciatedOrder GetLastRecord(string culture) {
        string sqlExpression =
            "SELECT TOP(1) * " +
            "FROM [DepreciatedOrder] " +
            "WHERE [DepreciatedOrder].Deleted = 0 ";

        sqlExpression +=
            culture.ToLower().Equals("pl")
                ? "AND [DepreciatedOrder].Number like 'P%' "
                : "AND [DepreciatedOrder].Number NOT like 'P%' ";

        sqlExpression +=
            "ORDER BY [DepreciatedOrder].ID DESC";

        return _connection.Query<DepreciatedOrder>(
            sqlExpression
        ).SingleOrDefault();
    }

    public DepreciatedOrder GetById(long id) {
        DepreciatedOrder toReturn = null;

        _connection.Query<DepreciatedOrder, Storage, User, Organization, DepreciatedOrder>(
            "SELECT * " +
            "FROM [DepreciatedOrder] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [DepreciatedOrder].StorageID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [DepreciatedOrder].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [DepreciatedOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [DepreciatedOrder].ID = @Id",
            (order, storage, responsible, organization) => {
                order.Storage = storage;
                order.Responsible = responsible;
                order.Organization = organization;

                toReturn = order;

                return order;
            },
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (toReturn != null)
            _connection.Query<DepreciatedOrderItem, Product, ProductLocation, Storage, ProductPlacement, DepreciatedOrderItem>(
                "SELECT * " +
                "FROM [DepreciatedOrderItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [DepreciatedOrderItem].ProductID " +
                "LEFT JOIN [ProductLocation] " +
                "ON [ProductLocation].DepreciatedOrderItemID = [DepreciatedOrderItem].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductLocation].StorageID " +
                "LEFT JOIN [ProductPlacement] " +
                "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
                "WHERE [DepreciatedOrderItem].Deleted = 0 " +
                "AND [DepreciatedOrderItem].DepreciatedOrderID = @Id",
                (item, product, productLocation, storage, productPlacement) => {
                    if (!toReturn.DepreciatedOrderItems.Any(i => i.Id.Equals(item.Id))) {
                        if (productLocation != null) {
                            productLocation.Storage = storage;
                            productLocation.ProductPlacement = productPlacement;

                            item.ProductLocations.Add(productLocation);
                        }

                        item.Product = product;

                        toReturn.DepreciatedOrderItems.Add(item);
                    } else if (productLocation != null) {
                        productLocation.Storage = storage;
                        productLocation.ProductPlacement = productPlacement;

                        toReturn.DepreciatedOrderItems.First(i => i.Id.Equals(item.Id)).ProductLocations.Add(productLocation);
                    }

                    return item;
                },
                new { toReturn.Id }
            );

        return toReturn;
    }

    public DepreciatedOrder GetByIdForConsignment(long id) {
        DepreciatedOrder toReturn = null;

        Type[] types = {
            typeof(DepreciatedOrder),
            typeof(Storage),
            typeof(Organization),
            typeof(DepreciatedOrderItem)
        };

        Func<object[], DepreciatedOrder> mapper = objects => {
            DepreciatedOrder depreciatedOrder = (DepreciatedOrder)objects[0];
            Storage storage = (Storage)objects[1];
            Organization organization = (Organization)objects[2];
            DepreciatedOrderItem depreciatedOrderItem = (DepreciatedOrderItem)objects[3];

            if (toReturn == null) {
                depreciatedOrder.Storage = storage;
                depreciatedOrder.Organization = organization;

                toReturn = depreciatedOrder;
            }

            if (depreciatedOrderItem == null) return depreciatedOrder;

            toReturn.DepreciatedOrderItems.Add(depreciatedOrderItem);
            return depreciatedOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [DepreciatedOrder] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [DepreciatedOrder].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [DepreciatedOrder].OrganizationID " +
            "LEFT JOIN [DepreciatedOrderItem] " +
            "ON [DepreciatedOrderItem].DepreciatedOrderID = [DepreciatedOrder].ID " +
            "AND [DepreciatedOrderItem].Deleted = 0 " +
            "WHERE [DepreciatedOrder].ID = @Id",
            types,
            mapper,
            new { Id = id }
        );

        return toReturn;
    }

    public DepreciatedOrder GetByNetId(Guid netId) {
        DepreciatedOrder toReturn = null;

        _connection.Query<DepreciatedOrder, Storage, User, Organization, DepreciatedOrder>(
            "SELECT * " +
            "FROM [DepreciatedOrder] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [DepreciatedOrder].StorageID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [DepreciatedOrder].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [DepreciatedOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [DepreciatedOrder].NetUID = @NetId",
            (order, storage, responsible, organization) => {
                order.Storage = storage;
                order.Responsible = responsible;
                order.Organization = organization;

                toReturn = order;

                return order;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (toReturn != null)
            _connection.Query<DepreciatedOrderItem, Product, ProductLocation, Storage, ProductPlacement, DepreciatedOrderItem>(
                "SELECT * " +
                "FROM [DepreciatedOrderItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [DepreciatedOrderItem].ProductID " +
                "LEFT JOIN [ProductLocation] " +
                "ON [ProductLocation].DepreciatedOrderItemID = [DepreciatedOrderItem].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductLocation].StorageID " +
                "LEFT JOIN [ProductPlacement] " +
                "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
                "WHERE [DepreciatedOrderItem].Deleted = 0 " +
                "AND [DepreciatedOrderItem].DepreciatedOrderID = @Id",
                (item, product, productLocation, storage, productPlacement) => {
                    if (!toReturn.DepreciatedOrderItems.Any(i => i.Id.Equals(item.Id))) {
                        if (productLocation != null) {
                            productLocation.Storage = storage;
                            productLocation.ProductPlacement = productPlacement;

                            item.ProductLocations.Add(productLocation);
                        }

                        item.Product = product;

                        toReturn.DepreciatedOrderItems.Add(item);
                    } else if (productLocation != null) {
                        productLocation.Storage = storage;
                        productLocation.ProductPlacement = productPlacement;

                        toReturn.DepreciatedOrderItems.First(i => i.Id.Equals(item.Id)).ProductLocations.Add(productLocation);
                    }

                    return item;
                },
                new { toReturn.Id }
            );

        return toReturn;
    }

    public List<DepreciatedOrder> GetAll() {
        List<DepreciatedOrder> orders =
            _connection.Query<DepreciatedOrder, Storage, User, Organization, DepreciatedOrder>(
                "SELECT * " +
                "FROM [DepreciatedOrder] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [DepreciatedOrder].StorageID " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [DepreciatedOrder].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [DepreciatedOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [DepreciatedOrder].Deleted = 0 " +
                "ORDER BY [DepreciatedOrder].FromDate DESC",
                (order, storage, responsible, organization) => {
                    order.Storage = storage;
                    order.Responsible = responsible;
                    order.Organization = organization;

                    return order;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            ).ToList();

        if (orders.Any())
            _connection.Query<DepreciatedOrderItem, Product, DepreciatedOrderItem>(
                "SELECT * " +
                "FROM [DepreciatedOrderItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [DepreciatedOrderItem].ProductID " +
                "WHERE [DepreciatedOrderItem].Deleted = 0 " +
                "AND [DepreciatedOrderItem].DepreciatedOrderID IN @Ids",
                (item, product) => {
                    item.Product = product;

                    orders.First(o => o.Id.Equals(item.DepreciatedOrderId)).DepreciatedOrderItems.Add(item);

                    return item;
                },
                new { Ids = orders.Select(o => o.Id) }
            );

        return orders;
    }

    public List<DepreciatedOrder> GetAllFiltered(DateTime from, DateTime to, long limit, long offset) {
        List<DepreciatedOrder> orders =
            _connection.Query<DepreciatedOrder, Storage, User, Organization, DepreciatedOrder>(
                ";WITH [Search_CTE] " +
                "AS (" +
                "SELECT [DepreciatedOrder].ID " +
                ", [DepreciatedOrder].FromDate " +
                "FROM [DepreciatedOrder] " +
                "WHERE [DepreciatedOrder].Deleted = 0 " +
                "AND [DepreciatedOrder].FromDate >= @From " +
                "AND [DepreciatedOrder].FromDate <= @To" +
                "), " +
                "[Rowed_CTE] " +
                "AS (" +
                "SELECT [Search_CTE].ID " +
                ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].FromDate DESC) AS [RowNumber] " +
                "FROM [Search_CTE]" +
                ")" +
                "SELECT * " +
                "FROM [DepreciatedOrder] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [DepreciatedOrder].StorageID " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [DepreciatedOrder].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [DepreciatedOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [DepreciatedOrder].ID IN (" +
                "SELECT [Rowed_CTE].ID " +
                "FROM [Rowed_CTE] " +
                "WHERE [Rowed_CTE].RowNumber > @Offset " +
                "AND [Rowed_CTE].RowNumber <= @Limit + @Offset" +
                ") " +
                "ORDER BY [DepreciatedOrder].FromDate DESC",
                (order, storage, responsible, organization) => {
                    order.Storage = storage;
                    order.Responsible = responsible;
                    order.Organization = organization;

                    return order;
                },
                new {
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    From = from,
                    To = to,
                    Limit = limit,
                    Offset = offset
                }
            ).ToList();

        if (orders.Any())
            _connection.Query<DepreciatedOrderItem, Product, ProductLocation, Storage, ProductPlacement, DepreciatedOrderItem>(
                "SELECT * " +
                "FROM [DepreciatedOrderItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [DepreciatedOrderItem].ProductID " +
                "LEFT JOIN [ProductLocation] " +
                "ON [ProductLocation].DepreciatedOrderItemID = [DepreciatedOrderItem].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductLocation].StorageID " +
                "LEFT JOIN [ProductPlacement] " +
                "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
                "WHERE [DepreciatedOrderItem].Deleted = 0 " +
                "AND [DepreciatedOrderItem].DepreciatedOrderID IN @Ids",
                (item, product, productLocation, storage, productPlacement) => {
                    DepreciatedOrder fromList = orders.First(o => o.Id.Equals(item.DepreciatedOrderId));

                    if (!fromList.DepreciatedOrderItems.Any(i => i.Id.Equals(item.Id))) {
                        if (productLocation != null) {
                            productLocation.Storage = storage;
                            productLocation.ProductPlacement = productPlacement;

                            item.ProductLocations.Add(productLocation);
                        }

                        item.Product = product;

                        fromList.DepreciatedOrderItems.Add(item);
                    } else if (productLocation != null) {
                        productLocation.Storage = storage;
                        productLocation.ProductPlacement = productPlacement;

                        fromList.DepreciatedOrderItems.First(i => i.Id.Equals(item.Id)).ProductLocations.Add(productLocation);
                    }

                    return item;
                },
                new { Ids = orders.Select(o => o.Id) }
            );

        return orders;
    }

    public List<DepreciatedOrder> GetAllFiltered(DateTime from, DateTime to) {
        List<DepreciatedOrder> orders =
            _connection.Query<DepreciatedOrder, Storage, User, Organization, Currency, DepreciatedOrder>(
                ";WITH [Search_CTE] " +
                "AS (" +
                "SELECT [DepreciatedOrder].ID " +
                ", [DepreciatedOrder].FromDate " +
                "FROM [DepreciatedOrder] " +
                "WHERE [DepreciatedOrder].Deleted = 0 " +
                "AND [DepreciatedOrder].FromDate >= @From " +
                "AND [DepreciatedOrder].FromDate <= @To" +
                "), " +
                "[Rowed_CTE] " +
                "AS (" +
                "SELECT [Search_CTE].ID " +
                ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].FromDate DESC) AS [RowNumber] " +
                "FROM [Search_CTE]" +
                ")" +
                "SELECT * " +
                "FROM [DepreciatedOrder] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [DepreciatedOrder].StorageID " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [DepreciatedOrder].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [DepreciatedOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [Organization].CurrencyID " +
                "WHERE [DepreciatedOrder].ID IN (" +
                "SELECT [Rowed_CTE].ID " +
                "FROM [Rowed_CTE] " +
                // "WHERE [Rowed_CTE].RowNumber > @Offset " +
                // "AND [Rowed_CTE].RowNumber <= @Limit + @Offset" +
                ") " +
                "ORDER BY [DepreciatedOrder].FromDate DESC",
                (order, storage, responsible, organization, organizationCurrency) => {
                    order.Storage = storage;
                    order.Responsible = responsible;
                    organization.Currency = organizationCurrency;
                    order.Organization = organization;

                    return order;
                },
                new {
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    From = from,
                    To = to
                }
            ).ToList();

        if (orders.Any()) {
            Type[] depreciatedItemTypes = {
                typeof(DepreciatedOrderItem),
                typeof(Product),
                typeof(ProductProductGroup),
                typeof(ProductGroup),
                typeof(MeasureUnit),
                typeof(ProductLocation),
                typeof(Storage),
                typeof(ProductPlacement),
                typeof(ConsignmentItemMovement),
                typeof(ConsignmentItem)
            };

            Func<object[], DepreciatedOrderItem> depreciatedItemMapper = objects => {
                DepreciatedOrderItem item = (DepreciatedOrderItem)objects[0];
                Product product = (Product)objects[1];
                ProductProductGroup productProductGroup = (ProductProductGroup)objects[2];
                ProductGroup productGroup = (ProductGroup)objects[3];
                MeasureUnit measureUnit = (MeasureUnit)objects[4];
                ProductLocation productLocation = (ProductLocation)objects[5];
                Storage storage = (Storage)objects[6];
                ProductPlacement productPlacement = (ProductPlacement)objects[7];
                ConsignmentItemMovement consignmentItemMovement = (ConsignmentItemMovement)objects[8];
                ConsignmentItem consignmentItem = (ConsignmentItem)objects[9];

                DepreciatedOrder fromList = orders.First(o => o.Id.Equals(item.DepreciatedOrderId));

                if (!fromList.DepreciatedOrderItems.Any(i => i.Id.Equals(item.Id))) {
                    if (productLocation != null) {
                        productLocation.Storage = storage;
                        productLocation.ProductPlacement = productPlacement;

                        item.ProductLocations.Add(productLocation);
                    }

                    if (productProductGroup != null && productGroup != null) {
                        productProductGroup.ProductGroup = productGroup;
                        product.ProductProductGroups.Add(productProductGroup);
                    }

                    item.PerUnitPrice = fromList.Organization.IsVatAgreements ? consignmentItem.AccountingPrice : consignmentItem.Price;
                    // item.PerUnitPrice = decimal.Round(item.PerUnitPrice * consignmentItem.ExchangeRate, 2, MidpointRounding.AwayFromZero);

                    product.MeasureUnit = measureUnit;
                    item.Product = product;

                    fromList.DepreciatedOrderItems.Add(item);
                } else if (productLocation != null) {
                    productLocation.Storage = storage;
                    productLocation.ProductPlacement = productPlacement;

                    fromList.DepreciatedOrderItems.First(i => i.Id.Equals(item.Id)).ProductLocations.Add(productLocation);
                }

                return item;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [DepreciatedOrderItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [DepreciatedOrderItem].ProductID " +
                "LEFT JOIN ProductProductGroup " +
                "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
                "LEFT JOIN ProductGroup " +
                "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [ProductLocation] " +
                "ON [ProductLocation].DepreciatedOrderItemID = [DepreciatedOrderItem].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductLocation].StorageID " +
                "LEFT JOIN [ProductPlacement] " +
                "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
                "LEFT JOIN ConsignmentItemMovement " +
                "ON ConsignmentItemMovement.DepreciatedOrderItemID = DepreciatedOrderItem.ID " +
                "LEFT JOIN ConsignmentItem " +
                "ON ConsignmentItem.ID = ConsignmentItemMovement.ConsignmentItemID " +
                "WHERE [DepreciatedOrderItem].Deleted = 0 " +
                "AND [DepreciatedOrderItem].DepreciatedOrderID IN @Ids",
                depreciatedItemTypes,
                depreciatedItemMapper,
                new { Ids = orders.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        }

        return orders;
    }

    public DepreciatedOrder GetByNetIdForExportDocument(Guid netId) {
        DepreciatedOrder toReturn = null;

        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

        string sqlQuery =
            "SELECT * " +
            "FROM [DepreciatedOrder] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [DepreciatedOrder].[StorageID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [DepreciatedOrder].[OrganizationID] " +
            "LEFT JOIN [DepreciatedOrderItem] " +
            "ON [DepreciatedOrderItem].[DepreciatedOrderID] = [DepreciatedOrder].[ID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [DepreciatedOrderItem].[ProductID] " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ConsignmentItemMovement] " +
            "ON [ConsignmentItemMovement].DepreciatedOrderItemID = [DepreciatedOrderItem].ID " +
            "AND [ConsignmentItemMovement].Deleted = 0 " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "WHERE [DepreciatedOrder].[NetUID] = @Id";

        Type[] types = {
            typeof(DepreciatedOrder),
            typeof(Storage),
            typeof(Organization),
            typeof(DepreciatedOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ConsignmentItemMovement),
            typeof(ConsignmentItem)
        };

        Func<object[], DepreciatedOrder> mapper = objects => {
            DepreciatedOrder depreciatedOrder = (DepreciatedOrder)objects[0];
            Storage storage = (Storage)objects[1];
            Organization organization = (Organization)objects[2];
            DepreciatedOrderItem depreciatedOrderItem = (DepreciatedOrderItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ConsignmentItemMovement consignmentItemMovement = (ConsignmentItemMovement)objects[6];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[7];

            if (toReturn == null) {
                toReturn = depreciatedOrder;
                toReturn.Organization = organization;
                toReturn.Storage = storage;
            }

            if (depreciatedOrderItem == null) return depreciatedOrder;

            if (toReturn.DepreciatedOrderItems.Any(x => x.Id.Equals(depreciatedOrderItem.Id))) {
                depreciatedOrderItem = toReturn.DepreciatedOrderItems.First(x => x.Id.Equals(depreciatedOrderItem.Id));
            } else {
                measureUnit.Name = culture == "pl" ? measureUnit.NamePl : measureUnit.NameUk;

                product.Name = culture == "pl" ? product.NameUA : product.NameUA;
                product.MeasureUnit = measureUnit;

                depreciatedOrderItem.Product = product;

                toReturn.DepreciatedOrderItems.Add(depreciatedOrderItem);
            }

            if (consignmentItemMovement == null) return depreciatedOrder;

            consignmentItemMovement.ConsignmentItem = consignmentItem;

            depreciatedOrderItem.ConsignmentItemMovements.Add(consignmentItemMovement);

            return depreciatedOrder;
        };

        _connection.Query(
            sqlQuery,
            types,
            mapper,
            new { Id = netId, Culture = culture }
        );

        return toReturn;
    }
}