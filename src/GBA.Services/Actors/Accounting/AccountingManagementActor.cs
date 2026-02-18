using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Accounting;

public sealed class AccountingManagementActor : ReceiveActor {
    public AccountingManagementActor() {
        ActorReferenceManager.Instance.Add(
            AccountingActorNames.ACCOUNTING_CASH_FLOW_ACTOR,
            Context.ActorOf(Context.DI().Props<AccountingCashFlowActor>().WithRouter(new RoundRobinPool(10)), AccountingActorNames.ACCOUNTING_CASH_FLOW_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            AccountingActorNames.ACCOUNTING_PAYABLE_INFO_ACTOR,
            Context.ActorOf(Context.DI().Props<AccountingPayableInfoActor>().WithRouter(new RoundRobinPool(10)), AccountingActorNames.ACCOUNTING_PAYABLE_INFO_ACTOR)
        );
    }
}