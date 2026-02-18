using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.Categories.Contracts;

namespace GBA.Domain.Repositories.Categories;

public sealed class CategoryRepository : ICategoryRepository {
    private readonly IDbConnection _connection;

    public CategoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Category category) {
        return _connection.Query<long>("INSERT INTO Category (Name, Description, RootCategoryID, Updated) " +
                                       "VALUES(@Name, @Description, @RootCategoryId, getutcdate());" +
                                       "SELECT SCOPE_IDENTITY()", category)
            .Single();
    }

    public List<Category> GetAll() {
        return _connection.Query<Category>(
                "SELECT * FROM Category WHERE Deleted = 0")
            .ToList();
    }

    public Category GetById(long id) {
        return _connection.Query<Category>(
                "SELECT * FROM Category WHERE ID = @Id",
                new { Id = id })
            .SingleOrDefault();
    }

    public Category GetByNetId(Guid netId) {
        return _connection.Query<Category>(
                "SELECT * FROM Category WHERE NetUID = @NetId",
                new { NetId = netId })
            .SingleOrDefault();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Category SET Deleted = 1 WHERE NetUID = @NetId",
            new { NetId = netId });
    }

    public void Update(Category category) {
        _connection.Execute(
            "UPDATE Category SET Name = @Name, Description = @Description, RootCategoryId = @RootCategoryId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid", category);
    }
}