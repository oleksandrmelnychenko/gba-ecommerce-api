using Akka.Actor;
using Akka.DI.Core;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Translations;

public sealed class TranslationManagementActor : ReceiveActor {
    public TranslationManagementActor() {
        ActorReferenceManager.Instance.Add(
            TranslationActorNames.USER_PROFILE_ROLE_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<UserProfileRoleTranslationsActor>(), TranslationActorNames.USER_PROFILE_ROLE_TRANSALTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TranslationActorNames.CLIENT_TYPE_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<ClientTypeTranslationsActor>(), TranslationActorNames.CLIENT_TYPE_TRANSALTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TranslationActorNames.CALCULATION_TYPE_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<CalculationTypeTranslationsActor>(), TranslationActorNames.CALCULATION_TYPE_TRANSALTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TranslationActorNames.AGREEMENT_TYPE_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<AgreementTypeTranslationsActor>(), TranslationActorNames.AGREEMENT_TYPE_TRANSALTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TranslationActorNames.CLIENT_TYPE_ROLE_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<ClientTypeRoleTranslationsActor>(), TranslationActorNames.CLIENT_TYPE_ROLE_TRANSALTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TranslationActorNames.PRICE_TYPE_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<PriceTypeTranslationsActor>(), TranslationActorNames.PRICE_TYPE_TRANSALTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TranslationActorNames.PERFECT_CLIENT_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<PerfectClientTranslationsActor>(), TranslationActorNames.PERFECT_CLIENT_TRANSALTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TranslationActorNames.PERFECT_CLIENT_VALUES_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<PerfectClientValuesTranslationsActor>(), TranslationActorNames.PERFECT_CLIENT_VALUES_TRANSALTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TranslationActorNames.ORGANIZATION_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<OrganizationTranslationsActor>(), TranslationActorNames.ORGANIZATION_TRANSALTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TranslationActorNames.PRICING_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<PricingTranslationsActor>(), TranslationActorNames.PRICING_TRANSALTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TranslationActorNames.MEASURE_UNIT_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<MeasureUnitTranslationsActor>(), TranslationActorNames.MEASURE_UNIT_TRANSALTIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TranslationActorNames.TRANSPORTER_TYPE_TRANSALTIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<TransporterTypeTranslationsActor>(), TranslationActorNames.TRANSPORTER_TYPE_TRANSALTIONS_ACTOR)
        );
    }
}