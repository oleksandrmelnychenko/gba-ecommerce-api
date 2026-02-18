using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Messages.Consignments;
using GBA.Domain.Messages.Supplies.Returns;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies;

public sealed class SupplyReturnsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public SupplyReturnsActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;

        Receive<AddNewSupplyReturnMessage>(ProcessAddNewSupplyReturnMessage);
    }

    private void ProcessAddNewSupplyReturnMessage(AddNewSupplyReturnMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            if (message.SupplyReturn == null) throw new Exception(SupplyReturnResourceNames.NULL_ENTITY);
            if (message.SupplyReturn.ClientAgreement == null || message.SupplyReturn.ClientAgreement.IsNew())
                throw new Exception(SupplyReturnResourceNames.SPECIFY_CLIENT_AGREEMENT);
            if (message.SupplyReturn.Organization == null || message.SupplyReturn.Organization.IsNew())
                throw new Exception(SupplyReturnResourceNames.SPECIFY_ORGANIZATION);
            if (message.SupplyReturn.Storage == null || message.SupplyReturn.Storage.IsNew())
                throw new Exception(SupplyReturnResourceNames.SPECIFY_STORAGE);
            if (message.SupplyReturn.Supplier == null || message.SupplyReturn.Supplier.IsNew())
                throw new Exception(SupplyReturnResourceNames.SPECIFY_SUPPLIER);
            if (!message.SupplyReturn.SupplyReturnItems.Any())
                throw new Exception(SupplyReturnResourceNames.SPECIFY_AT_LEAST_ONE_ITEM);
            if (!message.SupplyReturn.SupplyReturnItems.All(i => i.Product != null && !i.Product.IsNew()))
                throw new Exception(SupplyReturnResourceNames.SPECIFY_PRODUCT_FOR_ALL_ITEMS);
            if (!message.SupplyReturn.SupplyReturnItems.All(i => i.ConsignmentItem != null && !i.ConsignmentItem.IsNew() || !i.ConsignmentItemId.Equals(0)))
                throw new Exception(SupplyReturnResourceNames.SPECIFY_CONSIGNMENT_FOR_ALL_ITEMS);
            if (message.SupplyReturn.Comment.Length > 500)
                throw new Exception(SupplyReturnResourceNames.COMMENT_OVERFLOWS_ALLOWED_SIZE);

            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

            Storage storage = storageRepository.GetById(message.SupplyReturn.Storage.Id);

            if (storage == null) throw new Exception(SupplyReturnResourceNames.STORAGE_DOES_NOT_EXISTS);

            Organization organization = _organizationRepositoriesFactory.NewOrganizationRepository(connection).GetById(message.SupplyReturn.Organization.Id);

            if (organization == null) throw new Exception(SupplyReturnResourceNames.ORGANIZATION_DOES_NOT_EXISTS);

            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            ISupplyReturnRepository supplyReturnRepository = _supplyRepositoriesFactory.NewSupplyReturnRepository(connection);
            ISupplyReturnItemRepository supplyReturnItemRepository = _supplyRepositoriesFactory.NewSupplyReturnItemRepository(connection);

            message.SupplyReturn.ClientAgreementId = message.SupplyReturn.ClientAgreement.Id;
            message.SupplyReturn.OrganizationId = message.SupplyReturn.Organization.Id;
            message.SupplyReturn.StorageId = message.SupplyReturn.Storage.Id;
            message.SupplyReturn.SupplierId = message.SupplyReturn.Supplier.Id;

            message.SupplyReturn.FromDate =
                message.SupplyReturn.FromDate.Year.Equals(1)
                    ? DateTime.UtcNow.Date
                    : message.SupplyReturn.FromDate;

            message.SupplyReturn.ResponsibleId =
                message.SupplyReturn.Responsible != null && !message.SupplyReturn.Responsible.IsNew()
                    ? message.SupplyReturn.Responsible.Id
                    : userRepository.GetByNetIdWithoutIncludes(message.UserNetId).Id;

            SupplyReturn lastRecord = supplyReturnRepository.GetLastRecord(organization.Id);

            if (lastRecord != null && DateTime.Now.Year.Equals(lastRecord.Created.Year))
                message.SupplyReturn.Number =
                    string.Format(
                        "{0}{1}",
                        organization.Code,
                        string.Format(
                            "{0:D11}",
                            Convert.ToInt32(lastRecord.Number.Substring(organization.Code.Length, lastRecord.Number.Length - organization.Code.Length)) + 1
                        )
                    );
            else
                message.SupplyReturn.Number =
                    $"{organization.Code}{string.Format("{0:D11}", 1)}";

            message.SupplyReturn.Id = supplyReturnRepository.Add(message.SupplyReturn);

            supplyReturnItemRepository
                .Add(message.SupplyReturn.SupplyReturnItems.Select(item => {
                    item.ProductId = item.Product.Id;
                    item.ConsignmentItemId = item.ConsignmentItem != null && !item.ConsignmentItem.IsNew() ? item.ConsignmentItem.Id : item.ConsignmentItemId;
                    item.SupplyReturnId = message.SupplyReturn.Id;

                    return item;
                }));

            ActorReferenceManager.Instance.Get(BaseActorNames.CONSIGNMENTS_ACTOR)
                .Forward(new ValidateAndStoreConsignmentMovementFromSupplyReturnMessage(message.SupplyReturn.Id, message.SupplyReturn.IsManagement));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}