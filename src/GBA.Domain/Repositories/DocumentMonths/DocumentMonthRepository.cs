using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.DocumentMonths.Contracts;

namespace GBA.Domain.Repositories.DocumentMonths;

public sealed class DocumentMonthRepository : IDocumentMonthRepository {
    private readonly IDbConnection _connection;

    public DocumentMonthRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<DocumentMonth> GetAllByCulture(string culture) {
        return _connection.Query<DocumentMonth>(
            "SELECT * " +
            "FROM [DocumentMonth] " +
            "WHERE [DocumentMonth].CultureCode = @Culture",
            new { Culture = culture }
        );
    }
}