namespace GBA.Domain.Messages.Consignments;

public sealed class ValidateAndStoreConsignmentMovementFromSupplyReturnMessage {
    public ValidateAndStoreConsignmentMovementFromSupplyReturnMessage(long supplyReturnId, bool withReSale) {
        SupplyReturnId = supplyReturnId;
        WithReSale = withReSale;
    }

    public long SupplyReturnId { get; }
    public bool WithReSale { get; }
}