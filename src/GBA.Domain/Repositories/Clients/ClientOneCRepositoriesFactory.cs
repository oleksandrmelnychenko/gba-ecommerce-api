using System.Data;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientOneCRepositoriesFactory : IClientOneCRepositoriesFactory {
    public IClientOneCRepository NewClientOneCRepository(IDbConnection connection) {
        return new ClientOneCRepository(connection);
    }
}