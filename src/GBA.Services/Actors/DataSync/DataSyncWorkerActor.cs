using System.Linq;
using Akka.Actor;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Messages.DataSync;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.DataSync;

public sealed class DataSyncWorkerActor : ReceiveActor {
    public DataSyncWorkerActor() {
        Receive<StartDataSyncWorkMessage>(ProcessStartDataSyncWorkMessage);
    }

    //Process step by step sync, depending on selected sync entity types
    private static void ProcessStartDataSyncWorkMessage(StartDataSyncWorkMessage message) {
        if (!message.SyncEntityTypes.Any()) {
            ActorReferenceManager.Instance.Get(BaseActorNames.DATA_SYNC_MANAGEMENT_ACTOR).Tell(new SynchronizationFinishedMessage());

            return;
        }

        SyncEntityType syncEntityType = message.SyncEntityTypes.First();

        switch (syncEntityType) {
            case SyncEntityType.Products:
                ActorReferenceManager
                    .Instance
                    .Get(DataSyncActorNames.SYNC_PRODUCTS_WORKER_ACTOR)
                    .Tell(new SynchronizeProductsMessage(message.SyncEntityTypes.Where(t => !t.Equals(syncEntityType)).ToList(), message.UserNetId, message.ForAmg));

                break;
            case SyncEntityType.Clients:
                ActorReferenceManager
                    .Instance
                    .Get(DataSyncActorNames.SYNC_CLIENTS_WORKER_ACTOR)
                    .Tell(new SynchronizeClientsMessage(message.SyncEntityTypes.Where(t => !t.Equals(syncEntityType)).ToList(), message.UserNetId, message.ForAmg));

                break;
            case SyncEntityType.Consignments:
                ActorReferenceManager
                    .Instance
                    .Get(DataSyncActorNames.SYNC_CONSIGNMENTS_WORKER_ACTOR)
                    .Tell(new SynchronizeConsignmentsMessage(message.SyncEntityTypes.Where(t => !t.Equals(syncEntityType)).ToList(), message.UserNetId, message.ForAmg));

                break;
            case SyncEntityType.Accounting:
                ActorReferenceManager
                    .Instance
                    .Get(DataSyncActorNames.SYNC_ACCOUNTING_WORKER_ACTOR)
                    .Tell(new SynchronizeAccountingMessage(message.SyncEntityTypes.Where(t => !t.Equals(syncEntityType)).ToList(), message.UserNetId, message.ForAmg));

                break;
            case SyncEntityType.PaymentRegisters:
                ActorReferenceManager
                    .Instance
                    .Get(DataSyncActorNames.SYNC_PAYMENT_REGISTERS_WORKER_ACTOR)
                    .Tell(new SynchronizePaymentRegistersMessage(message.SyncEntityTypes.Where(t => !t.Equals(syncEntityType)).ToList(), message.UserNetId, message.ForAmg));

                break;
            default:
                ActorReferenceManager
                    .Instance
                    .Get(BaseActorNames.DATA_SYNC_MANAGEMENT_ACTOR)
                    .Tell(new SynchronizationFinishedMessage());

                break;
        }
    }
}