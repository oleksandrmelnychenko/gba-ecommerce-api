using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.BillOfLadings;

public sealed class UpdateBillOfLadingServiceExtraChargeMessage {
    public UpdateBillOfLadingServiceExtraChargeMessage(
        bool isAuto,
        Guid serviceNetId,
        SupplyExtraChargeType typeExtraCharge,
        ICollection<SupplyInvoiceBillOfLadingService> invoices,
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
    public ICollection<SupplyInvoiceBillOfLadingService> Invoices { get; }

    public Guid UserNetId { get; }
}