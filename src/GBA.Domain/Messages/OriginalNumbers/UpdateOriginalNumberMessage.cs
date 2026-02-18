using System;
using GBA.Domain.Entities;

namespace GBA.Domain.Messages.OriginalNumbers;

public sealed class UpdateOriginalNumberMessage {
    public UpdateOriginalNumberMessage(
        OriginalNumber originalNumber,
        Guid productNetId,
        bool isMain) {
        OriginalNumber = originalNumber;
        ProductNetId = productNetId;
        IsMain = isMain;
    }

    public OriginalNumber OriginalNumber { get; set; }

    public Guid ProductNetId { get; }

    public bool IsMain { get; }
}