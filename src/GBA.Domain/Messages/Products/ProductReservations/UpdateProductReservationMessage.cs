using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products.ProductReservations;

public sealed class UpdateProductReservationMessage {
    public UpdateProductReservationMessage(ProductReservation productReservation) {
        ProductReservation = productReservation;
    }

    public ProductReservation ProductReservation { get; set; }
}