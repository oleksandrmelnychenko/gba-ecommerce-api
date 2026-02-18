using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.Sales;

namespace GBA.Services.Actors.Accounting;

public sealed class ProtocolActEditInvoicetActor : ReceiveActor {
    public ProtocolActEditInvoicetActor() {
        ActorReferenceManager.Instance.Add(
            ProtocolActInvoiceActorNames.PROTOCOL_ACT_INVOICE_ACTOR,
            Context.ActorOf(Context.DI().Props<ProtocolActInvoiceActor>().WithRouter(new RoundRobinPool(10)), ProtocolActInvoiceActorNames.PROTOCOL_ACT_INVOICE_ACTOR)
        );
    }
}