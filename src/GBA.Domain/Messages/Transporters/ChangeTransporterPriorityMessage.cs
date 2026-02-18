using System;

namespace GBA.Domain.Messages.Transporters;

public sealed class ChangeTransporterPriorityMessage {
    public ChangeTransporterPriorityMessage(Guid increaseTo, Guid? decreaseTo = null) {
        IncreaseTo = increaseTo;

        DecreaseTo = decreaseTo;
    }

    public Guid IncreaseTo { get; set; }

    public Guid? DecreaseTo { get; set; }
}