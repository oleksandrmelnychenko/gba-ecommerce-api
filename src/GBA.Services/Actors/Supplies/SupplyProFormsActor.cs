using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Messages.Supplies;
using GBA.Domain.Messages.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Supplies;

public sealed class SupplyProFormsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public SupplyProFormsActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<AddOrUpdateSupplyProFormMessage>(ProcessAddOrUpdateSupplyProFormMessage);

        Receive<GetSupplyProFormByNetIdMessage>(ProcessGetSupplyProFormByNetIdMessage);

        Receive<UpdateSupplyProFormMessage>(ProcessUpdateSupplyProFormMessage);

        Receive<DeleteProFormDocumentMessage>(ProcessDeleteProFormDocumentMessage);
    }

    private void ProcessAddOrUpdateSupplyProFormMessage(AddOrUpdateSupplyProFormMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
        ISupplyProFormRepository supplyProFormRepository = _supplyRepositoriesFactory.NewSupplyProFormRepository(connection);
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
        ISupplyInformationDeliveryProtocolRepository supplyInformationDeliveryProtocolRepository =
            _supplyRepositoriesFactory.NewSupplyInformationDeliveryProtocolRepository(connection);
        ISupplyInformationDeliveryProtocolKeyRepository supplyInformationDeliveryProtocolKeyRepository =
            _supplyRepositoriesFactory.NewSupplyInformationDeliveryProtocolKeyRepository(connection);
        ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);

        SupplyOrder supplyOrder = supplyOrderRepository.GetByNetIdIfExist(message.SupplyOrderNetId);

        if (supplyOrder != null) {
            if (message.SupplyProForm.IsSkipped.Equals(true)) {
                if (message.SupplyProForm.IsNew()) {
                    supplyOrder.SupplyProFormId = supplyProFormRepository.Add(message.SupplyProForm);

                    Sender.Tell(new Tuple<SupplyProForm, List<SupplyPaymentTask>>(supplyProFormRepository.GetById((long)supplyOrder.SupplyProFormId), null));

                    supplyOrderRepository.Update(supplyOrder);
                } else {
                    supplyProFormRepository.Update(message.SupplyProForm);

                    Sender.Tell(new Tuple<SupplyProForm, List<SupplyPaymentTask>>(supplyProFormRepository.GetByNetId(message.SupplyProForm.NetUid), null));
                }
            } else {
                List<long> newPaymentTaskIds = new();

                User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

                long supplyProFormId;

                if (message.SupplyProForm.IsNew()) {
                    supplyProFormId = supplyProFormRepository.Add(message.SupplyProForm);

                    supplyOrder.SupplyProFormId = supplyProFormId;

                    supplyInformationDeliveryProtocolRepository.Add(new SupplyInformationDeliveryProtocol {
                        UserId = user.Id,
                        SupplyOrderId = supplyOrder.Id,
                        SupplyProFormId = supplyProFormId,
                        IsDefault = true,
                        SupplyInformationDeliveryProtocolKeyId = supplyInformationDeliveryProtocolKeyRepository.Add(new SupplyInformationDeliveryProtocolKey {
                            Key = "����� ������������"
                        })
                    });

                    supplyOrderRepository.Update(supplyOrder);
                } else {
                    supplyProFormRepository.Update(message.SupplyProForm);

                    supplyProFormId = message.SupplyProForm.Id;
                }

                if (message.SupplyProForm.ProFormDocuments.Any()) {
                    List<ProFormDocument> proFormDocumentsToAdd = new();

                    foreach (ProFormDocument proFormDocument in message.SupplyProForm.ProFormDocuments)
                        if (proFormDocument.IsNew()) {
                            proFormDocument.SupplyProFormId = supplyProFormId;
                            proFormDocumentsToAdd.Add(proFormDocument);
                        }

                    if (proFormDocumentsToAdd.Any()) _supplyRepositoriesFactory.NewProFormDocumentRepository(connection).Add(proFormDocumentsToAdd);
                }

                if (message.SupplyProForm.PaymentDeliveryProtocols.Any())
                    AddOrUpdatePaymentDeliveryProtocolsOnUpdateOrder(
                        message.SupplyProForm.PaymentDeliveryProtocols,
                        newPaymentTaskIds,
                        _supplyRepositoriesFactory.NewSupplyOrderPaymentDeliveryProtocolRepository(connection),
                        _supplyRepositoriesFactory.NewSupplyOrderPaymentDeliveryProtocolKeyRepository(connection),
                        supplyPaymentTaskRepository
                    );

                if (message.SupplyProForm.InformationDeliveryProtocols.Any()) {
                    List<SupplyInformationDeliveryProtocol> informationDeliveryProtocolsToAdd = new();
                    List<SupplyInformationDeliveryProtocol> informationDeliveryProtocolsToUpdate = new();

                    foreach (SupplyInformationDeliveryProtocol protocol in message.SupplyProForm.InformationDeliveryProtocols)
                        if (protocol.IsNew()) {
                            protocol.SupplyProFormId = supplyProFormId;

                            if (protocol.User != null) protocol.UserId = user.Id;

                            if (protocol.SupplyInformationDeliveryProtocolKey != null)
                                protocol.SupplyInformationDeliveryProtocolKeyId = supplyInformationDeliveryProtocolKeyRepository
                                    .Add(protocol.SupplyInformationDeliveryProtocolKey);

                            informationDeliveryProtocolsToAdd.Add(protocol);
                        } else {
                            informationDeliveryProtocolsToUpdate.Add(protocol);
                        }

                    if (informationDeliveryProtocolsToAdd.Any()) supplyInformationDeliveryProtocolRepository.Add(informationDeliveryProtocolsToAdd);

                    if (informationDeliveryProtocolsToUpdate.Any()) supplyInformationDeliveryProtocolRepository.Update(informationDeliveryProtocolsToUpdate);
                }

                SupplyProForm supplyProForm = supplyProFormRepository.GetByIdWithAllInclueds(supplyProFormId);

                List<SupplyPaymentTask> addedTasks = supplyPaymentTaskRepository.GetAllByIds(newPaymentTaskIds);

                Sender.Tell(new Tuple<SupplyProForm, List<SupplyPaymentTask>>(supplyProForm, addedTasks));
            }
        } else {
            Sender.Tell(new Tuple<SupplyProForm, List<SupplyPaymentTask>>(null, null));
        }
    }

    private void ProcessGetSupplyProFormByNetIdMessage(GetSupplyProFormByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyProFormRepository(connection)
            .GetByNetId(message.NetId)
        );
    }

    private void ProcessUpdateSupplyProFormMessage(UpdateSupplyProFormMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProFormDocumentRepository proFormDocumentRepository = _supplyRepositoriesFactory.NewProFormDocumentRepository(connection);

        if (message.SupplyProForm.ProFormDocuments.Any(d => !d.IsNew()))
            proFormDocumentRepository.Update(message.SupplyProForm.ProFormDocuments.Where(d => !d.IsNew()));

        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyOrderRepository(connection)
            .GetByNetId(message.SupplyOrderNetId)
        );
    }

    private void ProcessDeleteProFormDocumentMessage(DeleteProFormDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _supplyRepositoriesFactory
            .NewProFormDocumentRepository(connection)
            .Remove(message.NetId);
    }

    private static void AddOrUpdatePaymentDeliveryProtocolsOnUpdateOrder(
        IEnumerable<SupplyOrderPaymentDeliveryProtocol> paymentDeliveryProtocols,
        ICollection<long> newPaymentTaskIds,
        ISupplyOrderPaymentDeliveryProtocolRepository supplyOrderPaymentDeliveryProtocolRepository,
        ISupplyOrderPaymentDeliveryProtocolKeyRepository supplyOrderPaymentDeliveryProtocolKeyRepository,
        ISupplyPaymentTaskRepository supplyPaymentTaskRepository) {
        List<SupplyOrderPaymentDeliveryProtocol> paymentDeliveryProtocolsToUpdate = new();
        List<SupplyOrderPaymentDeliveryProtocol> paymentDeliveryProtocolsToAdd = new();

        List<SupplyPaymentTask> supplyPaymentTasksToUpdate = new();
        List<SupplyOrderPaymentDeliveryProtocolKey> supplyOrderPaymentDeliveryProtocolKeysToUpdate = new();

        foreach (SupplyOrderPaymentDeliveryProtocol paymentDeliveryProtocol in paymentDeliveryProtocols) {
            if (paymentDeliveryProtocol.User != null) paymentDeliveryProtocol.UserId = paymentDeliveryProtocol.User.Id;

            if (paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey != null) {
                if (paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey.IsNew()) {
                    paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKeyId = supplyOrderPaymentDeliveryProtocolKeyRepository
                        .Add(paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey);
                } else {
                    paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKeyId = paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey.Id;
                    supplyOrderPaymentDeliveryProtocolKeysToUpdate.Add(paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey);
                }
            }

            if (paymentDeliveryProtocol.SupplyPaymentTask != null) {
                if (paymentDeliveryProtocol.SupplyPaymentTask.User != null)
                    paymentDeliveryProtocol.SupplyPaymentTask.UserId = paymentDeliveryProtocol.SupplyPaymentTask.User.Id;

                if (paymentDeliveryProtocol.SupplyPaymentTask.IsNew()) {
                    paymentDeliveryProtocol.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PaymentDeliveryProtocol;
                    paymentDeliveryProtocol.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;

                    paymentDeliveryProtocol.SupplyPaymentTask.PayToDate = !paymentDeliveryProtocol.SupplyPaymentTask.PayToDate.HasValue
                        ? DateTime.UtcNow.Date
                        : paymentDeliveryProtocol.SupplyPaymentTask.PayToDate.Value.Date;

                    paymentDeliveryProtocol.SupplyPaymentTask.NetPrice = paymentDeliveryProtocol.Value;
                    paymentDeliveryProtocol.SupplyPaymentTask.GrossPrice = paymentDeliveryProtocol.Value;

                    long paymentTaskId = supplyPaymentTaskRepository
                        .Add(paymentDeliveryProtocol.SupplyPaymentTask);

                    newPaymentTaskIds.Add(paymentTaskId);

                    paymentDeliveryProtocol.SupplyPaymentTaskId = paymentTaskId;
                } else {
                    //TODO: if status changed - store updated dateTime.

                    supplyPaymentTasksToUpdate.Add(paymentDeliveryProtocol.SupplyPaymentTask);
                }
            }

            if (paymentDeliveryProtocol.IsNew())
                paymentDeliveryProtocolsToAdd.Add(paymentDeliveryProtocol);
            else
                paymentDeliveryProtocolsToUpdate.Add(paymentDeliveryProtocol);
        }

        if (paymentDeliveryProtocolsToUpdate.Any()) supplyOrderPaymentDeliveryProtocolRepository.Update(paymentDeliveryProtocolsToUpdate);

        if (paymentDeliveryProtocolsToAdd.Any()) supplyOrderPaymentDeliveryProtocolRepository.Add(paymentDeliveryProtocolsToAdd);

        if (supplyPaymentTasksToUpdate.Any()) supplyPaymentTaskRepository.Update(supplyPaymentTasksToUpdate);

        if (supplyOrderPaymentDeliveryProtocolKeysToUpdate.Any()) supplyOrderPaymentDeliveryProtocolKeyRepository.Update(supplyOrderPaymentDeliveryProtocolKeysToUpdate);
    }
}