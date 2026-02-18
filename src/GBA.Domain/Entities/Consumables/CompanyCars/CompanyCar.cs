using System.Collections.Generic;

namespace GBA.Domain.Entities.Consumables;

public sealed class CompanyCar : EntityBase {
    public CompanyCar() {
        CompanyCarRoadLists = new HashSet<CompanyCarRoadList>();

        CompanyCarFuelings = new HashSet<CompanyCarFueling>();
    }

    public string LicensePlate { get; set; }

    public string CarBrand { get; set; }

    public double TankCapacity { get; set; }

    public double FuelAmount { get; set; }

    public double InCityConsumption { get; set; }

    public double OutsideCityConsumption { get; set; }

    public double MixedModeConsumption { get; set; }

    public long InitialMileage { get; set; }

    public long Mileage { get; set; }

    public long CreatedById { get; set; }

    public long? UpdatedById { get; set; }

    public long ConsumablesStorageId { get; set; }

    public long OrganizationId { get; set; }

    public User CreatedBy { get; set; }

    public User UpdatedBy { get; set; }

    public ConsumablesStorage ConsumablesStorage { get; set; }

    public Organization Organization { get; set; }

    public ICollection<CompanyCarRoadList> CompanyCarRoadLists { get; set; }

    public ICollection<CompanyCarFueling> CompanyCarFuelings { get; set; }
}