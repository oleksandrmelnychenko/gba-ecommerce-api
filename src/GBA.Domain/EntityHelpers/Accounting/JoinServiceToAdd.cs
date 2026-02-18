namespace GBA.Domain.EntityHelpers.Accounting;

public sealed class JoinServiceToAdd {
    public JoinServiceToAdd(JoinService joinService, decimal grossPrice) {
        JoinService = joinService;

        GrossPrice = grossPrice;
    }

    public JoinService JoinService { get; }

    public decimal GrossPrice { get; }
}