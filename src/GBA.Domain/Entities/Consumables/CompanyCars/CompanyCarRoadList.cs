using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Entities.Consumables;

public sealed class CompanyCarRoadList : EntityBase {
    public CompanyCarRoadList() {
        CompanyCarRoadListDrivers = new HashSet<CompanyCarRoadListDriver>();
    }

    public string Comment { get; set; }

    public double FuelAmount { get; set; }

    public long Mileage { get; set; }

    public int TotalKilometers { get; set; }

    public int InCityKilometers { get; set; }

    public int OutsideCityKilometers { get; set; }

    public int MixedModeKilometers { get; set; }

    public long CompanyCarId { get; set; }

    public long ResponsibleId { get; set; }

    public long CreatedById { get; set; }

    public long? UpdatedById { get; set; }

    public long? OutcomePaymentOrderId { get; set; }

    public CompanyCar CompanyCar { get; set; }

    public User Responsible { get; set; }

    public User CreatedBy { get; set; }

    public User UpdatedBy { get; set; }

    public OutcomePaymentOrder OutcomePaymentOrder { get; set; }

    public ICollection<CompanyCarRoadListDriver> CompanyCarRoadListDrivers { get; set; }
}