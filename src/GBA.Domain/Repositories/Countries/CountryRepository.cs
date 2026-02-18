using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.Countries.Contracts;

namespace GBA.Domain.Repositories.Countries;

public sealed class CountryRepository : ICountryRepository {
    private readonly IDbConnection _connection;

    public CountryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Country country) {
        return _connection.Query<long>(
                "INSERT INTO [Country] ([Name], [Code], Updated) " +
                "VALUES (@Name, @Code, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                country
            )
            .Single();
    }

    public void Update(Country country) {
        _connection.Execute(
            "UPDATE [Country] " +
            "SET [Name] = @Name, [Code] = @Code, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            country
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [Country] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public Country GetById(long id) {
        return _connection.Query<Country>(
                "SELECT * " +
                "FROM [Country] " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public Country GetByNetId(Guid netId) {
        return _connection.Query<Country>(
                "SELECT * " +
                "FROM [Country] " +
                "WHERE NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public List<Country> GetAll() {
        return _connection.Query<Country>(
                "SELECT * " +
                "FROM [Country] " +
                "WHERE Deleted = 0 " +
                "ORDER BY [Country].[Name] "
            )
            .ToList();
    }
}