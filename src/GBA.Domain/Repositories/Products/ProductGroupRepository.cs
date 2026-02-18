using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers.ProductGroupModels;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductGroupRepository : IProductGroupRepository {
    private readonly IDbConnection _connection;

    public ProductGroupRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductGroup productGroup) {
        return _connection.Query<long>(
                "INSERT INTO ProductGroup (Name, FullName, Description, IsSubGroup, Updated, [IsActive]) " +
                "VALUES(@Name, @FullName, @Description, @IsSubGroup,getutcdate(), @IsActive); " +
                "SELECT SCOPE_IDENTITY()",
                productGroup
            )
            .Single();
    }

    public List<ProductGroup> GetAll() {
        List<ProductGroup> productGroups = new();

        string sqlExpression = "SELECT * " +
                               "FROM [ProductGroup] " +
                               "LEFT JOIN [ProductSubGroup] " +
                               "ON [ProductSubGroup].RootProductGroupID = [ProductGroup].ID " +
                               "AND [ProductSubGroup].Deleted = 0 " +
                               "LEFT JOIN [ProductGroup] AS [SubProductGroup] " +
                               "ON [ProductSubGroup].SubProductGroupID = [SubProductGroup].ID " +
                               "WHERE [ProductGroup].IsSubGroup = 0 " +
                               "AND [ProductGroup].Deleted = 0 " +
                               "ORDER BY [ProductGroup].FullName, SubProductGroup.FullName";

        Type[] types = {
            typeof(ProductGroup),
            typeof(ProductSubGroup),
            typeof(ProductGroup)
        };

        Func<object[], ProductGroup> mapper = objects => {
            ProductGroup productGroup = (ProductGroup)objects[0];
            ProductSubGroup productSubGroup = (ProductSubGroup)objects[1];
            ProductGroup subProductGroup = (ProductGroup)objects[2];

            if (productGroups.Any(p => p.NetUid.Equals(productGroup.NetUid))) {
                if (productSubGroup != null) {
                    if (subProductGroup != null) productSubGroup.SubProductGroup = subProductGroup;

                    productGroups.FirstOrDefault(p => p.NetUid.Equals(productGroup.NetUid)).SubProductGroups.Add(productSubGroup);
                }
            } else {
                if (productSubGroup != null) {
                    if (subProductGroup != null) productSubGroup.SubProductGroup = subProductGroup;

                    productGroup.SubProductGroups.Add(productSubGroup);
                }

                productGroups.Add(productGroup);
            }

            return productGroup;
        };

        _connection.Query(sqlExpression, types, mapper);

        return productGroups;
    }

    public List<ProductGroup> GetAllByProductNetId(Guid productNetId) {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, ProductGroup> groupDict = new();

        _connection.Query<ProductGroup, ProductSubGroup, ProductGroup, ProductGroup>(
            "SELECT [ProductGroup].* " +
            ", [ProductSubGroup].*" +
            ", [RootProductGroup].* " +
            "FROM [ProductGroup] " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductGroupID = [ProductGroup].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductProductGroup].ProductID " +
            "LEFT JOIN [ProductSubGroup] " +
            "ON [ProductSubGroup].SubProductGroupID = [ProductGroup].ID " +
            "AND [ProductSubGroup].Deleted = 0 " +
            "LEFT JOIN [ProductGroup] AS [RootProductGroup] " +
            "ON [RootProductGroup].ID = [ProductSubGroup].RootProductGroupID " +
            "WHERE [Product].NetUID = @ProductNetId " +
            "ORDER BY [ProductGroup].[Name]",
            (productGroup, subGroup, rootProductGroup) => {
                // O(1) lookup with TryGetValue instead of O(n) Any + First
                if (groupDict.TryGetValue(productGroup.Id, out ProductGroup existingGroup)) {
                    if (subGroup != null) {
                        subGroup.RootProductGroup = rootProductGroup;

                        existingGroup.RootProductGroups.Add(subGroup);
                    }
                } else {
                    if (subGroup != null) {
                        subGroup.RootProductGroup = rootProductGroup;

                        productGroup.RootProductGroups.Add(subGroup);
                    }

                    groupDict[productGroup.Id] = productGroup;
                }

                return productGroup;
            },
            new { ProductNetId = productNetId }
        );

        return groupDict.Values.ToList();
    }

    public ProductGroup GetById(long id) {
        ProductGroup productGroupToReturn = null;

        string sqlExpression = "SELECT * FROM ProductGroup " +
                               "LEFT OUTER JOIN ProductSubGroup " +
                               "ON ProductSubGroup.RootProductGroupID = ProductGroup.ID " +
                               "LEFT OUTER JOIN ProductGroup subProductGroup " +
                               "ON ProductSubGroup.SubProductGroupID = subProductGroup.ID " +
                               "WHERE ProductGroup.ID = @Id AND ProductGroup.Deleted = 0";

        Type[] types = {
            typeof(ProductGroup),
            typeof(ProductSubGroup),
            typeof(ProductGroup)
        };

        Func<object[], ProductGroup> mapper = objects => {
            ProductGroup productGroup = (ProductGroup)objects[0];
            ProductSubGroup productSubGroup = (ProductSubGroup)objects[1];
            ProductGroup subProductGroup = (ProductGroup)objects[2];

            if (productGroupToReturn == null) {
                if (productSubGroup != null) {
                    if (subProductGroup != null) productSubGroup.SubProductGroup = subProductGroup;

                    productGroup.SubProductGroups.Add(productSubGroup);
                }

                productGroupToReturn = productGroup;
            } else {
                if (productSubGroup != null) {
                    if (subProductGroup != null) productSubGroup.SubProductGroup = subProductGroup;

                    productGroupToReturn.SubProductGroups.Add(productSubGroup);
                }
            }

            return productGroup;
        };

        var props = new { Id = id };

        _connection.Query(sqlExpression, types, mapper, props);


        return productGroupToReturn;
    }

    public ProductGroup GetByNetId(Guid netId) {
        ProductGroup productGroupToReturn = null;

        string sqlExpression = "SELECT * FROM ProductGroup " +
                               "LEFT OUTER JOIN ProductSubGroup " +
                               "ON ProductSubGroup.RootProductGroupID = ProductGroup.ID " +
                               "LEFT OUTER JOIN ProductGroup subProductGroup " +
                               "ON ProductSubGroup.SubProductGroupID = subProductGroup.ID " +
                               "WHERE ProductGroup.NetUID = @NetId AND ProductGroup.Deleted = 0";

        Type[] types = {
            typeof(ProductGroup),
            typeof(ProductSubGroup),
            typeof(ProductGroup)
        };

        Func<object[], ProductGroup> mapper = objects => {
            ProductGroup productGroup = (ProductGroup)objects[0];
            ProductSubGroup productSubGroup = (ProductSubGroup)objects[1];
            ProductGroup subProductGroup = (ProductGroup)objects[2];

            if (productGroupToReturn == null) {
                if (productSubGroup != null) {
                    if (subProductGroup != null) productSubGroup.SubProductGroup = subProductGroup;

                    productGroup.SubProductGroups.Add(productSubGroup);
                }

                productGroupToReturn = productGroup;
            } else {
                if (productSubGroup != null) {
                    if (subProductGroup != null) productSubGroup.SubProductGroup = subProductGroup;

                    productGroupToReturn.SubProductGroups.Add(productSubGroup);
                }
            }

            return productGroup;
        };

        var props = new { NetId = netId };

        _connection.Query(sqlExpression, types, mapper, props);


        return productGroupToReturn;
    }

    public ProductGroup GetByName(string name) {
        return _connection.Query<ProductGroup>(
                "SELECT * " +
                "FROM [ProductGroup] " +
                "WHERE [Name] = @Name",
                new { Name = name }
            )
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ProductGroup SET Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(ProductGroup productGroup) {
        _connection.Execute(
            "UPDATE ProductGroup " +
            "SET Name = @Name" +
            ", FullName = @FullName" +
            ", Description = @Description" +
            ", IsSubGroup = @IsSubGroup" +
            ", Updated = getutcdate() " +
            ", [IsActive] = @IsActive " +
            "WHERE NetUID = @NetUID",
            productGroup
        );
    }

    public void SetIsSubGroup(long id) {
        _connection.Execute(
            "UPDATE [ProductGroup] " +
            "SET [IsSubGroup] = 1" +
            ", [Updated] = getutcdate() " +
            "WHERE [ProductGroup].[ID] = @Id",
            new { Id = id }
        );
    }

    public ProductGroupsWithTotalModel GetAllFiltered(
        string value) {
        ProductGroupsWithTotalModel toReturn = new() {
            ProductGroups = _connection.Query<ProductGroup, ProductGroup, ProductGroup>(
                ";WITH [TOTAL_SEARCH] AS ( " +
                "SELECT " +
                "ROW_NUMBER() OVER(ORDER BY [ProductGroup].ID DESC) AS [RowNumber] " +
                ", [ProductGroup].[ID] " +
                ", ( " +
                "SELECT COUNT(1) " +
                "FROM [ProductSubGroup] AS [CountSubGroup] " +
                "WHERE [CountSubGroup].[RootProductGroupID] = [ProductGroup].[ID] " +
                "AND [CountSubGroup].[Deleted] = 0 " +
                ") AS [TotalProductSubGroup] " +
                ", ( " +
                "SELECT COUNT(1) " +
                "FROM [ProductProductGroup] " +
                "WHERE [ProductProductGroup].[ProductGroupID] = [ProductGroup].[ID] " +
                "AND [ProductProductGroup].[Deleted] = 0 " +
                ") AS [TotalProduct] " +
                "FROM [ProductGroup] " +
                "LEFT JOIN [ProductSubGroup] " +
                "ON [ProductSubGroup].[RootProductGroupID] = [ProductGroup].[ID] " +
                "AND [ProductSubGroup].[Deleted] = 0 " +
                "LEFT JOIN [ProductGroup] AS [ProductSubGroupGroup] " +
                "ON [ProductSubGroupGroup].[ID] = [ProductSubGroup].[SubProductGroupID] " +
                "LEFT JOIN [ProductSubGroup] AS [RootProductSubGroup] " +
                "ON [RootProductSubGroup].[SubProductGroupID] = [ProductGroup].[ID] " +
                "AND [RootProductSubGroup].[Deleted] = 0 " +
                "LEFT JOIN [ProductGroup] AS [RootProductGroup] " +
                "ON [RootProductGroup].[ID] = [RootProductSubGroup].[RootProductGroupID] " +
                "WHERE [ProductGroup].[Deleted] = 0 " +
                "AND ([ProductGroup].[Name] LIKE '%' + @Value + '%' " +
                "OR [ProductGroup].[FullName] LIKE '%' + @Value + '%' " +
                "OR [ProductGroup].[Description] LIKE '%' + @Value + '%' " +
                "OR [ProductSubGroupGroup].[Name] LIKE '%' + @Value + '%' " +
                "OR [ProductSubGroupGroup].[FullName] LIKE '%' + @Value + '%' " +
                "OR [ProductSubGroupGroup].[Description] LIKE '%' + @Value + '%' " +
                "OR [RootProductGroup].[Name] LIKE '%' + @Value + '%' " +
                "OR [RootProductGroup].[FullName] LIKE '%' + @Value + '%' " +
                "OR [RootProductGroup].[Description] LIKE '%' + @Value + '%') " +
                "GROUP BY [ProductGroup].[ID] " +
                ") " +
                "SELECT " +
                "[ProductGroup].* " +
                ",[TOTAL_SEARCH].[TotalProductSubGroup] " +
                ",[TOTAL_SEARCH].[TotalProduct] " +
                ",[RootProductGroup].* " +
                "FROM [ProductGroup] " +
                "LEFT JOIN [ProductSubGroup] " +
                "ON [ProductSubGroup].[SubProductGroupID] = [ProductGroup].[ID] " +
                "AND [ProductSubGroup].[Deleted] = 0 " +
                "LEFT JOIN [ProductGroup] AS [RootProductGroup] " +
                "ON [RootProductGroup].[ID] = [ProductSubGroup].[RootProductGroupID] " +
                "LEFT JOIN [TOTAL_SEARCH] " +
                "ON [TOTAL_SEARCH].[ID] = [ProductGroup].[ID] " +
                "WHERE [ProductGroup].[ID] IN ( " +
                "SELECT [TOTAL_SEARCH].[ID] " +
                "FROM [TOTAL_SEARCH] " +
                "WHERE [TOTAL_SEARCH].[RowNumber] IS NOT NULL) " +
                "ORDER BY [RootProductGroup].[Name], [ProductGroup].[Name] ",
                (productGroup, rootProductGroup) => {
                    productGroup.RootProductGroup = rootProductGroup;
                    return productGroup;
                },
                new {
                    Value = value
                }).AsList(),
            TotalQty = _connection.Query<int>(
                "SELECT " +
                "COUNT(1) " +
                "FROM [ProductGroup] " +
                "WHERE [ProductGroup].[Deleted] = 0 ").FirstOrDefault()
        };

        return toReturn;
    }


    public ProductGroup GetByNetIdWithRootGroups(Guid netId) {
        ProductGroup toReturn =
            _connection.Query<ProductGroup>(
                ";WITH [TOTAL_SEARCH] AS ( " +
                "SELECT " +
                "ROW_NUMBER() OVER(ORDER BY [ProductGroup].ID DESC) AS [RowNumber] " +
                ", [ProductGroup].[ID] " +
                ", COUNT(DISTINCT [ProductSubGroup].[ID]) AS [TotalProductSubGroup] " +
                ", COUNT(DISTINCT [ProductProductGroup].[ID]) AS [TotalProduct] " +
                "FROM [ProductGroup] " +
                "LEFT JOIN [ProductSubGroup] " +
                "ON [ProductSubGroup].[RootProductGroupID] = [ProductGroup].[ID] " +
                "AND [ProductSubGroup].[Deleted] = 0 " +
                "LEFT JOIN [ProductProductGroup] " +
                "ON [ProductProductGroup].[ProductGroupID] = [ProductGroup].[ID] " +
                "AND [ProductProductGroup].[Deleted] = 0 " +
                "WHERE [ProductGroup].[Deleted] = 0 " +
                "AND [ProductGroup].[NetUID] = @NetId " +
                "GROUP BY [ProductGroup].[ID] " +
                ") " +
                "SELECT " +
                "[ProductGroup].* " +
                ", [TOTAL_SEARCH].[TotalProductSubGroup] " +
                ", [TOTAL_SEARCH].[TotalProduct] " +
                "FROM [ProductGroup] " +
                "LEFT JOIN [TOTAL_SEARCH] " +
                "ON [TOTAL_SEARCH].[ID] = [ProductGroup].[ID] " +
                "WHERE [ProductGroup].[ID] = ( " +
                "SELECT [TOTAL_SEARCH].[ID] " +
                "FROM [TOTAL_SEARCH] " +
                "); ",
                new { NetId = netId }).FirstOrDefault();

        if (toReturn == null) return null;

        if (toReturn.IsSubGroup)
            toReturn.RootProductGroups =
                _connection.Query<ProductSubGroup, ProductGroup, ProductSubGroup>(
                    "SELECT " +
                    "[ProductSubGroup].* " +
                    ", [ProductGroup].* " +
                    "FROM [ProductSubGroup] " +
                    "LEFT JOIN [ProductGroup] " +
                    "ON [ProductGroup].[ID] = [ProductSubGroup].[RootProductGroupID] " +
                    "AND [ProductGroup].[Deleted] = 0 " +
                    "WHERE [ProductSubGroup].[SubProductGroupID] = @Id " +
                    "AND [ProductSubGroup].[Deleted] = 0; ",
                    (productSubGroup, productRootGroup) => {
                        productSubGroup.RootProductGroup = productRootGroup;
                        return productSubGroup;
                    }, new { toReturn.Id }).AsList();

        return toReturn;
    }

    public ProductSubGroupsWithTotalModel GetFilteredSubGroupsProductGroup(
        Guid netId,
        int limit,
        int offset,
        string value) {
        ProductSubGroupsWithTotalModel toReturn = new() {
            ProductSubGroups = _connection.Query<ProductSubGroup, ProductGroup, ProductSubGroup>(
                ";WITH [TOTAL_SEARCH] AS ( " +
                "SELECT " +
                "ROW_NUMBER() OVER(ORDER BY [ProductSubGroup].[ID] DESC) AS [RowNumber] " +
                ", [ProductSubGroup].[ID] " +
                ", COUNT(DISTINCT [ProductGroupSubGroup].[ID]) AS [TotalProductSubGroup] " +
                ", COUNT(DISTINCT [ProductProductGroup].[ID]) AS [TotalProduct] " +
                "FROM [ProductSubGroup] " +
                "LEFT JOIN [ProductGroup] " +
                "ON [ProductGroup].[ID] = [ProductSubGroup].[SubProductGroupID] " +
                "AND [ProductGroup].[Deleted] = 0 " +
                "LEFT JOIN [ProductSubGroup] AS [ProductSubSubGroup] " +
                "ON [ProductSubSubGroup].[RootProductGroupID] = [ProductGroup].[ID] " +
                "AND [ProductSubSubGroup].[Deleted] = 0 " +
                "LEFT JOIN [ProductGroup] AS [ProductGroupSubGroup] " +
                "ON [ProductGroupSubGroup].[ID] = [ProductSubSubGroup].[SubProductGroupID] " +
                "LEFT JOIN [ProductProductGroup] " +
                "ON [ProductProductGroup].[ProductGroupID] = [ProductGroup].[ID] " +
                "LEFT JOIN [ProductGroup] AS [RootRootProductGroup] " +
                "ON [RootRootProductGroup].[ID] = [ProductSubGroup].[RootProductGroupID] " +
                "AND [RootRootProductGroup].[Deleted] = 0 " +
                "WHERE [ProductSubGroup].[Deleted] = 0 " +
                "AND [RootRootProductGroup].[NetUID] = @NetId " +
                "AND ( " +
                "[ProductGroup].[Name] LIKE '%' + @Value + '%' " +
                "OR [ProductGroup].[FullName] LIKE '%' + @Value + '%' " +
                "OR [ProductGroup].[Description] LIKE '%' + @Value + '%' " +
                ") " +
                "GROUP BY [ProductSubGroup].[ID] " +
                ") " +
                "SELECT " +
                "[ProductSubGroup].* " +
                ", [ProductGroup].* " +
                ", [TOTAL_SEARCH].[TotalProductSubGroup] " +
                ", [TOTAL_SEARCH].[TotalProduct] " +
                "FROM [ProductSubGroup] " +
                "LEFT JOIN [ProductGroup] " +
                "ON [ProductGroup].[ID] = [ProductSubGroup].[SubProductGroupID] " +
                "LEFT JOIN [TOTAL_SEARCH] " +
                "ON [TOTAL_SEARCH].[ID] = [ProductSubGroup].[ID] " +
                "WHERE [ProductSubGroup].[ID] IN ( " +
                "SELECT [TOTAL_SEARCH].[ID] " +
                "FROM [TOTAL_SEARCH] " +
                "WHERE [TOTAL_SEARCH].[RowNumber] > @Offset " +
                "AND [TOTAL_SEARCH].RowNumber <= @Limit + @Offset " +
                ") ",
                (productSubGroup, productGroup) => {
                    productSubGroup.SubProductGroup = productGroup;
                    return productSubGroup;
                }, new { NetId = netId, Limit = limit, Offset = offset, Value = value }).AsList()
        };

        toReturn.TotalFilteredQty =
            _connection.Query<int>(
                "SELECT " +
                "COUNT(1) " +
                "FROM [ProductSubGroup] " +
                "LEFT JOIN [ProductGroup] " +
                "ON [ProductGroup].[ID] = [ProductSubGroup].[SubProductGroupID] " +
                "AND [ProductGroup].[Deleted] = 0 " +
                "LEFT JOIN [ProductGroup] AS [RootRootProductGroup] " +
                "ON [RootRootProductGroup].[ID] = [ProductSubGroup].[RootProductGroupID] " +
                "AND [RootRootProductGroup].[Deleted] = 0 " +
                "WHERE [ProductSubGroup].[Deleted] = 0 " +
                "AND [RootRootProductGroup].[NetUID] = @NetId " +
                "AND ( " +
                "[ProductGroup].[Name] LIKE '%' + @Value + '%' " +
                "OR [ProductGroup].[FullName] LIKE '%' + @Value + '%' " +
                "OR [ProductGroup].[Description] LIKE '%' + @Value + '%' " +
                ") ",
                new { NetId = netId, Value = value }).FirstOrDefault();

        toReturn.TotalQty =
            _connection.Query<int>(
                "SELECT " +
                "COUNT(1) " +
                "FROM [ProductSubGroup] " +
                "LEFT JOIN [ProductGroup] AS [RootRootProductGroup] " +
                "ON [RootRootProductGroup].[ID] = [ProductSubGroup].[RootProductGroupID] " +
                "AND [RootRootProductGroup].[Deleted] = 0 " +
                "WHERE [ProductSubGroup].[Deleted] = 0 " +
                "AND [RootRootProductGroup].[NetUID] = @NetId; ",
                new { NetId = netId }).FirstOrDefault();

        return toReturn;
    }

    public List<ProductGroup> GetRootProductGroupsByNetId(Guid netId) {
        return _connection.Query<ProductGroup>(
            ";WITH [NOT_ROOT_IDS] AS ( " +
            "SELECT " +
            "[ProductSubGroup].[SubProductGroupID] " +
            "FROM [ProductGroup] " +
            "LEFT JOIN [ProductSubGroup] " +
            "ON [ProductSubGroup].[RootProductGroupID] = [ProductGroup].[ID] " +
            "AND [ProductSubGroup].[Deleted] = 0 " +
            "WHERE [ProductGroup].[NetUID] = @NetId " +
            "AND [ProductSubGroup].[SubProductGroupID] IS NOT NULL " +
            ") " +
            "SELECT * FROM [ProductGroup] " +
            "WHERE [ProductGroup].[Deleted] = 0 " +
            "AND [ProductGroup].[ID] NOT IN ( " +
            "SELECT * FROM [NOT_ROOT_IDS] " +
            ") " +
            "AND [ProductGroup].[NetUID] != @NetId ",
            new { NetId = netId }).AsList();
    }

    public ProductProductGroupsWithTotalModel GetFilteredProductByProductGroupNetId(
        Guid netId,
        int limit,
        int offset,
        string value) {
        ProductProductGroupsWithTotalModel toReturn = new() {
            ProductProductGroups = _connection.Query<ProductProductGroup, Product, MeasureUnit, ProductProductGroup>(
                ";WITH [FILTERED_PRODUCT_IDS] AS ( " +
                "SELECT " +
                "ROW_NUMBER() OVER(ORDER BY [ProductProductGroup].[ID]) AS [RowNumber] " +
                ", [ProductProductGroup].[ID] " +
                "FROM [ProductProductGroup] " +
                "LEFT JOIN [Product] " +
                "ON [Product].[ID] = [ProductProductGroup].[ProductID] " +
                "LEFT JOIN [ProductGroup] " +
                "ON [ProductGroup].[ID] = [ProductProductGroup].[ProductGroupID] " +
                "WHERE [ProductProductGroup].[Deleted] = 0 " +
                "AND [ProductGroup].[NetUID] = @NetId " +
                "AND ( " +
                "[Product].[VendorCode] LIKE '%' + @Value + '%' OR " +
                "[Product].[Name] LIKE '%' + @Value + '%' OR " +
                "[Product].[SearchName] LIKE '%' + @Value + '%' OR " +
                "[Product].[NameUA] LIKE '%' + @Value + '%' OR " +
                "[Product].[SearchNameUA] LIKE '%' + @Value + '%' OR " +
                "[Product].[Description] LIKE '%' + @Value + '%' OR " +
                "[Product].[DescriptionUA] LIKE '%' + @Value + '%' OR " +
                "[ProductProductGroup].[VendorCode] LIKE '%' + @Value + '%' " +
                ") " +
                ") " +
                "SELECT " +
                "* " +
                "FROM [ProductProductGroup] " +
                "LEFT JOIN [Product] " +
                "ON [Product].[ID] = [ProductProductGroup].[ProductID] " +
                "LEFT JOIN [MeasureUnit] " +
                "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
                "WHERE [ProductProductGroup].[ID] IN ( " +
                "SELECT [FILTERED_PRODUCT_IDS].[ID] " +
                "FROM [FILTERED_PRODUCT_IDS] " +
                "WHERE [FILTERED_PRODUCT_IDS].[RowNumber] > @Offset " +
                "AND [FILTERED_PRODUCT_IDS].[RowNumber] <= @Limit + @Offset " +
                ") " +
                "ORDER BY [ProductProductGroup].[ID] ",
                (productProductGroup, product, measureUnit) => {
                    product.MeasureUnit = measureUnit;
                    productProductGroup.Product = product;
                    return productProductGroup;
                }, new { NetId = netId, Limit = limit, Offset = offset, Value = value }).AsList()
        };

        toReturn.TotalFilteredQty =
            _connection.Query<int>(
                "SELECT " +
                "COUNT(1) " +
                "FROM [ProductProductGroup] " +
                "LEFT JOIN [Product] " +
                "ON [Product].[ID] = [ProductProductGroup].[ProductID] " +
                "LEFT JOIN [ProductGroup] " +
                "ON [ProductGroup].[ID] = [ProductProductGroup].[ProductGroupID] " +
                "WHERE [ProductProductGroup].[Deleted] = 0 " +
                "AND [ProductGroup].[NetUID] = @NetId " +
                "AND ( " +
                "[Product].[VendorCode] LIKE '%' + @Value + '%' OR " +
                "[Product].[Name] LIKE '%' + @Value + '%' OR " +
                "[Product].[SearchName] LIKE '%' + @Value + '%' OR " +
                "[Product].[NameUA] LIKE '%' + @Value + '%' OR " +
                "[Product].[SearchNameUA] LIKE '%' + @Value + '%' OR " +
                "[Product].[Description] LIKE '%' + @Value + '%' OR " +
                "[Product].[DescriptionUA] LIKE '%' + @Value + '%' OR " +
                "[ProductProductGroup].[VendorCode] LIKE '%' + @Value + '%' " +
                ") ",
                new { NetId = netId, Value = value }).FirstOrDefault();

        toReturn.TotalQty =
            _connection.Query<int>(
                "SELECT " +
                "COUNT(1) " +
                "FROM [ProductProductGroup] " +
                "LEFT JOIN [ProductGroup] " +
                "ON [ProductGroup].[ID] = [ProductProductGroup].[ProductGroupID] " +
                "WHERE [ProductProductGroup].[Deleted] = 0 " +
                "AND [ProductGroup].[NetUID] = @NetId ",
                new { Netid = netId }).FirstOrDefault();

        return toReturn;
    }

    public IEnumerable<ProductGroup> GetAllForReSaleAvailabilities() {
        List<ProductGroup> toReturn = new();

        List<ProductGroup> productGroups =
            _connection.Query<ProductGroup>(
                ";WITH [FILTERED_PRODUCT_GROUP_CTE] AS ( " +
                "SELECT [ProductGroup].[ID] FROM [ReSaleAvailability] " +
                "LEFT JOIN [ProductAvailability] " +
                "ON [ProductAvailability].[ID] = [ReSaleAvailability].[ProductAvailabilityID] " +
                "LEFT JOIN [Product] " +
                "ON [Product].[ID] = [ProductAvailability].[ProductID] " +
                "LEFT JOIN [ProductProductGroup] " +
                "ON [ProductProductGroup].[ProductID] = [Product].[ID] " +
                "LEFT JOIN [ProductGroup] " +
                "ON [ProductGroup].[ID] = [ProductProductGroup].[ProductGroupID] " +
                "WHERE [ReSaleAvailability].[Deleted] = 0 " +
                "AND [ReSaleAvailability].[RemainingQty] > 0 " +
                "AND [ProductGroup].[ID] IS NOT NULL " +
                "GROUP BY [ProductGroup].[ID] " +
                ") " +
                "SELECT " +
                "[ProductGroup].* " +
                "FROM [FILTERED_PRODUCT_GROUP_CTE] " +
                "LEFT JOIN [ProductGroup] " +
                "ON [ProductGroup].[ID] = [FILTERED_PRODUCT_GROUP_CTE].[ID] ").ToList();

        if (!productGroups.Any())
            return toReturn;

        toReturn.AddRange(productGroups.Where(x => !x.IsSubGroup));

        if (productGroups.Any(x => x.IsSubGroup))
            _connection.Query<ProductGroup, ProductSubGroup, ProductGroup>(
                "SELECT * FROM [ProductGroup] " +
                "LEFT JOIN [ProductSubGroup] " +
                "ON [ProductSubGroup].[SubProductGroupID] = [ProductGroup].[ID] " +
                "WHERE [ProductGroup].[ID] IN @Ids " +
                "ORDER BY [ProductGroup].[IsSubGroup] DESC",
                (productGroup, productSubGroup) => {
                    if (toReturn.Any(x => x.Id.Equals(productSubGroup.RootProductGroupId)) &&
                        !toReturn.Any(x => x.Id.Equals(productGroup.Id))) {
                        if (!toReturn.First(x => x.Id.Equals(productSubGroup.RootProductGroupId)).SubProductGroups.Any(x => x.Id.Equals(productSubGroup.Id))) {
                            productSubGroup.SubProductGroup = productGroup;

                            toReturn.First(x => x.Id.Equals(productSubGroup.RootProductGroupId)).SubProductGroups.Add(productSubGroup);
                        }
                    } else {
                        if (!toReturn.Any(x => x.Id.Equals(productGroup.Id)) &&
                            !toReturn.Any(x => x.SubProductGroups.Any(y => y.Id.Equals(productGroup.Id))))
                            toReturn.Add(productGroup);
                    }

                    return productGroup;
                }, new { Ids = productGroups.Where(x => x.IsSubGroup).Select(x => x.Id) });

        return toReturn;
    }
}