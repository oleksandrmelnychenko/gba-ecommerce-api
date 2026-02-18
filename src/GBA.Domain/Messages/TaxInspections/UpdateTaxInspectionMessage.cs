using GBA.Domain.Entities;

namespace GBA.Domain.Messages.TaxInspections;

public sealed class UpdateTaxInspectionMessage {
    public UpdateTaxInspectionMessage(TaxInspection taxInspection) {
        TaxInspection = taxInspection;
    }

    public TaxInspection TaxInspection { get; }
}