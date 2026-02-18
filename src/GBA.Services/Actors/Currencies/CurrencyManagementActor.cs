using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.Currencies.CurrenciesGetActors;

namespace GBA.Services.Actors.Currencies;

public sealed class CurrencyManagementActor : ReceiveActor {
    public CurrencyManagementActor() {
        ActorReferenceManager.Instance.Add(
            CurrencyActorNames.CURRENCIES_ACTOR,
            Context.ActorOf(Context.DI().Props<CurrenciesActor>(), CurrencyActorNames.CURRENCIES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            CurrencyActorNames.CURRENCY_TRADERS_ACTOR,
            Context.ActorOf(Context.DI().Props<CurrencyTradersActor>(), CurrencyActorNames.CURRENCY_TRADERS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            CurrencyActorNames.BASE_CURRENCIES_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseCurrenciesGetActor>().WithRouter(new RoundRobinPool(10)), CurrencyActorNames.BASE_CURRENCIES_GET_ACTOR)
        );
    }
}