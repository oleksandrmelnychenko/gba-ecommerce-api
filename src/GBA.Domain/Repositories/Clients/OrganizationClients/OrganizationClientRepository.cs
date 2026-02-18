using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Repositories.Clients.OrganizationClients.Contracts;

namespace GBA.Domain.Repositories.Clients.OrganizationClients;

public sealed class OrganizationClientRepository : IOrganizationClientRepository {
    private readonly IDbConnection _connection;

    public OrganizationClientRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(OrganizationClient client) {
        return _connection.Query<long>(
                "INSERT INTO [OrganizationClient] (FullName, Address, Country, City, NIP, MarginAmount, Updated) " +
                "VALUES (@FullName, @Address, @Country, @City, @NIP, @MarginAmount, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                client
            )
            .Single();
    }

    public void Update(OrganizationClient client) {
        _connection.Execute(
            "UPDATE [OrganizationClient] " +
            "SET FullName = @FullName, Address = @Address, Country = @Country, City = @City, NIP = @NIP, MarginAmount = @MarginAmount, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            client
        );
    }

    public OrganizationClient GetById(long id) {
        OrganizationClient toReturn =
            _connection.Query<OrganizationClient>(
                    "SELECT * " +
                    "FROM [OrganizationClient] " +
                    "WHERE ID = @Id",
                    new { Id = id }
                )
                .SingleOrDefault();

        if (toReturn != null)
            toReturn.OrganizationClientAgreements =
                _connection.Query<OrganizationClientAgreement, Currency, OrganizationClientAgreement>(
                    "SELECT * " +
                    "FROM [OrganizationClientAgreement] " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [OrganizationClientAgreement].CurrencyID " +
                    "WHERE [OrganizationClientAgreement].OrganizationClientID = @Id " +
                    "AND [OrganizationClientAgreement].Deleted = 0",
                    (agreement, currency) => {
                        agreement.Currency = currency;

                        return agreement;
                    },
                    new { toReturn.Id }
                ).ToList();

        return toReturn;
    }

    public OrganizationClient GetByNetId(Guid netId) {
        OrganizationClient toReturn =
            _connection.Query<OrganizationClient>(
                    "SELECT * " +
                    "FROM [OrganizationClient] " +
                    "WHERE NetUID = @NetId",
                    new { NetId = netId }
                )
                .SingleOrDefault();

        if (toReturn != null)
            toReturn.OrganizationClientAgreements =
                _connection.Query<OrganizationClientAgreement, Currency, OrganizationClientAgreement>(
                    "SELECT * " +
                    "FROM [OrganizationClientAgreement] " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [OrganizationClientAgreement].CurrencyID " +
                    "WHERE [OrganizationClientAgreement].OrganizationClientID = @Id " +
                    "AND [OrganizationClientAgreement].Deleted = 0",
                    (agreement, currency) => {
                        agreement.Currency = currency;

                        return agreement;
                    },
                    new { toReturn.Id }
                ).ToList();

        return toReturn;
    }

    public IEnumerable<OrganizationClient> GetAll() {
        IEnumerable<OrganizationClient> clients =
            _connection.Query<OrganizationClient>(
                "SELECT * " +
                "FROM [OrganizationClient] " +
                "WHERE Deleted = 0"
            );

        if (clients.Any())
            _connection.Query<OrganizationClientAgreement, Currency, OrganizationClientAgreement>(
                "SELECT * " +
                "FROM [OrganizationClientAgreement] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [OrganizationClientAgreement].CurrencyID " +
                "WHERE [OrganizationClientAgreement].OrganizationClientID IN @Ids " +
                "AND [OrganizationClientAgreement].Deleted = 0",
                (agreement, currency) => {
                    agreement.Currency = currency;

                    clients.First(i => i.Id.Equals(agreement.OrganizationClientId)).OrganizationClientAgreements.Add(agreement);

                    return agreement;
                },
                new { Ids = clients.Select(i => i.Id) }
            );

        return clients;
    }

    public IEnumerable<OrganizationClient> GetAllFromSearch(string value) {
        IEnumerable<OrganizationClient> clients =
            _connection.Query<OrganizationClient>(
                "SELECT * " +
                "FROM [OrganizationClient] " +
                "WHERE Deleted = 0 " +
                "AND FullName like '%' + @Value + '%'",
                new { Value = value }
            );

        if (clients.Any())
            _connection.Query<OrganizationClientAgreement, Currency, OrganizationClientAgreement>(
                "SELECT * " +
                "FROM [OrganizationClientAgreement] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [OrganizationClientAgreement].CurrencyID " +
                "WHERE [OrganizationClientAgreement].OrganizationClientID IN @Ids " +
                "AND [OrganizationClientAgreement].Deleted = 0",
                (agreement, currency) => {
                    agreement.Currency = currency;

                    clients.First(i => i.Id.Equals(agreement.OrganizationClientId)).OrganizationClientAgreements.Add(agreement);

                    return agreement;
                },
                new { Ids = clients.Select(i => i.Id) }
            );

        return clients;
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [OrganizationClient] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [OrganizationClient].ID = @Id",
            new { Id = id }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [OrganizationClient] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [OrganizationClient].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}