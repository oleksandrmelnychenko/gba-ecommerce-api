using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Charts;

public sealed class ChartsManagementActor : ReceiveActor {
    public ChartsManagementActor() {
        ActorReferenceManager.Instance.Add(
            ChartsActorNames.EXCHANGE_RATE_CHARTS_ACTOR,
            Context.ActorOf(Context.DI().Props<ExchangeRateChartsActor>().WithRouter(new RoundRobinPool(10)),
                ChartsActorNames.EXCHANGE_RATE_CHARTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ChartsActorNames.CURRENCY_TRADER_EXCHANGE_RATE_CHARTS_ACTOR,
            Context.ActorOf(Context.DI().Props<CurrencyTraderExchangeRateChartsActor>().WithRouter(new RoundRobinPool(10)),
                ChartsActorNames.CURRENCY_TRADER_EXCHANGE_RATE_CHARTS_ACTOR)
        );
    }
}