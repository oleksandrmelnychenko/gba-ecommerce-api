using System.Collections.Generic;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.EntityHelpers;

public sealed class Reservation {
    public Reservation() {
        ProductReservationsPL = new List<ProductReservation>();

        ProductReservationsUK = new List<ProductReservation>();

        CartProductReservationsPL = new List<ProductReservation>();

        CartProductReservationsUK = new List<ProductReservation>();

        DefectiveAvailabilities = new List<ProductAvailability>();
    }

    public double TotalReservedPL { get; set; }

    public double TotalReservedUK { get; set; }

    public double TotalCartReservedPL { get; set; }

    public double TotalCartReservedUK { get; set; }

    public List<ProductReservation> ProductReservationsPL { get; set; }

    public List<ProductReservation> ProductReservationsUK { get; set; }

    public List<ProductReservation> CartProductReservationsPL { get; set; }

    public List<ProductReservation> CartProductReservationsUK { get; set; }

    public SupplyOrderUkraineCartItem SupplyOrderUkraineCartItem { get; set; }

    public List<ProductAvailability> DefectiveAvailabilities { get; set; }

    public int TotalProductReSaleQty { get; set; }
}