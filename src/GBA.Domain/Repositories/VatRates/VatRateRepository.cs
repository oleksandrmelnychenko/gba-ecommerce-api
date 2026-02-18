using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.Repositories.VatRates.Contracts;

namespace GBA.Domain.Repositories.VatRates;

public sealed class VatRateRepository : IVatRateRepository {
    private readonly IDbConnection _connection;

    public VatRateRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long New(VatRate vatRate) {
        return _connection.Query<long>(
            "INSERT INTO [VatRate]([Updated], [Value]) " +
            "VALUES (getutcdate(), @Value); " +
            "SELECT SCOPE_IDENTITY(); ",
            vatRate).FirstOrDefault();
    }

    public void Update(VatRate vatRate) {
        _connection.Execute(
            "UPDATE [VatRate] " +
            "SET [Updated] = getutcdate() " +
            ", [Name] = @Value " +
            "WHERE [VatRate].[ID] = @Id; ");
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [VatRate] " +
            "SET [Updated] = getutcdate() " +
            ", [Deleted] = 1 " +
            "WHERE [VatRate].[NetUID] = @NetId; ",
            new { NetId = netId });
    }

    public IEnumerable<VatRate> GetAll() {
        return _connection.Query<VatRate>(
            "SELECT * FROM [VatRate] " +
            "WHERE [VatRate].[Deleted] = 0 ").AsList();
    }

    public VatRate GetById(long id) {
        return _connection.Query<VatRate>(
            "SELECT * FROM [VatRate] " +
            "WHERE [VatRate].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }
}