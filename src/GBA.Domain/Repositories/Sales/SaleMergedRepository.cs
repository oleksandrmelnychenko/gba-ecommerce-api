using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Sales.SaleMerges;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class SaleMergedRepository : ISaleMergedRepository {
    private readonly IDbConnection _connection;

    public SaleMergedRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SaleMerged saleMerged) {
        return _connection.Query<long>(
                "INSERT INTO SaleMerged (InputSaleID, OutputSaleId, Updated) " +
                "VALUES(@InputSaleID, @OutputSaleId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                saleMerged
            )
            .Single();
    }

    public void Add(List<SaleMerged> salesMerged) {
        _connection.Execute(
            "INSERT INTO SaleMerged (InputSaleID, OutputSaleId, Updated) " +
            "VALUES(@InputSaleID, @OutputSaleId, getutcdate()); ",
            salesMerged
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE SaleMerged " +
            "SET Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }
}