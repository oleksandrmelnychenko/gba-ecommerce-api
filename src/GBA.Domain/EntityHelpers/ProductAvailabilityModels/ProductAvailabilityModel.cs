using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.ProductAvailabilityModels;

public sealed class ProductAvailabilityModel {
    public ProductAvailabilityModel() {
        InAccounts = new List<AvailabilityModel>();
        InStorageUkrVat = new List<AvailabilityModel>();
        InStorageUkrNotVat = new List<AvailabilityModel>();
        InStoragePl = new List<AvailabilityModel>();
        OnWayToPl = new List<AvailabilityModel>();
        OnWayToUkr = new List<AvailabilityModel>();
        AvailableQtyUkReSale = new List<AvailabilityModel>();
    }

    public Dictionary<TypeProductAvailability, double> TotalAvailabilities { get; set; }

    public List<AvailabilityModel> InAccounts { get; set; }

    public List<AvailabilityModel> InStorageUkrVat { get; set; }

    public List<AvailabilityModel> InStorageUkrNotVat { get; set; }

    public List<AvailabilityModel> InStoragePl { get; set; }

    public List<AvailabilityModel> OnWayToPl { get; set; }

    public List<AvailabilityModel> OnWayToUkr { get; set; }

    public List<AvailabilityModel> AvailableQtyUkReSale { get; set; }

    public List<AvailabilityModel> AvailabilityInvoiceModel { get; set; }
}