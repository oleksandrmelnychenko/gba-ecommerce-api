using System.Collections.Generic;

namespace GBA.Domain.Entities;

public sealed class TaxInspection : EntityBase {
    public TaxInspection() {
        Organizations = new HashSet<Organization>();
    }

    public string InspectionNumber { get; set; }

    public string InspectionType { get; set; }

    public string InspectionName { get; set; }

    public string InspectionRegionName { get; set; }

    public string InspectionRegionCode { get; set; }

    public string InspectionAddress { get; set; }

    public string InspectionUSREOU { get; set; }

    public ICollection<Organization> Organizations { get; set; }
}