using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.Messages.ReSales;

public sealed class GetAllReSaleAvailabilitiesFilteredMessage {
    public GetAllReSaleAvailabilitiesFilteredMessage(FilterReSaleAvailabilityModel filter) {
        Filter = filter;
    }

    public FilterReSaleAvailabilityModel Filter { get; }
}