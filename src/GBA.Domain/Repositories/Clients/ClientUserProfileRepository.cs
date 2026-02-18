using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientUserProfileRepository : IClientUserProfileRepository {
    private readonly IDbConnection _connection;

    public ClientUserProfileRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<ClientUserProfile> clientUserProfiles) {
        _connection.Execute(
            "INSERT INTO ClientUserProfile (ClientId, UserProfileId, Updated) " +
            "VALUES (@ClientId, @UserProfileId, getutcdate())",
            clientUserProfiles
        );
    }

    public void Update(IEnumerable<ClientUserProfile> clientUserProfiles) {
        _connection.Execute(
            "UPDATE ClientUserProfile SET " +
            "ClientId = @ClientId, UserProfileId = @UserProfileId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            clientUserProfiles
        );
    }

    public List<ClientUserProfile> GetAllByClientId(long id) {
        return _connection.Query<ClientUserProfile>(
                "SELECT * FROM ClientUserProfile " +
                "WHERE ClientId = @Id AND Deleted = 0",
                new { Id = id }
            )
            .ToList();
    }

    public void Remove(IEnumerable<ClientUserProfile> clientUserProfiles) {
        _connection.Execute(
            "UPDATE ClientUserProfile SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            clientUserProfiles
        );
    }

    public void RemoveAllByClientId(long clientId) {
        _connection.Execute(
            "UPDATE [ClientUserProfile] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ClientUserProfile].ClientID = @ClientId",
            new { ClientId = clientId }
        );
    }
}