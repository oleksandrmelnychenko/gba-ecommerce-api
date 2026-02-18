using System;
using GBA.Domain.EntityHelpers.Supplies;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class AddOrUpdateSupplyOrderUkraineAfterPreviewValidationMessage {
    public AddOrUpdateSupplyOrderUkraineAfterPreviewValidationMessage(
        UkraineOrderValidation ukraineOrderValidation,
        Guid userNetId
    ) {
        UkraineOrderValidation = ukraineOrderValidation;

        UserNetId = userNetId;
    }

    public UkraineOrderValidation UkraineOrderValidation { get; }

    public Guid UserNetId { get; }
}