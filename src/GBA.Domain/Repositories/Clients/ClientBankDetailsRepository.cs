using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientBankDetailsRepository : IClientBankDetailsRepository {
    private readonly IDbConnection _connection;

    public ClientBankDetailsRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ClientBankDetails clientBankDetails) {
        return _connection.Query<long>(
                "INSERT INTO ClientBankDetails (BankAddress, BankAndBranch, AccountNumberID, ClientBankDetailIbanNoID, Swift, BranchCode, Updated) " +
                "VALUES(@BankAddress, @BankAndBranch, @AccountNumberID, @ClientBankDetailIbanNoID, @Swift, @BranchCode, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                clientBankDetails
            )
            .Single();
    }

    public void Update(ClientBankDetails clientBankDetails) {
        _connection.Execute(
            "UPDATE ClientBankDetails " +
            "SET BankAddress = @BankAddress, BankAndBranch = @BankAndBranch, AccountNumberID = @AccountNumberID, ClientBankDetailIbanNoID = @ClientBankDetailIbanNoID, Swift = @Swift, BranchCode = @BranchCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            clientBankDetails
        );
    }
}