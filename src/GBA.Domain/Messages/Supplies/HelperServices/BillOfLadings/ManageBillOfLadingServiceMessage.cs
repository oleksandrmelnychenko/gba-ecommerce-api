using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Messages.Supplies.HelperServices.BillOfLadings;

public sealed class ManageBillOfLadingServiceMessage {
    public ManageBillOfLadingServiceMessage(
        BillOfLadingService billOfLadingService,
        Guid netId,
        Guid userNedId) {
        BillOfLadingService = billOfLadingService;
        NetId = netId;
        UserNedId = userNedId;
    }

    public BillOfLadingService BillOfLadingService { get; }

    public Guid NetId { get; }

    public Guid UserNedId { get; }
}