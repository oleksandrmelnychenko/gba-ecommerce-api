using System.Data;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientBalanceMovementRepository : IClientBalanceMovementRepository {
    private readonly IDbConnection _connection;

    public ClientBalanceMovementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ClientBalanceMovement movement) {
        _connection.Execute(
            "INSERT INTO [ClientBalanceMovement] (Amount, ExchangeRateAmount, MovementType, ClientAgreementID, Updated) " +
            "VALUES (@Amount, @ExchangeRateAmount, @MovementType, @ClientAgreementId, GETUTCDATE())",
            movement
        );
    }

    public void AddInMovement(ClientBalanceMovement movement) {
        _connection.Execute(
            "INSERT INTO [ClientBalanceMovement] (Amount, ExchangeRateAmount, MovementType, ClientAgreementID, Updated) " +
            "VALUES (@Amount, @ExchangeRateAmount, 0, @ClientAgreementId, GETUTCDATE())",
            movement
        );
    }

    public void AddOutMovement(ClientBalanceMovement movement) {
        _connection.Execute(
            "INSERT INTO [ClientBalanceMovement] (Amount, ExchangeRateAmount, MovementType, ClientAgreementID, Updated) " +
            "VALUES (@Amount, @ExchangeRateAmount, 1, @ClientAgreementId, GETUTCDATE())",
            movement
        );
    }

    public void Update(ClientBalanceMovement movement) {
        _connection.Execute(
            "UPDATE [ClientBalanceMovement] " +
            "SET Amount = @Amount, ExchangeRateAmount = @ExchangeRateAmount, MovementType = @MovementType, GETUTCDATE() " +
            "WHERE ID = @Id",
            movement
        );
    }
}