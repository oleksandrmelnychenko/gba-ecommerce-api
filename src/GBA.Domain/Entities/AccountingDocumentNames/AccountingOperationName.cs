using GBA.Domain.EntityHelpers.Accounting;

namespace GBA.Domain.Entities.AccountingDocumentNames;

public sealed class AccountingOperationName : EntityBase {
    public OperationType OperationType { get; set; }

    public string BankNameUK { get; set; }

    public string BankNamePL { get; set; }

    public string CashNameUK { get; set; }

    public string CashNamePL { get; set; }
}