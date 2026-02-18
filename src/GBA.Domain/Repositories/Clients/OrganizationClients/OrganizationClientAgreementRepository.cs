using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Repositories.Clients.OrganizationClients.Contracts;

namespace GBA.Domain.Repositories.Clients.OrganizationClients;

public sealed class OrganizationClientAgreementRepository : IOrganizationClientAgreementRepository {
    private readonly IDbConnection _connection;

    public OrganizationClientAgreementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(OrganizationClientAgreement agreement) {
        _connection.Execute(
            "INSERT INTO [OrganizationClientAgreement] (Number, FromDate, CurrencyId, OrganizationClientId, Updated) " +
            "VALUES (@Number, @FromDate, @CurrencyId, @OrganizationClientId, GETUTCDATE())",
            agreement
        );
    }

    public void Add(IEnumerable<OrganizationClientAgreement> agreements) {
        _connection.Execute(
            "INSERT INTO [OrganizationClientAgreement] (Number, FromDate, CurrencyId, OrganizationClientId, Updated) " +
            "VALUES (@Number, @FromDate, @CurrencyId, @OrganizationClientId, GETUTCDATE())",
            agreements
        );
    }

    public void Update(OrganizationClientAgreement agreement) {
        _connection.Execute(
            "UPDATE [OrganizationClientAgreement] " +
            "SET FromDate = @FromDate, CurrencyId = @CurrencyId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            agreement
        );
    }

    public void Update(IEnumerable<OrganizationClientAgreement> agreements) {
        _connection.Execute(
            "UPDATE [OrganizationClientAgreement] " +
            "SET FromDate = @FromDate, CurrencyId = @CurrencyId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            agreements
        );
    }

    public OrganizationClientAgreement GetLastRecord() {
        return _connection.Query<OrganizationClientAgreement>(
                "SELECT TOP(1) * " +
                "FROM [OrganizationClientAgreement] " +
                "ORDER BY ID DESC"
            )
            .SingleOrDefault();
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [OrganizationClientAgreement] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByClientIdExceptProvided(long clientId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [OrganizationClientAgreement] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID NOT IN @Ids " +
            "AND OrganizationClientID = @ClientId " +
            "AND Deleted = 0",
            new { ClientId = clientId, Ids = ids }
        );
    }
}