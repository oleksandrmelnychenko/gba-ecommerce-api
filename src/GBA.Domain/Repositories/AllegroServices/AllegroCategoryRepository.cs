using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.AllegroServices;
using GBA.Domain.Repositories.AllegroServices.Contracts;

namespace GBA.Domain.Repositories.AllegroServices;

public sealed class AllegroCategoryRepository : IAllegroCategoryRepository {
    private readonly IDbConnection _connection;

    public AllegroCategoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<AllegroCategory> GetAll() {
        return _connection.Query<AllegroCategory>(
                "SELECT * " +
                "FROM [AllegroCategory] " +
                "WHERE [AllegroCategory].Deleted = 0"
            )
            .ToList();
    }

    public List<AllegroCategory> GetAllRootCategoriesWithSubCategories() {
        List<AllegroCategory> toReturn = new();

        _connection.Query<AllegroCategory, AllegroCategory, AllegroCategory>(
            "SELECT * " +
            "FROM [AllegroCategory] " +
            "LEFT JOIN [AllegroCategory] AS [SubCategory] " +
            "ON [SubCategory].ParentCategoryID = [AllegroCategory].CategoryID " +
            "WHERE [AllegroCategory].ParentCategoryID = 0",
            (category, subCategory) => {
                if (!toReturn.Any(c => c.Id.Equals(category.Id))) {
                    category.SubCategories.Add(subCategory);

                    toReturn.Add(category);
                } else {
                    toReturn.First(c => c.Id.Equals(category.Id)).SubCategories.Add(subCategory);
                }

                return subCategory;
            }
        );

        return toReturn;
    }

    public List<AllegroCategory> GetAllSubCategoriesByParentCategoryId(long id) {
        return _connection.Query<AllegroCategory>(
                "SELECT * " +
                "FROM [AllegroCategory] " +
                "WHERE [AllegroCategory].ParentCategoryID = @Id",
                new { Id = id }
            )
            .ToList();
    }

    public List<AllegroCategory> GetAllFromSearch(string value, int limit, int offset) {
        return _connection.Query<AllegroCategory>(
                ";WITH [Search_CTE] " +
                "AS " +
                "( " +
                "SELECT ROW_NUMBER() OVER (ORDER BY [AllegroCategory].CategoryID) AS RowNumber " +
                ",[AllegroCategory].* " +
                "FROM [AllegroCategory] " +
                "WHERE [AllegroCategory].Name like '%' + @Value + '%' " +
                "AND [AllegroCategory].IsLeaf = 1 " +
                ") " +
                "SELECT * " +
                "FROM [Search_CTE] " +
                "WHERE [Search_CTE].RowNumber > @Offset " +
                "AND [Search_CTE].RowNumber <= (@Limit + @Offset) " +
                "ORDER BY [Search_CTE].RowNumber",
                new { Value = value, Limit = limit, Offset = offset }
            )
            .ToList();
    }

    public List<AllegroCategory> GetTreeByCategoryId(int categoryId) {
        return _connection.Query<AllegroCategory>(
                ";WITH [ParentCategory_CTE] " +
                "AS " +
                "(" +
                "SELECT * " +
                "FROM [AllegroCategory] " +
                "WHERE [AllegroCategory].CategoryID = @CategoryID " +
                "UNION ALL " +
                "SELECT [ParentCategory].* " +
                "FROM [AllegroCategory] AS [ParentCategory] " +
                "JOIN [ParentCategory_CTE] " +
                "ON [ParentCategory_CTE].ParentCategoryID = [ParentCategory].CategoryID" +
                ") " +
                "SELECT * " +
                "FROM [ParentCategory_CTE] " +
                "ORDER BY [ParentCategory_CTE].ParentCategoryID",
                new { CategoryId = categoryId }
            )
            .ToList();
    }

    public void Add(IEnumerable<AllegroCategory> categories) {
        _connection.Execute(
            "INSERT INTO [AllegroCategory] (Name, CategoryId, ParentCategoryId, Position, IsLeaf, Updated) " +
            "VALUES (@Name, @CategoryId, @ParentCategoryId, @Position, @IsLeaf, getutcdate())",
            categories
        );
    }

    public void Update(IEnumerable<AllegroCategory> categories) {
        _connection.Execute(
            "UPDATE [AllegroCategory] " +
            "SET Name = @Name, CategoryId = @CategoryId, ParentCategoryId = @ParentCategoryId, Position = @Position, IsLeaf = @IsLeaf, Updated = getutcdate() " +
            "WHERE [AllegroCategory].NetUID = @NetUid ",
            categories
        );
    }

    public void RemoveByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [AllegroCategory] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [AllegroCategory].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void DeleteRemovedCategories() {
        _connection.Execute(
            "DELETE FROM [AllegroCategory] " +
            "WHERE [AllegroCategory].Deleted = 1"
        );
    }
}