using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.Clients.ClientsGetActors;
using GBA.Services.Actors.Clients.OrganizationClientsGetActors;
using GBA.Services.Actors.Clients.PerfectClientsGetActors;
using GBA.Services.Actors.Clients.RetailClientGetActors;

namespace GBA.Services.Actors.Clients;

public sealed class ClientsManagementActor : ReceiveActor {
    public ClientsManagementActor() {
        ActorReferenceManager.Instance.Add(
            ClientsActorNames.CLIENTS_ACTOR,
            Context.ActorOf(Context.DI().Props<ClientsActor>(), ClientsActorNames.CLIENTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.CLIENT_TYPES_ACTOR,
            Context.ActorOf(Context.DI().Props<ClientTypesActor>(), ClientsActorNames.CLIENT_TYPES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.CLIENT_TYPE_ROLES_ACTOR,
            Context.ActorOf(Context.DI().Props<ClientTypeRolesActor>(), ClientsActorNames.CLIENT_TYPE_ROLES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.PERFECT_CLIENTS_ACTOR,
            Context.ActorOf(Context.DI().Props<PerfectClientsActor>(), ClientsActorNames.PERFECT_CLIENTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.PACKING_MARKINGS_ACTOR,
            Context.ActorOf(Context.DI().Props<PackingMarkingsActor>().WithRouter(new RoundRobinPool(5)), ClientsActorNames.PACKING_MARKINGS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.CLIENT_CONTRACT_DOCUMENTS_ACTOR,
            Context.ActorOf(Context.DI().Props<ClientContractDocumentsActor>(), ClientsActorNames.CLIENT_CONTRACT_DOCUMENTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.ORGANIZATION_CLIENTS_ACTOR,
            Context.ActorOf(Context.DI().Props<OrganizationClientsActor>(), ClientsActorNames.ORGANIZATION_CLIENTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.BASE_ORGANIZATION_CLIENTS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseOrganizationClientsGetActor>().WithRouter(new RoundRobinPool(5)),
                ClientsActorNames.BASE_ORGANIZATION_CLIENTS_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.INCOTERMS_ACTOR,
            Context.ActorOf(Context.DI().Props<IncotermsActor>(), ClientsActorNames.INCOTERMS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.BASE_CLIENTS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseClientsGetActor>().WithRouter(new RoundRobinPool(10)),
                ClientsActorNames.BASE_CLIENTS_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.BASE_PERFECT_CLIENTS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BasePerfectClientsGetActor>().WithRouter(new RoundRobinPool(5)),
                ClientsActorNames.BASE_PERFECT_CLIENTS_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.RETAIL_CLIENT_ACTOR,
            Context.ActorOf(Context.DI().Props<RetailClientActor>(), ClientsActorNames.RETAIL_CLIENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.BASE_RETAIL_CLIENT_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseRetailClientGetActor>().WithRouter(new RoundRobinPool(5)),
                ClientsActorNames.BASE_RETAIL_CLIENT_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ClientsActorNames.ECOMMERCE_CLIENT_ACTOR,
            Context.ActorOf(Context.DI().Props<EcommerceClientActor>(), ClientsActorNames.ECOMMERCE_CLIENT_ACTOR));
    }
}