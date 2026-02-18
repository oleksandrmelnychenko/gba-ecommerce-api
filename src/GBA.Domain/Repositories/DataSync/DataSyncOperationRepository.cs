using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Synchronizations;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Repositories.DataSync.Contracts;

namespace GBA.Domain.Repositories.DataSync;

public sealed class DataSyncOperationRepository : IDataSyncOperationRepository {
    private readonly IDbConnection _connection;

    public DataSyncOperationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(DataSyncOperation dataSyncOperation) {
        _connection.Execute(
            "INSERT INTO [DataSyncOperation] " +
            "([OperationType], [UserID], [Updated], [ForAmg]) " +
            "VALUES " +
            "(@OperationType, @UserId, GETUTCDATE(), @ForAmg)",
            dataSyncOperation
        );
    }

    public void AddWithSpecificDates(DataSyncOperation dataSyncOperation) {
        _connection.Execute(
            "INSERT INTO [DataSyncOperation] " +
            "([OperationType], [UserID], [Created], [Updated], [ForAmg]) " +
            "VALUES " +
            "(@OperationType, @UserId, @Created, @Updated, @ForAmg)",
            dataSyncOperation
        );
    }

    public DataSyncOperation GetLastRecordByOperationType(DataSyncOperationType operationType) {
        return _connection.Query<DataSyncOperation>(
            "SELECT TOP(1) * " +
            "FROM [DataSyncOperation] " +
            "WHERE [DataSyncOperation].OperationType = @OperationType " +
            "ORDER BY [DataSyncOperation].[Created] DESC",
            new { OperationType = operationType }
        ).SingleOrDefault();
    }

    public DataSyncOperation GetLastRecordByOperationType(DataSyncOperationType operationType, DataSyncOperationType additionalOperationType) {
        return _connection.Query<DataSyncOperation>(
            "SELECT TOP(1) * " +
            "FROM [DataSyncOperation] " +
            "WHERE [DataSyncOperation].OperationType = @OperationType " +
            "OR " +
            "[DataSyncOperation].OperationType = @AdditionalOperationType " +
            "ORDER BY [DataSyncOperation].[Created] DESC",
            new { OperationType = operationType, AdditionalOperationType = additionalOperationType }
        ).SingleOrDefault();
    }

    public IEnumerable<DataSyncInfo> GetLastDataSyncInfo(
        bool forAmg,
        DateTime from,
        DateTime to) {
        return _connection.Query<DataSyncInfo>(
            "SELECT " +
            "[DataSyncOperation].[Created] [Date] " +
            ", [DataSyncOperation].[OperationType] [Type] " +
            "FROM [DataSyncOperation] " +
            "WHERE [DataSyncOperation].[ForAmg] = @ForAmg " +
            "AND [DataSyncOperation].[Created] >= @From " +
            "AND [DataSyncOperation].[Created] <= @To " +
            "ORDER BY [Created] DESC ",
            new { ForAmg = forAmg, From = from, To = to }).ToList();
    }
}