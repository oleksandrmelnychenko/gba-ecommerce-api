using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers.PrintingDocuments;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.Supplies.SupplyOrderModels;
using GBA.Domain.Messages.Supplies;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Services.Actors.Supplies.SupplyOrdersGetActors;

public sealed class BaseSupplyOrdersGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseSupplyOrdersGetActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<GetNearestSupplyArrivalByProductNetIdMessage>(ProcessGetNearestSupplyArrivalByProductNetIdMessage);

        Receive<GetTotalsBySupplyOrderNetIdMessage>(ProcessGetTotalsBySupplyOrderNetIdMessage);

        Receive<GetSupplyOrderByNetIdIfExistsMessage>(ProcessGetSupplyOrderByNetIdIfExistsMessage);

        Receive<GetSupplyOrderByNetIdMessage>(ProcessGetSupplyOrderByNetIdMessage);

        Receive<GetAllSupplyOrdersMessage>(ProcessGetAllSupplyOrdersMessage);

        Receive<GetAllSupplyOrderPaymentDeliveryProtocolKeysMessage>(ProcessGetAllSupplyOrderPaymentDeliveryProtocolKeysMessage);

        Receive<GetAllSupplyInformationDeliveryProtocolKeysMessage>(ProcessGetAllSupplyInformationDeliveryProtocolKeysMessage);

        Receive<GetAllServiceDetailItemKeysByServiceTypeMessage>(ProcessGetAllServiceDetailItemKeysByServiceTypeMessage);

        Receive<GetTotalsOnSupplyOrderItemsBySupplyOrderNetIdMessage>(ProcessGetTotalsOnSupplyOrderItemsBySupplyOrderNetIdMessage);

        Receive<GetAllFromSearchMessage>(ProcessGetAllFromSearchMessage);

        Receive<GetAllSupplyOrdersForUkOrganizationsFilteredMessage>(ProcessGetAllSupplyOrdersForUkOrganizationsFilteredMessage);

        Receive<GetAllSupplyOrdersForPlacementMessage>(ProcessGetAllSupplyOrdersForPlacementMessage);

        Receive<GetSupplyOrderByNetIdForPlacementMessage>(ProcessGetSupplyOrderByNetIdForPlacementMessage);

        Receive<GetUrlSupplyOrdersPrintDocumentMessage>(ProcessGetUrlSupplyOrdersPrintDocument);
    }

    private void ProcessGetNearestSupplyArrivalByProductNetIdMessage(GetNearestSupplyArrivalByProductNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell((object)_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetNearestSupplyArrivalByProductNetId(message.NetId));
    }

    private void ProcessGetTotalsBySupplyOrderNetIdMessage(GetTotalsBySupplyOrderNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell((object)_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetTotalsByNetId(message.NetId));
    }

    private void ProcessGetSupplyOrderByNetIdIfExistsMessage(GetSupplyOrderByNetIdIfExistsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyOrderRepository(connection)
            .GetByNetIdIfExist(message.NetId)
        );
    }

    private void ProcessGetSupplyOrderByNetIdMessage(GetSupplyOrderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyRepositoriesFactory
                .NewSupplyOrderRepository(connection)
                .GetByNetId(message.NetId)
        );
    }

    private void ProcessGetAllSupplyOrdersMessage(GetAllSupplyOrdersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyOrderRepository(connection)
            .GetAll()
        );
    }

    private void ProcessGetAllSupplyOrderPaymentDeliveryProtocolKeysMessage(GetAllSupplyOrderPaymentDeliveryProtocolKeysMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderPaymentDeliveryProtocolKeyRepository supplyOrderPaymentDeliveryProtocolKeyRepository =
            _supplyRepositoriesFactory.NewSupplyOrderPaymentDeliveryProtocolKeyRepository(connection);

        List<SupplyOrderPaymentDeliveryProtocolKey> keys = supplyOrderPaymentDeliveryProtocolKeyRepository.GetAll();

        if (keys.Any()) {
            Sender.Tell(keys);
        } else {
            keys.Add(new SupplyOrderPaymentDeliveryProtocolKey {
                Key = "������"
            });

            keys.Add(new SupplyOrderPaymentDeliveryProtocolKey {
                Key = "�����������"
            });

            supplyOrderPaymentDeliveryProtocolKeyRepository.Add(keys);

            Sender.Tell(supplyOrderPaymentDeliveryProtocolKeyRepository.GetAll());
        }
    }

    private void ProcessGetAllSupplyInformationDeliveryProtocolKeysMessage(GetAllSupplyInformationDeliveryProtocolKeysMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyInformationDeliveryProtocolKeyRepository(connection)
            .GetAll()
        );
    }

    private void ProcessGetAllServiceDetailItemKeysByServiceTypeMessage(GetAllServiceDetailItemKeysByServiceTypeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewServiceDetailItemKeyRepository(connection)
            .GetAllByType(message.SupplyServiceType)
        );
    }

    private void ProcessGetTotalsOnSupplyOrderItemsBySupplyOrderNetIdMessage(GetTotalsOnSupplyOrderItemsBySupplyOrderNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell((object)_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetTotalsOnSupplyOrderItemsBySupplyOrderNetId(message.NetId));
    }

    private void ProcessGetAllFromSearchMessage(GetAllFromSearchMessage message) {
        if (message.Limit.Equals(0)) message.Limit = 20;

        if (message.Offset < 0) message.Offset = 0;

        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        if (message.From.Year.Equals(1)) message.From = TimeZoneInfo.ConvertTimeToUtc(DateTime.UtcNow.Date);

        message.To =
            TimeZoneInfo.ConvertTimeToUtc(message.To.Year.Equals(1)
                ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59)
                : message.To.AddHours(23).AddMinutes(59).AddSeconds(59));

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (!string.IsNullOrEmpty(message.DocumentType))
            Sender.Tell(
                _supplyRepositoriesFactory
                    .NewSupplyOrderDeliveryDocumentRepository(connection)
                    .GetAllFromSearch(
                        message.DocumentType,
                        message.Limit,
                        message.Offset,
                        message.From,
                        message.To,
                        message.ClientNetId
                    )
            );
        else
            switch (message.Type) {
                case OrderFilterType.All:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewSupplyOrderRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.Order:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewSupplyOrderRepository(connection)
                            .GetAllFromSearchByOrderNumber(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.ProForm:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewSupplyProFormRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.Invoice:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewSupplyInvoiceRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.ContainerService:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewContainerServiceRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.CustomAgencyService:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewCustomAgencyServiceRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.CustomService:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewCustomServiceRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.PlaneDeliveryService:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewPlaneDeliveryServiceRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.PortCustomAgencyService:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewPortCustomAgencyServiceRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.PortWorkService:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewPortWorkServiceRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.TransportationService:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewTransportationServiceRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.VehicleDeliveryService:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewVehicleDeliveryServiceRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.Product:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewSupplyOrderRepository(connection)
                            .GetAllFromSearchByProduct(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                case OrderFilterType.MergedService:
                    Sender.Tell(
                        _supplyRepositoriesFactory
                            .NewMergedServiceRepository(connection)
                            .GetAllFromSearch(
                                message.Value,
                                message.Limit,
                                message.Offset,
                                message.From,
                                message.To,
                                message.ClientNetId
                            )
                    );
                    break;
                default:
                    Sender.Tell(new List<object>());
                    break;
            }
    }

    private void ProcessGetAllSupplyOrdersForUkOrganizationsFilteredMessage(GetAllSupplyOrdersForUkOrganizationsFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyRepositoriesFactory
                .NewSupplyOrderRepository(connection)
                .GetAllFromSearchForUkOrganizations(
                    message.Value,
                    message.Limit,
                    message.Offset,
                    message.From,
                    message.To,
                    message.SupplierName,
                    message.CurrencyId,
                    message.ClientNetId
                )
        );
    }

    private void ProcessGetAllSupplyOrdersForPlacementMessage(GetAllSupplyOrdersForPlacementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetAllForPlacement());
    }

    private void ProcessGetSupplyOrderByNetIdForPlacementMessage(GetSupplyOrderByNetIdForPlacementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetIdForPlacement(message.NetId));
    }

    private void ProcessGetUrlSupplyOrdersPrintDocument(GetUrlSupplyOrdersPrintDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            List<SupplyOrderModel> rowToPrint =
                _supplyRepositoriesFactory
                    .NewSupplyOrderRepository(connection)
                    .GetAllForPrint(message.From, message.To);

            rowToPrint.AddRange(
                _supplyUkraineRepositoriesFactory
                    .NewSupplyOrderUkraineRepository(connection)
                    .GetAllForPrintDocument(message.From, message.To));

            PrintDocumentsHelper printDocumentsHelper = new(rowToPrint.OrderByDescending(x => x.FromDate), message.DataForPrint);

            List<Dictionary<string, string>> rows = printDocumentsHelper.GetRowsForPrintDocument();

            (string pathXls, string pathPdf) =
                _xlsFactoryManager
                    .NewPrintDocumentsManager()
                    .GetPrintDocument(
                        message.PathToFolder,
                        message.DataForPrint,
                        rows);

            Sender.Tell((pathXls, pathPdf));
        } catch {
            Sender.Tell((string.Empty, string.Empty));
        }
    }
}