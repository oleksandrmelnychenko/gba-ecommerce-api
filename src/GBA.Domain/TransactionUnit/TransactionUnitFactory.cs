using GBA.Domain.TransactionUnit.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Domain.TransactionUnit;

public class TransactionUnitFactory : ITransactionUnitFactory {
    private readonly TransactionUnit _transactionUnit;

    public TransactionUnitFactory([FromServices] TransactionUnit transactionUnit) {
        _transactionUnit = transactionUnit;
    }

    public TransactionUnit New() {
        return _transactionUnit;
    }
}