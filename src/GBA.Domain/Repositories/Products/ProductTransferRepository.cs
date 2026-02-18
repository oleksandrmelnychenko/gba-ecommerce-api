using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductTransferRepository : IProductTransferRepository {
    private readonly IDbConnection _connection;

    public ProductTransferRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductTransfer productTransfer) {
        return _connection.Query<long>(
                "INSERT INTO [ProductTransfer] (Number, Comment, FromDate, ResponsibleId, FromStorageId, ToStorageId, OrganizationId, Updated, " +
                "[IsManagement]) " +
                "VALUES (@Number, @Comment, @FromDate, @ResponsibleId, @FromStorageId, @ToStorageId, @OrganizationId, GETUTCDATE(), " +
                "@IsManagement); " +
                "SELECT SCOPE_IDENTITY()",
                productTransfer
            )
            .SingleOrDefault();
    }

    public void Update(ProductTransfer productTransfer) {
        _connection.Execute(
            "UPDATE [ProductTransfer] " +
            "SET Number = @Number, Comment = @Comment, FromDate = @FromDate, ResponsibleId = @ResponsibleId, FromStorageId = @FromStorageId, " +
            "ToStorageId = @ToStorageId, OrganizationId = @OrganizationId, Updated = GETUTCDATE(), " +
            "[IsManagement] = @IsManagement" +
            "WHERE [ProductTransfer].ID = @Id",
            productTransfer
        );
    }

    public ProductTransfer GetLastRecord(long organizationId) {
        return _connection.Query<ProductTransfer>(
                "SELECT TOP(1) * " +
                "FROM [ProductTransfer] " +
                "WHERE [ProductTransfer].Deleted = 0 " +
                "AND [ProductTransfer].OrganizationID = @OrganizationId " +
                "ORDER BY ID DESC",
                new { OrganizationId = organizationId }
            )
            .SingleOrDefault();
    }

    public ProductTransfer GetLastRecord(string culture) {
        string sqlExpression =
            "SELECT TOP(1) * " +
            "FROM [ProductTransfer] " +
            "WHERE [ProductTransfer].Deleted = 0 ";

        sqlExpression +=
            culture.ToLower().Equals("pl")
                ? "AND [ProductTransfer].Number like 'P%' "
                : "AND [ProductTransfer].Number NOT like 'P%'";

        sqlExpression += "ORDER BY [ProductTransfer].ID DESC";

        return _connection.Query<ProductTransfer>(
                sqlExpression
            )
            .SingleOrDefault();
    }

    public ProductTransfer GetById(long id) {
        ProductTransfer toReturn = null;

        _connection.Query<ProductTransfer, User, Storage, Storage, Organization, ProductTransfer>(
            "SELECT * " +
            "FROM [ProductTransfer] " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductTransfer].ResponsibleID " +
            "LEFT JOIN [Storage] AS [FromStorage] " +
            "ON [FromStorage].ID = [ProductTransfer].FromStorageID " +
            "LEFT JOIN [Storage] AS [ToStorage] " +
            "ON [ToStorage].ID = [ProductTransfer].ToStorageID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductTransfer].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [ProductTransfer].ID = @Id",
            (transfer, responsible, fromStorage, toStorage, organization) => {
                transfer.Responsible = responsible;
                transfer.FromStorage = fromStorage;
                transfer.ToStorage = toStorage;
                transfer.Organization = organization;

                toReturn = transfer;

                return transfer;
            },
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (toReturn != null)
            _connection.Query<ProductTransferItem, Product, MeasureUnit, ProductLocation, Storage, ProductPlacement, ProductTransferItem>(
                "SELECT * " +
                "FROM [ProductTransferItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [ProductTransferItem].ProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [ProductLocation] " +
                "ON [ProductLocation].ProductTransferItemID = [ProductTransferItem].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductLocation].StorageID " +
                "LEFT JOIN [ProductPlacement] " +
                "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
                "WHERE [ProductTransferItem].Deleted = 0 " +
                "AND [ProductTransferItem].ProductTransferID = @Id",
                (item, product, measureUnit, productLocation, storage, productPlacement) => {
                    if (!toReturn.ProductTransferItems.Any(i => i.Id.Equals(item.Id))) {
                        if (productLocation != null) {
                            productLocation.Storage = storage;
                            productLocation.ProductPlacement = productPlacement;

                            item.ProductLocations.Add(productLocation);
                        }

                        product.MeasureUnit = measureUnit;

                        item.Product = product;

                        toReturn.ProductTransferItems.Add(item);
                    } else if (productLocation != null) {
                        productLocation.Storage = storage;
                        productLocation.ProductPlacement = productPlacement;

                        toReturn.ProductTransferItems.First(i => i.Id.Equals(item.Id)).ProductLocations.Add(productLocation);
                    }

                    return item;
                },
                new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

        return toReturn;
    }

    public ProductTransfer GetByIdForConsignmentCreation(long id) {
        ProductTransfer toReturn = null;

        Type[] types = {
            typeof(ProductTransfer),
            typeof(Storage),
            typeof(Storage),
            typeof(Organization),
            typeof(ProductTransferItem),
            typeof(Organization),
            typeof(Organization)
        };

        Func<object[], ProductTransfer> mapper = objects => {
            ProductTransfer transfer = (ProductTransfer)objects[0];
            Storage fromStorage = (Storage)objects[1];
            Storage toStorage = (Storage)objects[2];
            Organization organization = (Organization)objects[3];
            ProductTransferItem transferItem = (ProductTransferItem)objects[4];
            Organization fromStorageOrganization = (Organization)objects[5];
            Organization toStorageOrganization = (Organization)objects[6];

            if (toReturn == null) {
                transfer.Organization = organization;
                fromStorage.Organization = fromStorageOrganization;
                transfer.FromStorage = fromStorage;
                toStorage.Organization = toStorageOrganization;
                transfer.ToStorage = toStorage;

                toReturn = transfer;
            }

            if (transferItem == null) return transfer;

            toReturn.ProductTransferItems.Add(transferItem);

            return transfer;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ProductTransfer] " +
            "LEFT JOIN [Storage] AS [FromStorage] " +
            "ON [FromStorage].ID = [ProductTransfer].FromStorageID " +
            "LEFT JOIN [Storage] AS [ToStorage] " +
            "ON [ToStorage].ID = [ProductTransfer].ToStorageID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductTransfer].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ProductTransferItem] " +
            "ON [ProductTransferItem].ProductTransferID = [ProductTransfer].ID " +
            "AND [ProductTransferItem].Deleted = 0 " +
            "LEFT JOIN [Organization] AS [FromStorageOrganization] " +
            "ON [FromStorageOrganization].[ID] = [FromStorage].[OrganizationID] " +
            "LEFT JOIN [Organization] AS [ToStorageOrganization] " +
            "ON [ToStorageOrganization].[ID] = [ToStorage].[OrganizationID] " +
            "WHERE [ProductTransfer].ID = @Id",
            types,
            mapper,
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public ProductTransfer GetByNetId(Guid netId) {
        ProductTransfer toReturn = null;

        _connection.Query<ProductTransfer, User, Storage, Storage, Organization, ProductTransfer>(
            "SELECT * " +
            "FROM [ProductTransfer] " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductTransfer].ResponsibleID " +
            "LEFT JOIN [Storage] AS [FromStorage] " +
            "ON [FromStorage].ID = [ProductTransfer].FromStorageID " +
            "LEFT JOIN [Storage] AS [ToStorage] " +
            "ON [ToStorage].ID = [ProductTransfer].ToStorageID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductTransfer].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [ProductTransfer].NetUID = @NetId",
            (transfer, responsible, fromStorage, toStorage, organization) => {
                transfer.Responsible = responsible;
                transfer.FromStorage = fromStorage;
                transfer.ToStorage = toStorage;
                transfer.Organization = organization;

                toReturn = transfer;

                return transfer;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (toReturn != null)
            _connection.Query<ProductTransferItem, Product, MeasureUnit, ProductLocation, Storage, ProductPlacement, ProductTransferItem>(
                "SELECT * " +
                "FROM [ProductTransferItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [ProductTransferItem].ProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [ProductLocation] " +
                "ON [ProductLocation].ProductTransferItemID = [ProductTransferItem].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductLocation].StorageID " +
                "LEFT JOIN [ProductPlacement] " +
                "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
                "WHERE [ProductTransferItem].Deleted = 0 " +
                "AND [ProductTransferItem].ProductTransferID = @Id",
                (item, product, measureUnit, productLocation, storage, productPlacement) => {
                    if (!toReturn.ProductTransferItems.Any(i => i.Id.Equals(item.Id))) {
                        if (productLocation != null) {
                            productLocation.Storage = storage;
                            productLocation.ProductPlacement = productPlacement;

                            item.ProductLocations.Add(productLocation);
                        }

                        product.MeasureUnit = measureUnit;

                        item.Product = product;

                        toReturn.ProductTransferItems.Add(item);
                    } else if (productLocation != null) {
                        productLocation.Storage = storage;
                        productLocation.ProductPlacement = productPlacement;

                        toReturn.ProductTransferItems.First(i => i.Id.Equals(item.Id)).ProductLocations.Add(productLocation);
                    }

                    return item;
                },
                new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

        return toReturn;
    }

    public List<ProductTransfer> GetAll() {
        List<ProductTransfer> transfers =
            _connection.Query<ProductTransfer, User, Storage, Storage, Organization, ProductTransfer>(
                "SELECT * " +
                "FROM [ProductTransfer] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [ProductTransfer].ResponsibleID " +
                "LEFT JOIN [Storage] AS [FromStorage] " +
                "ON [FromStorage].ID = [ProductTransfer].FromStorageID " +
                "LEFT JOIN [Storage] AS [ToStorage] " +
                "ON [ToStorage].ID = [ProductTransfer].ToStorageID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [ProductTransfer].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [ProductTransfer].Deleted = 0",
                (transfer, responsible, fromStorage, toStorage, organization) => {
                    transfer.Responsible = responsible;
                    transfer.FromStorage = fromStorage;
                    transfer.ToStorage = toStorage;
                    transfer.Organization = organization;

                    return transfer;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            ).ToList();

        if (transfers.Any())
            _connection.Query<ProductTransferItem, Product, MeasureUnit, ProductTransferItem>(
                "SELECT * " +
                "FROM [ProductTransferItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [ProductTransferItem].ProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "WHERE [ProductTransferItem].Deleted = 0 " +
                "AND [ProductTransferItem].ProductTransferID IN @Ids",
                (item, product, measureUnit) => {
                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    transfers.First(t => t.Id.Equals(item.ProductTransferId)).ProductTransferItems.Add(item);

                    return item;
                },
                new { Ids = transfers.Select(t => t.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

        return transfers;
    }

    public List<ProductTransfer> GetAllFiltered(DateTime from, DateTime to, long limit, long offset) {
        List<ProductTransfer> transfers =
            _connection.Query<ProductTransfer, User, Storage, Storage, Organization, ProductTransfer>(
                ";WITH [Search_CTE] " +
                "AS (" +
                "SELECT [ProductTransfer].ID " +
                ", [ProductTransfer].FromDate " +
                "FROM [ProductTransfer] " +
                "WHERE [ProductTransfer].Deleted = 0 " +
                "AND [ProductTransfer].FromDate >= @From " +
                "AND [ProductTransfer].FromDate <= @To" +
                "), " +
                "[Rowed_CTE] " +
                "AS (" +
                "SELECT [Search_CTE].ID " +
                ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].FromDate DESC) AS [RowNumber] " +
                "FROM [Search_CTE]" +
                ")" +
                "SELECT * " +
                "FROM [ProductTransfer] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [ProductTransfer].ResponsibleID " +
                "LEFT JOIN [Storage] AS [FromStorage] " +
                "ON [FromStorage].ID = [ProductTransfer].FromStorageID " +
                "LEFT JOIN [Storage] AS [ToStorage] " +
                "ON [ToStorage].ID = [ProductTransfer].ToStorageID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [ProductTransfer].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "WHERE [ProductTransfer].ID IN (" +
                "SELECT [Rowed_CTE].ID " +
                "FROM [Rowed_CTE] " +
                "WHERE [Rowed_CTE].RowNumber > @Offset " +
                "AND [Rowed_CTE].RowNumber <= @Limit + @Offset" +
                ") " +
                "ORDER BY [ProductTransfer].FromDate DESC",
                (transfer, responsible, fromStorage, toStorage, organization) => {
                    transfer.Responsible = responsible;
                    transfer.FromStorage = fromStorage;
                    transfer.ToStorage = toStorage;
                    transfer.Organization = organization;
                    return transfer;
                },
                new {
                    From = from,
                    To = to,
                    Limit = limit,
                    Offset = offset,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            ).ToList();

        if (transfers.Any())
            _connection.Query<ProductTransferItem, Product, MeasureUnit, ProductLocation, Storage, ProductPlacement, ProductTransferItem>(
                "SELECT * " +
                "FROM [ProductTransferItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [ProductTransferItem].ProductID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [ProductLocation] " +
                "ON [ProductLocation].ProductTransferItemID = [ProductTransferItem].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductLocation].StorageID " +
                "LEFT JOIN [ProductPlacement] " +
                "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
                "WHERE [ProductTransferItem].Deleted = 0 " +
                "AND [ProductTransferItem].ProductTransferID IN @Ids",
                (item, product, measureUnit, productLocation, storage, productPlacement) => {
                    ProductTransfer fromList = transfers.First(t => t.Id.Equals(item.ProductTransferId));

                    if (!fromList.ProductTransferItems.Any(i => i.Id.Equals(item.Id))) {
                        if (productLocation != null) {
                            productLocation.Storage = storage;
                            productLocation.ProductPlacement = productPlacement;

                            item.ProductLocations.Add(productLocation);
                        }

                        product.MeasureUnit = measureUnit;

                        item.Product = product;

                        fromList.ProductTransferItems.Add(item);
                    } else if (productLocation != null) {
                        productLocation.Storage = storage;
                        productLocation.ProductPlacement = productPlacement;

                        fromList.ProductTransferItems.First(i => i.Id.Equals(item.Id)).ProductLocations.Add(productLocation);
                    }

                    return item;
                },
                new { Ids = transfers.Select(t => t.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );

        return transfers;
    }

    public List<ProductTransfer> GetAllFiltered(DateTime from, DateTime to) {
        List<ProductTransfer> transfers =
            _connection.Query<ProductTransfer, User, Storage, Storage, Organization, Currency, ProductTransfer>(
                ";WITH [Search_CTE] " +
                "AS (" +
                "SELECT [ProductTransfer].ID " +
                ", [ProductTransfer].FromDate " +
                "FROM [ProductTransfer] " +
                "WHERE [ProductTransfer].Deleted = 0 " +
                "AND [ProductTransfer].FromDate >= @From " +
                "AND [ProductTransfer].FromDate <= @To" +
                "), " +
                "[Rowed_CTE] " +
                "AS (" +
                "SELECT [Search_CTE].ID " +
                ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].FromDate DESC) AS [RowNumber] " +
                "FROM [Search_CTE]" +
                ")" +
                "SELECT * " +
                "FROM [ProductTransfer] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [ProductTransfer].ResponsibleID " +
                "LEFT JOIN [Storage] AS [FromStorage] " +
                "ON [FromStorage].ID = [ProductTransfer].FromStorageID " +
                "LEFT JOIN [Storage] AS [ToStorage] " +
                "ON [ToStorage].ID = [ProductTransfer].ToStorageID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [ProductTransfer].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [Organization].CurrencyID " +
                "WHERE [ProductTransfer].ID IN (" +
                "SELECT [Rowed_CTE].ID " +
                "FROM [Rowed_CTE] " +
                // "WHERE [Rowed_CTE].RowNumber > @Offset " +
                // "AND [Rowed_CTE].RowNumber <= @Limit + @Offset" +
                ") " +
                "ORDER BY [ProductTransfer].FromDate DESC",
                (transfer, responsible, fromStorage, toStorage, organization, organizationCurrency) => {
                    transfer.Responsible = responsible;
                    transfer.FromStorage = fromStorage;
                    transfer.ToStorage = toStorage;
                    organization.Currency = organizationCurrency;
                    transfer.Organization = organization;
                    return transfer;
                },
                new {
                    From = from,
                    To = to,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            ).ToList();

        if (transfers.Any()) {
            Type[] transferItemTypes = {
                typeof(ProductTransferItem),
                typeof(Product),
                typeof(ProductProductGroup),
                typeof(ProductGroup),
                typeof(MeasureUnit),
                typeof(ProductLocation),
                typeof(Storage),
                typeof(ProductPlacement)
            };

            Func<object[], ProductTransferItem> transferItemMappers = objects => {
                ProductTransferItem item = (ProductTransferItem)objects[0];
                Product product = (Product)objects[1];
                ProductProductGroup productProductGroup = (ProductProductGroup)objects[2];
                ProductGroup productGroup = (ProductGroup)objects[3];
                MeasureUnit measureUnit = (MeasureUnit)objects[4];
                ProductLocation productLocation = (ProductLocation)objects[5];
                Storage storage = (Storage)objects[6];
                ProductPlacement productPlacement = (ProductPlacement)objects[7];


                ProductTransfer fromList = transfers.First(t => t.Id.Equals(item.ProductTransferId));

                if (!fromList.ProductTransferItems.Any(i => i.Id.Equals(item.Id))) {
                    if (productLocation != null) {
                        productLocation.Storage = storage;
                        productLocation.ProductPlacement = productPlacement;

                        item.ProductLocations.Add(productLocation);
                    }

                    if (productProductGroup != null && productGroup != null) {
                        productProductGroup.ProductGroup = productGroup;
                        product.ProductProductGroups.Add(productProductGroup);
                    }

                    product.MeasureUnit = measureUnit;

                    item.Product = product;

                    fromList.ProductTransferItems.Add(item);
                } else if (productLocation != null) {
                    productLocation.Storage = storage;
                    productLocation.ProductPlacement = productPlacement;

                    fromList.ProductTransferItems.First(i => i.Id.Equals(item.Id)).ProductLocations.Add(productLocation);
                }

                return item;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ProductTransferItem] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [ProductTransferItem].ProductID " +
                "LEFT JOIN ProductProductGroup " +
                "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
                "LEFT JOIN ProductGroup " +
                "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [ProductLocation] " +
                "ON [ProductLocation].ProductTransferItemID = [ProductTransferItem].ID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductLocation].StorageID " +
                "LEFT JOIN [ProductPlacement] " +
                "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
                "WHERE [ProductTransferItem].Deleted = 0 " +
                "AND [ProductTransferItem].ProductTransferID IN @Ids",
                transferItemTypes,
                transferItemMappers,
                new { Ids = transfers.Select(t => t.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            );
        }

        return transfers;
    }
}