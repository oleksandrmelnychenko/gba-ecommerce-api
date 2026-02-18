using System;
using GBA.Domain.Entities;

namespace GBA.Domain.Messages.OriginalNumbers;

public sealed class AddOriginalNumberMessage {
    public AddOriginalNumberMessage(
        OriginalNumber originalNumber,
        Guid productNetId,
        bool isMain) {
        OriginalNumber = originalNumber;
        ProductNetId = productNetId;
        IsMain = isMain;
    }

    public OriginalNumber OriginalNumber { get; }

    public Guid ProductNetId { get; }

    public bool IsMain { get; }
}