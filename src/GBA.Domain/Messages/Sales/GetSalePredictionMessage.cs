using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSalePredictionMessage {
    public GetSalePredictionMessage(
        Guid clientNetId,
        Guid producttNetId
    ) {
        ClientNetId = clientNetId;

        ProductNetId = producttNetId;
    }

    public Guid ClientNetId { get; set; }

    public Guid ProductNetId { get; set; }
}