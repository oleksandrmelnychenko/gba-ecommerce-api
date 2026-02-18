using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientBalanceMovementRepository {
    void Add(ClientBalanceMovement movement);

    void AddInMovement(ClientBalanceMovement movement);

    void AddOutMovement(ClientBalanceMovement movement);

    void Update(ClientBalanceMovement movement);
}