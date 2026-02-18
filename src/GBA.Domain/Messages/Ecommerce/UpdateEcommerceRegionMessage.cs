using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Messages.Ecommerce;

public sealed class UpdateEcommerceRegionMessage {
    public UpdateEcommerceRegionMessage(EcommerceRegion ecommerceRegion) {
        EcommerceRegion = ecommerceRegion;
    }

    public EcommerceRegion EcommerceRegion { get; }
}