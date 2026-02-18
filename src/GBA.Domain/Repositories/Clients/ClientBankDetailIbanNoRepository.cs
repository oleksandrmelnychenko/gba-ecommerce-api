using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientBankDetailIbanNoRepository : IClientBankDetailIbanNoRepository {
    private readonly IDbConnection _connection;

    public ClientBankDetailIbanNoRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ClientBankDetailIbanNo clientBankDetailIbanNo) {
        return _connection.Query<long>(
                "INSERT INTO ClientBankDetailIbanNo (IBANNO, CurrencyID, Updated) " +
                "VALUES(@IBANNO, @CurrencyID, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                clientBankDetailIbanNo
            )
            .Single();
    }

    public void Update(ClientBankDetailIbanNo clientBankDetailIbanNo) {
        _connection.Execute(
            "UPDATE ClientBankDetailIbanNo SET IBANNO = @IBANNO, CurrencyID = @CurrencyID, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            clientBankDetailIbanNo
        );
    }
}