using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncAccounting {
    public string Number { get; set; }

    public DateTime Date { get; set; }

    public decimal Value { get; set; }
}