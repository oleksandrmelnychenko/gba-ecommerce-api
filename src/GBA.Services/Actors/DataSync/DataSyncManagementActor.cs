using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using Akka.DI.Core;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Messages.Communications.Hubs;
using GBA.Domain.Messages.DataSync;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using Microsoft.Extensions.Localization;

namespace GBA.Services.Actors.DataSync;

public sealed class DataSyncManagementActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IStringLocalizer<SharedResource> _localizer;

    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    private string _dailySynchronizationStartedBy;

    private string _incomedOrdersSynchronizationStartedBy;

    private bool _isDailySyncInProgress;

    private bool _isIncomedOrdersSyncInProgress;

    private bool _isOutcomeOrdersSyncInProgress;

    private bool _isSyncInProgress;

    private string _outcomeOrdersSynchronizationStartedBy;

    private string _synchronizationStartedBy;

    //Main sync management actor, for handling sync state and start sync if not yet started
    public DataSyncManagementActor(
        IStringLocalizer<SharedResource> localizer,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory) {
        _localizer = localizer;

        _connectionFactory = connectionFactory;

        _userRepositoriesFactory = userRepositoriesFactory;

        //Creating global sync worker actor and concrete type specific sync workers
        ActorReferenceManager.Instance.Add(
            DataSyncActorNames.DATA_SYNC_WORKER_ACTOR,
            Context.ActorOf(Context.DI().Props<DataSyncWorkerActor>(), DataSyncActorNames.DATA_SYNC_WORKER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            DataSyncActorNames.SYNC_PRODUCTS_WORKER_ACTOR,
            Context.ActorOf(Context.DI().Props<SyncProductsWorkerActor>(), DataSyncActorNames.SYNC_PRODUCTS_WORKER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            DataSyncActorNames.SYNC_CLIENTS_WORKER_ACTOR,
            Context.ActorOf(Context.DI().Props<SyncClientsWorkerActor>(), DataSyncActorNames.SYNC_CLIENTS_WORKER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            DataSyncActorNames.SYNC_CONSIGNMENTS_WORKER_ACTOR,
            Context.ActorOf(Context.DI().Props<SyncConsignmentsWorkerActor>(), DataSyncActorNames.SYNC_CONSIGNMENTS_WORKER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            DataSyncActorNames.SYNC_ACCOUNTING_WORKER_ACTOR,
            Context.ActorOf(Context.DI().Props<SyncAccountingWorkerActor>(), DataSyncActorNames.SYNC_ACCOUNTING_WORKER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            DataSyncActorNames.SYNC_PAYMENT_REGISTERS_WORKER_ACTOR,
            Context.ActorOf(Context.DI().Props<SyncPaymentRegistersWorkerActor>(), DataSyncActorNames.SYNC_PAYMENT_REGISTERS_WORKER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            DataSyncActorNames.SYNC_INCOMED_ORDERS_WORKER,
            Context.ActorOf(Context.DI().Props<SyncIncomedOrdersWorkerActor>(), DataSyncActorNames.SYNC_INCOMED_ORDERS_WORKER)
        );

        ActorReferenceManager.Instance.Add(
            DataSyncActorNames.SYNC_OUTCOME_ORDERS_WORKER,
            Context.ActorOf(Context.DI().Props<SyncOutcomeOrdersWorkerActor>(), DataSyncActorNames.SYNC_OUTCOME_ORDERS_WORKER)
        );

        ActorReferenceManager.Instance.Add(
            DataSyncActorNames.DAILY_DATA_SYNC_WORKER,
            Context.ActorOf(Context.DI().Props<DailyDataSyncWorkerActor>(), DataSyncActorNames.DAILY_DATA_SYNC_WORKER));

        //Setting initial state
        _isSyncInProgress = false;

        _isDailySyncInProgress = false;

        _isIncomedOrdersSyncInProgress = false;

        _isOutcomeOrdersSyncInProgress = false;

        _synchronizationStartedBy = string.Empty;

        _incomedOrdersSynchronizationStartedBy = string.Empty;

        _outcomeOrdersSynchronizationStartedBy = string.Empty;

        Receive<SynchronizeDataFromOneCMessage>(ProcessSynchronizeDataFromOneCMessage);

        Receive<SynchronizationFinishedMessage>(ProcessSynchronizationFinishedMessage);

        Receive<SynchronizeIncomedOrdersMessage>(ProcessSynchronizeIncomedOrdersMessage);

        Receive<IncomedOrdersSynchronizationFinishedMessage>(ProcessIncomedOrdersSynchronizationFinishedMessage);

        Receive<SynchronizeOutcomeOrdersMessage>(ProcessSynchronizeOutcomeOrdersMessage);

        Receive<SynchronizeDailyDataMessage>(ProcessSynchronizeDailyDataMessage);

        Receive<OutcomeOrdersSynchronizationFinishedMessage>(ProcessOutcomeOrdersSynchronizationFinishedMessage);

        Receive<DailySynchronizationFinishedMessage>(ProcessDailySynchronizationFinishedMessage);
    }

    //Receive request from controller for sync start 
    private void ProcessSynchronizeDataFromOneCMessage(SynchronizeDataFromOneCMessage message) {
        if (_isSyncInProgress) {
            //If sync already started - respond to controller with localized error message
            Sender.Tell(new LocalizedException(
                    DataSyncResourceNames.SYNCHRONIZATION_ALREADY_IN_PROGRESS,
                    _synchronizationStartedBy
                )
            );
        } else {
            //Notify user about sync start via communication actor via SignalR
            ActorReferenceManager
                .Instance
                .Get(CommunicationsActorNames.HUBS_SENDER_ACTOR)
                .Tell(new PushDataSyncNotificationMessage(
                    DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_STARTED]
                ));

            if (!message.SyncEntityTypes.Any()) Sender.Tell(new Exception(DataSyncResourceNames.NO_TYPES_PROVIDED));

            //Setting state props for future sync requests
            _isSyncInProgress = true;

            using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                _synchronizationStartedBy = $"<{user?.LastName ?? string.Empty} {user?.FirstName ?? string.Empty}>";
            }

            //Respond to controller
            Sender.Tell(DataSyncResourceNames.SYNCHRONIZATION_STARTED);

            //Pass sync start message to global sync worker
            ActorReferenceManager
                .Instance
                .Get(DataSyncActorNames.DATA_SYNC_WORKER_ACTOR)
                .Tell(new StartDataSyncWorkMessage(message.SyncEntityTypes.Distinct().ToList(), message.UserNetId, message.ForAmg));
        }
    }

    //Process finished sync message from global sync worker, re-setting initial state
    private void ProcessSynchronizationFinishedMessage(SynchronizationFinishedMessage message) {
        _synchronizationStartedBy = string.Empty;

        _isSyncInProgress = false;

        ActorReferenceManager
            .Instance
            .Get(CommunicationsActorNames.HUBS_SENDER_ACTOR)
            .Tell(new PushDataSyncNotificationMessage(
                DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_FINISHED],
                true
            ));
    }

    //Receive request from controller for start incomed orders sync
    private void ProcessSynchronizeIncomedOrdersMessage(SynchronizeIncomedOrdersMessage message) {
        if (_isIncomedOrdersSyncInProgress) {
            //If sync already started - respond to controller with localized error message
            Sender.Tell(new LocalizedException(
                    DataSyncResourceNames.SYNCHRONIZATION_ALREADY_IN_PROGRESS,
                    _incomedOrdersSynchronizationStartedBy
                )
            );
        } else {
            //Notify user about sync start via communication actor via SignalR
            ActorReferenceManager
                .Instance
                .Get(CommunicationsActorNames.HUBS_SENDER_ACTOR)
                .Tell(new PushDataSyncNotificationMessage(
                    DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_STARTED]
                ));

            //Setting state props for future sync requests
            _isIncomedOrdersSyncInProgress = true;

            using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                _incomedOrdersSynchronizationStartedBy = $"<{user?.LastName ?? string.Empty} {user?.FirstName ?? string.Empty}>";
            }

            //Respond to controller
            Sender.Tell(DataSyncResourceNames.SYNCHRONIZATION_STARTED);

            //Pass sync start message to concrete sync worker
            ActorReferenceManager
                .Instance
                .Get(DataSyncActorNames.SYNC_INCOMED_ORDERS_WORKER)
                .Tell(message);
        }
    }

    //Process finished incomed orders sync message
    private void ProcessIncomedOrdersSynchronizationFinishedMessage(IncomedOrdersSynchronizationFinishedMessage message) {
        _incomedOrdersSynchronizationStartedBy = string.Empty;

        _isIncomedOrdersSyncInProgress = false;

        ActorReferenceManager
            .Instance
            .Get(CommunicationsActorNames.HUBS_SENDER_ACTOR)
            .Tell(new PushDataSyncNotificationMessage(
                DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_FINISHED],
                true
            ));
    }

    //Receive request from controller for start incomed orders sync
    private void ProcessSynchronizeOutcomeOrdersMessage(SynchronizeOutcomeOrdersMessage message) {
        if (_isOutcomeOrdersSyncInProgress) {
            //If sync already started - respond to controller with localized error message
            Sender.Tell(new LocalizedException(
                    DataSyncResourceNames.SYNCHRONIZATION_ALREADY_IN_PROGRESS,
                    _outcomeOrdersSynchronizationStartedBy
                )
            );
        } else {
            //Notify user about sync start via communication actor via SignalR
            ActorReferenceManager
                .Instance
                .Get(CommunicationsActorNames.HUBS_SENDER_ACTOR)
                .Tell(new PushDataSyncNotificationMessage(
                    DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_STARTED]
                ));

            //Setting state props for future sync requests
            _isOutcomeOrdersSyncInProgress = true;

            using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                _outcomeOrdersSynchronizationStartedBy = $"<{user?.LastName ?? string.Empty} {user?.FirstName ?? string.Empty}>";
            }

            //Respond to controller
            Sender.Tell(DataSyncResourceNames.SYNCHRONIZATION_STARTED);

            //Pass sync start message to concrete sync worker
            ActorReferenceManager
                .Instance
                .Get(DataSyncActorNames.SYNC_OUTCOME_ORDERS_WORKER)
                .Tell(message);
        }
    }

    private void ProcessSynchronizeDailyDataMessage(SynchronizeDailyDataMessage message) {
        if (_isDailySyncInProgress) {
            //If sync already started - respond to controller with localized error message
            Sender.Tell(new LocalizedException(
                    DataSyncResourceNames.SYNCHRONIZATION_ALREADY_IN_PROGRESS,
                    _dailySynchronizationStartedBy
                )
            );
        } else {
            //Notify user about sync start via communication actor via SignalR
            ActorReferenceManager
                .Instance
                .Get(CommunicationsActorNames.HUBS_SENDER_ACTOR)
                .Tell(new PushDataSyncNotificationMessage(
                    DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_STARTED]
                ));

            //Setting state props for future sync requests
            _isDailySyncInProgress = true;

            using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                _dailySynchronizationStartedBy = $"<{user?.LastName ?? string.Empty} {user?.FirstName ?? string.Empty}>";
            }

            //Respond to controller
            Sender.Tell(DataSyncResourceNames.SYNCHRONIZATION_STARTED);

            //Pass sync start message to concrete sync worker
            ActorReferenceManager
                .Instance
                .Get(DataSyncActorNames.DAILY_DATA_SYNC_WORKER)
                .Tell(message);
        }
    }

    private void ProcessDailySynchronizationFinishedMessage(DailySynchronizationFinishedMessage message) {
        _isDailySyncInProgress = false;

        ActorReferenceManager
            .Instance
            .Get(CommunicationsActorNames.HUBS_SENDER_ACTOR)
            .Tell(new PushDataSyncNotificationMessage(
                DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_FINISHED],
                true
            ));
    }

    //Process finished incomed orders sync message
    private void ProcessOutcomeOrdersSynchronizationFinishedMessage(OutcomeOrdersSynchronizationFinishedMessage message) {
        _outcomeOrdersSynchronizationStartedBy = string.Empty;

        _isOutcomeOrdersSyncInProgress = false;

        ActorReferenceManager
            .Instance
            .Get(CommunicationsActorNames.HUBS_SENDER_ACTOR)
            .Tell(new PushDataSyncNotificationMessage(
                DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_FINISHED],
                true
            ));
    }
}