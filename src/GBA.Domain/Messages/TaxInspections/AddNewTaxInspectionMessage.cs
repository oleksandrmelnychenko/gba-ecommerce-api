using GBA.Domain.Entities;

namespace GBA.Domain.Messages.TaxInspections;

public sealed class AddNewTaxInspectionMessage {
    public AddNewTaxInspectionMessage(TaxInspection taxInspection) {
        TaxInspection = taxInspection;
    }

    public TaxInspection TaxInspection { get; }
}