using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Messages.Accounting.CashFlows;
using GBA.Domain.Messages.ReSales;
using GBA.Domain.Repositories.Accounting.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Accounting;

public sealed class AccountingCashFlowActor : ReceiveActor {
    private readonly IAccountingRepositoriesFactory _accountingRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public AccountingCashFlowActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IAccountingRepositoriesFactory accountingRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _accountingRepositoriesFactory = accountingRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<GetAccountingCashFlowInfoMessage>(ProcessGetAccountingCashFlowInfoMessage);

        Receive<ExportAccountingCashFlowDocumentMessage>(ProcessExportAccountingCashFlowDocumentMessage);

        Receive<GetAccountingCashFlowMessage>(ProcessGetAccountingCashFlowMessage);
    }

    private void ProcessGetAccountingCashFlowInfoMessage(GetAccountingCashFlowInfoMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            AccountingCashFlow accountingCashFlow = null;
            SupplyOrganization supplyOrganization = _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection).GetByNetId(message.NetId);

            if (supplyOrganization != null) {
                accountingCashFlow = _supplyRepositoriesFactory
                    .NewSupplyOrganizationCashFlowRepository(connection)
                    .GetRangedBySupplyOrganization(supplyOrganization, message.From, message.To, message.TypePaymentTask);
            } else {
                SupplyOrganizationAgreement agreement = _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection).GetByNetId(message.NetId);

                if (agreement != null) {
                    accountingCashFlow = _supplyRepositoriesFactory
                        .NewSupplyOrganizationCashFlowRepository(connection)
                        .GetRangedBySupplyOrganizationAgreement(agreement, message.From, message.To, message.TypePaymentTask);
                } else {
                    Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetIdWithRoleAndType(message.NetId);

                    if (client != null) {
                        if (client.ClientInRole != null) {
                            if (client.ClientInRole.ClientTypeRole.Name.ToLower().Equals("manufacturer"))
                                accountingCashFlow = _clientRepositoriesFactory
                                    .NewClientCashFlowRepository(connection)
                                    .GetRangedBySupplierV2(client, message.From, message.To);
                            else
                                accountingCashFlow = _clientRepositoriesFactory
                                    .NewClientCashFlowRepository(connection)
                                    .GetRangedByClient(client, message.From, message.To);
                        } else {
                            accountingCashFlow = _clientRepositoriesFactory
                                .NewClientCashFlowRepository(connection)
                                .GetRangedBySupplierV2(client, message.From, message.To);
                        }
                    } else {
                        ClientAgreement clientAgreement =
                            _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByNetIdWithClientRole(message.NetId);

                        if (clientAgreement != null) {
                            if (clientAgreement.Client.ClientInRole != null &&
                                clientAgreement.Client.ClientInRole.ClientTypeRole.Name.ToLower().Equals("manufacturer"))
                                accountingCashFlow = _clientRepositoriesFactory
                                    .NewClientCashFlowRepository(connection)
                                    .GetRangedBySupplierClientAgreementV2(
                                        clientAgreement,
                                        message.From,
                                        message.To
                                    );
                            else
                                accountingCashFlow = _clientRepositoriesFactory
                                    .NewClientCashFlowRepository(connection)
                                    .GetRangedByClientAgreement(
                                        clientAgreement,
                                        message.From,
                                        message.To,
                                        clientAgreement.Agreement.Currency != null && clientAgreement.Agreement.Currency.Code.ToUpper().Equals("EUR")
                                    );
                        } else {
                            Sender.Tell(new AccountingCashFlow());
                        }
                    }
                }
            }

            if (accountingCashFlow != null) {
                foreach (AccountingCashFlowHeadItem cashFlowHeadItem in accountingCashFlow
                             .AccountingCashFlowHeadItems.Where(i => i.Type.Equals(JoinServiceType.ReSale)))
                    cashFlowHeadItem.UpdatedReSaleModel = (UpdatedReSaleModel)ActorReferenceManager.Instance.Get(BaseActorNames.RE_SALE_ACTOR)
                        .Ask<object>(new GetUpdatedReSaleByNetIdMessage(null, cashFlowHeadItem.UpdatedReSaleModel.ReSale.NetUid)).Result;

                accountingCashFlow.AccountingCashFlowHeadItems =
                    _accountingRepositoriesFactory
                        .NewAccountingDocumentNameRepository(connection)
                        .GetDocumentNames(accountingCashFlow.AccountingCashFlowHeadItems, message.TypePaymentTask);
            } else {
                Sender.Tell(null);
            }

            Sender.Tell(accountingCashFlow);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAccountingCashFlowMessage(GetAccountingCashFlowMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            AccountingCashFlow accountingCashFlow = null;

            SupplyOrganization supplyOrganization = _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection).GetByNetId(message.NetId);

            if (supplyOrganization != null) {
                accountingCashFlow = _supplyRepositoriesFactory
                    .NewSupplyOrganizationCashFlowRepository(connection)
                    .GetRangedBySupplyOrganization(supplyOrganization, message.From, message.To, message.TypePaymentTask);
            } else {
                SupplyOrganizationAgreement agreement = _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection).GetByNetId(message.NetId);

                if (agreement != null) {
                    accountingCashFlow = _supplyRepositoriesFactory
                        .NewSupplyOrganizationCashFlowRepository(connection)
                        .GetRangedBySupplyOrganizationAgreement(agreement, message.From, message.To, message.TypePaymentTask);
                } else {
                    Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetIdWithRoleAndType(message.NetId);

                    if (client != null) {
                        if (client.ClientInRole != null) {
                            if (client.ClientInRole.ClientTypeRole.Name.ToLower().Equals("manufacturer"))
                                accountingCashFlow = _clientRepositoriesFactory
                                    .NewClientCashFlowRepository(connection)
                                    .GetRangedBySupplierV2(client, message.From, message.To);
                            else
                                accountingCashFlow = _clientRepositoriesFactory
                                    .NewClientCashFlowRepository(connection)
                                    .GetRangedByClient(client, message.From, message.To);
                        } else {
                            accountingCashFlow = _clientRepositoriesFactory
                                .NewClientCashFlowRepository(connection)
                                .GetRangedBySupplierV2(client, message.From, message.To);
                        }
                    } else {
                        ClientAgreement clientAgreement =
                            _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByNetIdWithClientRole(message.NetId);

                        if (clientAgreement != null) {
                            if (clientAgreement.Client.ClientInRole != null &&
                                clientAgreement.Client.ClientInRole.ClientTypeRole.Name.ToLower().Equals("manufacturer"))
                                accountingCashFlow = _clientRepositoriesFactory
                                    .NewClientCashFlowRepository(connection)
                                    .GetRangedBySupplierClientAgreementV2(
                                        clientAgreement,
                                        message.From,
                                        message.To
                                    );
                            else
                                accountingCashFlow = _clientRepositoriesFactory
                                    .NewClientCashFlowRepository(connection)
                                    .GetRangedByClientAgreement(
                                        clientAgreement,
                                        message.From,
                                        message.To,
                                        clientAgreement.Agreement.Currency != null && clientAgreement.Agreement.Currency.Code.ToUpper().Equals("EUR")
                                    );
                        } else {
                            Sender.Tell(new AccountingCashFlow());
                        }
                    }
                }
            }

            if (accountingCashFlow != null) {
                foreach (AccountingCashFlowHeadItem cashFlowHeadItem in accountingCashFlow
                             .AccountingCashFlowHeadItems.Where(i => i.Type.Equals(JoinServiceType.ReSale)))
                    cashFlowHeadItem.UpdatedReSaleModel = (UpdatedReSaleModel)ActorReferenceManager.Instance.Get(BaseActorNames.RE_SALE_ACTOR)
                        .Ask<object>(new GetUpdatedReSaleByNetIdMessage(null, cashFlowHeadItem.UpdatedReSaleModel.ReSale.NetUid)).Result;

                accountingCashFlow.AccountingCashFlowHeadItems =
                    _accountingRepositoriesFactory
                        .NewAccountingDocumentNameRepository(connection)
                        .GetDocumentNamesForClients(accountingCashFlow.AccountingCashFlowHeadItems);
            } else {
                Sender.Tell(null);
            }

            Sender.Tell(accountingCashFlow);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessExportAccountingCashFlowDocumentMessage(ExportAccountingCashFlowDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            AccountingCashFlow accountingCashFlow = null;

            User user = _userRepositoriesFactory.NewUserRepository(connection)
                .GetByNetIdWithoutIncludes(message.UserNetId);

            SupplyOrganization supplyOrganization = _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection).GetByNetId(message.NetId);

            if (supplyOrganization != null) {
                accountingCashFlow =
                    _supplyRepositoriesFactory
                        .NewSupplyOrganizationCashFlowRepository(connection)
                        .GetRangedBySupplyOrganization(supplyOrganization, message.From, message.To, message.TypePaymentTask);
            } else {
                SupplyOrganizationAgreement agreement = _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection).GetByNetId(message.NetId);

                if (agreement != null) {
                    accountingCashFlow =
                        _supplyRepositoriesFactory
                            .NewSupplyOrganizationCashFlowRepository(connection)
                            .GetRangedBySupplyOrganizationAgreement(agreement, message.From, message.To, message.TypePaymentTask);
                } else {
                    Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetIdWithRoleAndType(message.NetId);

                    if (client != null) {
                        if (client.ClientInRole != null) {
                            if (client.ClientInRole.ClientTypeRole.Name.ToLower().Equals("manufacturer"))
                                accountingCashFlow =
                                    _clientRepositoriesFactory
                                        .NewClientCashFlowRepository(connection)
                                        .GetRangedBySupplierV2(client, message.From, message.To);
                            else
                                accountingCashFlow =
                                    _clientRepositoriesFactory
                                        .NewClientCashFlowRepository(connection)
                                        .GetRangedByClient(client, message.From, message.To);
                        } else {
                            accountingCashFlow =
                                _clientRepositoriesFactory
                                    .NewClientCashFlowRepository(connection)
                                    .GetRangedBySupplierV2(client, message.From, message.To);
                        }
                    } else {
                        ClientAgreement clientAgreement =
                            _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByNetIdWithClientRole(message.NetId);

                        if (clientAgreement != null) {
                            if (clientAgreement.Client.ClientInRole != null &&
                                clientAgreement.Client.ClientInRole.ClientTypeRole.Name.ToLower().Equals("manufacturer"))
                                accountingCashFlow =
                                    _clientRepositoriesFactory
                                        .NewClientCashFlowRepository(connection)
                                        .GetRangedBySupplierClientAgreementV2(
                                            clientAgreement,
                                            message.From,
                                            message.To
                                        );
                            else
                                accountingCashFlow =
                                    _clientRepositoriesFactory
                                        .NewClientCashFlowRepository(connection)
                                        .GetRangedByClientAgreement(
                                            clientAgreement,
                                            message.From,
                                            message.To,
                                            clientAgreement.Agreement.Currency != null && clientAgreement.Agreement.Currency.Code.ToUpper().Equals("EUR")
                                        );
                        }
                    }
                }
            }

            if (accountingCashFlow == null) {
                Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
            } else {
                accountingCashFlow.AccountingCashFlowHeadItems =
                    _accountingRepositoriesFactory
                        .NewAccountingDocumentNameRepository(connection)
                        .GetDocumentNames(accountingCashFlow.AccountingCashFlowHeadItems);

                (string excelFilePath, string pdfFilePath) =
                    _xlsFactoryManager
                        .NewAccountingXlsManager()
                        .ExportAccountingCashFlowToXlsx(
                            message.PathToFolder,
                            accountingCashFlow,
                            user,
                            message.To
                        );

                Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
            }
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }
}