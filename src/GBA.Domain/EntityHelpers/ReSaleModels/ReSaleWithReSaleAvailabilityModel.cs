using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class ReSaleWithReSaleAvailabilityModel {
    public string Comment { get; set; }

    public long FromStorageId { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public Organization Organization { get; set; }

    public List<ReSaleAvailabilityItemModel> ReSaleAvailabilityModels { get; set; }
}