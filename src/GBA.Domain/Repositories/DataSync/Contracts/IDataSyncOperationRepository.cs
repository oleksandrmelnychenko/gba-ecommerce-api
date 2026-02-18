using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Synchronizations;
using GBA.Domain.EntityHelpers.DataSync;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IDataSyncOperationRepository {
    void Add(DataSyncOperation dataSyncOperation);

    void AddWithSpecificDates(DataSyncOperation dataSyncOperation);

    DataSyncOperation GetLastRecordByOperationType(DataSyncOperationType operationType);

    DataSyncOperation GetLastRecordByOperationType(DataSyncOperationType operationType, DataSyncOperationType additionalOperationType);

    IEnumerable<DataSyncInfo> GetLastDataSyncInfo(
        bool forAmg,
        DateTime from,
        DateTime to);
}