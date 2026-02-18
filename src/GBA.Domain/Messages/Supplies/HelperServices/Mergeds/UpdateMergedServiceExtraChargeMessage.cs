using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.Mergeds;

public sealed class UpdateMergedServiceExtraChargeMessage {
    public UpdateMergedServiceExtraChargeMessage(
        bool isAuto,
        Guid serviceNetId,
        SupplyExtraChargeType typeExtraCharge,
        ICollection<SupplyInvoiceMergedService> invoices,
        Guid userNetId) {
        IsAuto = isAuto;
        ServiceNetId = serviceNetId;
        TypeExtraCharge = typeExtraCharge;
        Invoices = invoices;
        UserNetId = userNetId;
    }

    public bool IsAuto { get; }
    public Guid ServiceNetId { get; }
    public SupplyExtraChargeType TypeExtraCharge { get; }
    public ICollection<SupplyInvoiceMergedService> Invoices { get; }
    public Guid UserNetId { get; }
}