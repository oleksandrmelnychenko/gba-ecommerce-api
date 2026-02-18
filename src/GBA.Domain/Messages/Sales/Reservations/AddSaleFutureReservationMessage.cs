using System;

namespace GBA.Domain.Messages.Sales.Reservations;

public sealed class AddSaleFutureReservationMessage {
    public Guid ProductNetId { get; set; }

    public Guid ClientNetId { get; set; }

    public Guid SupplyOrderNetId { get; set; }

    public DateTime RemindDate { get; set; }

    public double Count { get; set; }
}