using GBA.Domain.EntityHelpers.Accounting;

namespace GBA.Domain.Entities.AccountingDocumentNames;

public sealed class AccountingDocumentName : EntityBase {
    public JoinServiceType DocumentType { get; set; }

    public string NameUK { get; set; }

    public string NamePL { get; set; }
}