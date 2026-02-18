using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.Agreements.AgreementsGetActors;

namespace GBA.Services.Actors.Agreements;

public sealed class AgreementsManagementActor : ReceiveActor {
    public AgreementsManagementActor() {
        ActorReferenceManager.Instance.Add(
            AgreementsActorNames.AGREEMENTS_ACTOR,
            Context.ActorOf(Context.DI().Props<AgreementsActor>(), AgreementsActorNames.AGREEMENTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            AgreementsActorNames.AGREEMENT_TYPES_ACTOR,
            Context.ActorOf(Context.DI().Props<AgreementTypesActor>(), AgreementsActorNames.AGREEMENT_TYPES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            AgreementsActorNames.CALCULATION_TYPES_ACTOR,
            Context.ActorOf(Context.DI().Props<CalculationTypesActor>(), AgreementsActorNames.CALCULATION_TYPES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            AgreementsActorNames.BASE_AGREEMENTS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseAgreementsGetActor>().WithRouter(new RoundRobinPool(10)), AgreementsActorNames.BASE_AGREEMENTS_GET_ACTOR)
        );
    }
}