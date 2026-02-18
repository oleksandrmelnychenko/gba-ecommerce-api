using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientBankDetailAccountNumberRepository : IClientBankDetailAccountNumberRepository {
    private readonly IDbConnection _connection;

    public ClientBankDetailAccountNumberRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ClientBankDetailAccountNumber clientBankDetailAccountNumber) {
        return _connection.Query<long>(
                "INSERT INTO ClientBankDetailAccountNumber (AccountNumber, CurrencyID, Updated) " +
                "VALUES(@AccountNumber, @CurrencyID, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                clientBankDetailAccountNumber
            )
            .Single();
    }

    public void Update(ClientBankDetailAccountNumber clientBankDetailAccountNumber) {
        _connection.Execute(
            "UPDATE ClientBankDetailAccountNumber " +
            "SET AccountNumber = @AccountNumber, CurrencyID = @CurrencyID, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            clientBankDetailAccountNumber
        );
    }
}