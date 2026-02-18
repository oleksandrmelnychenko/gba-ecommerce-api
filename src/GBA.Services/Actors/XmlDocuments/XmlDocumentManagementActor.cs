using Akka.Actor;
using Akka.DI.Core;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.XmlDocuments;

public sealed class XmlDocumentManagementActor : ReceiveActor {
    public XmlDocumentManagementActor() {
        ActorReferenceManager.Instance.Add(
            XmlDocumentActorNames.XLM_DOCUMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<XmlDocumentActor>(), XmlDocumentActorNames.XLM_DOCUMENT_ACTOR)
        );
    }
}