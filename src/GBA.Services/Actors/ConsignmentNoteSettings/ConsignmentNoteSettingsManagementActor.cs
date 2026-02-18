using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.ConsignmentNoteSettings.ConsignmentNoteSettingsGetActors;

namespace GBA.Services.Actors.ConsignmentNoteSettings;

public sealed class ConsignmentNoteSettingsManagementActor : ReceiveActor {
    public ConsignmentNoteSettingsManagementActor() {
        ActorReferenceManager.Instance.Add(
            ConsignmentNoteSettingsActorNames.CONSIGNMENT_NOTE_SETTINGS_ACTOR,
            Context.ActorOf(Context.DI().Props<ConsignmentNoteSettingsActor>(),
                ConsignmentNoteSettingsActorNames.CONSIGNMENT_NOTE_SETTINGS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ConsignmentNoteSettingsActorNames.BASE_CONSIGNMENT_NOTE_SETTINGS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseConsignmentNoteSettingsGetActor>().WithRouter(new RoundRobinPool(5)),
                ConsignmentNoteSettingsActorNames.BASE_CONSIGNMENT_NOTE_SETTINGS_GET_ACTOR)
        );
    }
}