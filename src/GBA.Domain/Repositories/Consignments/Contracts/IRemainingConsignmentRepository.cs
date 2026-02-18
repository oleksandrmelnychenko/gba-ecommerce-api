using System;
using System.Collections.Generic;
using GBA.Domain.EntityHelpers.Consignments;

namespace GBA.Domain.Repositories.Consignments.Contracts;

public interface IRemainingConsignmentRepository {
    IEnumerable<RemainingConsignment> GetAllByProductNetId(Guid productNetId);

    IEnumerable<RemainingConsignment> GetAllByStorageNetId(Guid storageNetId);

    IEnumerable<RemainingConsignment> GetAllByProductIncomeNetId(Guid productIncomeNetId);

    IEnumerable<AvailableConsignment> GetAllAvailableConsignmentsForSupplyReturnByProductAndStorageNetIds(Guid productNetId, Guid storageNetId);

    IEnumerable<RemainingConsignment> GetAllByStorageNetIdFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to,
        string searchValue,
        int limit,
        int offset);

    Tuple<decimal, decimal, decimal, decimal, double> GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetId(Guid? storageNetId);

    Tuple<decimal, decimal, decimal, decimal, double> GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetIdFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to,
        string searchValue);

    Tuple<decimal, decimal, decimal, decimal, double> GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetIdFiltered(
        Guid? storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to);

    IEnumerable<GroupedConsignment> GetGroupedByStorageNetIdFiltered(
        Guid? storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to,
        int limit,
        int offset);

    List<RemainingConsignment> GetAllByStorageForDocumentExport(
        Guid storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to,
        string searchValue);

    List<GroupedConsignment> GetGroupedByStorageForDocumentExport(
        Guid? storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to);
}