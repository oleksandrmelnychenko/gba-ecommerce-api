using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.EntityHelpers.Consignments;

namespace GBA.Domain.Repositories.Consignments.Contracts;

public interface IConsignmentInfoRepository {
    IEnumerable<IncomeConsignmentInfo> GetIncomeConsignmentInfoFiltered(
        Guid productNetId,
        DateTime from,
        DateTime to);

    IEnumerable<OutcomeConsignmentInfo> GetOutcomeConsignmentInfoFiltered(
        Guid productNetId,
        DateTime from,
        DateTime to);

    IEnumerable<MovementConsignmentInfo> GetMovementConsignmentInfoFiltered(
        IEnumerable<ConsignmentItemMovementType> types,
        Guid productNetId,
        DateTime from,
        DateTime to,
        ConsignmentMovementType movementType);

    IEnumerable<InfoIncome> GetInfoIcomesFiltered(
        Guid NetId);

    ConsignmentItem GetInfoIcomesFiltered(long id);

    IEnumerable<MovementConsignmentInfo> GetFullMovementConsignmentInfoByConsignmentItemNetId(
        Guid consignmentItemNetId,
        DateTime from,
        DateTime to);

    IEnumerable<ClientMovementConsignmentInfo> GetClientMovementConsignmentInfoFiltered(
        Guid clientNetId,
        DateTime from,
        DateTime to,
        int limit,
        int offset,
        long[] organizationIds,
        string article);

    IEnumerable<ClientMovementConsignmentInfo> GetClientMovementConsignmentInfoFilteredFoxDocumentExport(
        Guid clientNetId,
        DateTime from,
        DateTime to);


    Tuple<IEnumerable<ConsignmentAvailabilityItem>, int> GetConsignmentAvailabilityFiltered(
        Guid storageNetId,
        DateTime from,
        DateTime to,
        string vendorCode,
        int limit,
        int offset);

    IEnumerable<ConsignmentAvailabilityItem> GetAllConsignmentAvailabilityFiltered(
        Guid storageNetId,
        DateTime from,
        DateTime to,
        string vendorCode);
}