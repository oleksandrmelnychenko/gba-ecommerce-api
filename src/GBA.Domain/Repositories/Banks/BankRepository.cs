using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.Banks.Contracts;

namespace GBA.Domain.Repositories.Banks;

public sealed class BankRepository : IBankRepository {
    private readonly IDbConnection _connection;

    public BankRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<Bank> GetAll() {
        return _connection.Query<Bank>(
            "SELECT * FROM [Bank] " +
            "WHERE [Deleted] = 0 ");
    }

    public void Update(Bank bank) {
        _connection.Execute(
            "UPDATE [Bank] " +
            "SET [Name] = @Name, [MfoCode] = @MfoCode, [EdrpouCode] = @EdrpouCode, [City] = @City, [Address] = @Address, " +
            "[Phones] = @Phones, [Deleted] = @Deleted, [Updated] = GETUTCDATE() " +
            "WHERE ID = @ID;",
            bank);
    }

    public long Add(Bank bank) {
        return _connection.Query<long>(
            "INSERT INTO [Bank] ([Name], [MfoCode], [EdrpouCode], [City], [Address], [Phones], [Updated]) " +
            "VALUES (@Name, @MfoCode, @EdrpouCode, @City, @Address, @Phones, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY() ",
            bank).Single();
    }
}