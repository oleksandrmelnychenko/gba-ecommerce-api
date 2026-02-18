using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.Messages.ReSales;

public sealed class GetGenerateAutomaticallyReSaleMessage {
    public GetGenerateAutomaticallyReSaleMessage(
        GenerateAutomaticallyReSaleModel filter) {
        Filter = filter;
    }

    public GenerateAutomaticallyReSaleModel Filter { get; }
}