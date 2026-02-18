using System.Data;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientOneCRepositoriesFactory {
    IClientOneCRepository NewClientOneCRepository(IDbConnection connection);
}