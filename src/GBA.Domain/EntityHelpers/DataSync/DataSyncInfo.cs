using System;
using GBA.Domain.Entities.Synchronizations;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class DataSyncInfo {
    public DateTime Date { get; set; }

    public DataSyncOperationType Type { get; set; }
}