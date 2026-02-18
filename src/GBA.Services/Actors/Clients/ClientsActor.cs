using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Util.Internal;
using GBA.Common.Helpers;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Common.ResourceNames;
using GBA.Domain.AuditEntities;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.EntityHelpers;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Messages.Auditing;
using GBA.Domain.Messages.Clients;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Regions.Contracts;
using GBA.Domain.Repositories.ServicePayers.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Clients;

public sealed class ClientsActor : ReceiveActor {
    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IIdentityRepositoriesFactory _identityRepositoriesFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IRegionRepositoriesFactory _regionRepositoriesFactory;
    private readonly IServicePayerRepositoryFactory _servicePayerRepositoryFactory;

    public ClientsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IRegionRepositoriesFactory regionRepositoriesFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IServicePayerRepositoryFactory servicePayerRepositoryFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        IIdentityRepositoriesFactory identityRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _regionRepositoriesFactory = regionRepositoriesFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _servicePayerRepositoryFactory = servicePayerRepositoryFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _identityRepositoriesFactory = identityRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        Receive<SwitchActiveClientStateMessage>(ProcessSwitchActiveClientStateMessage);

        Receive<AddClientMessage>(ProcessAddClientMessage);

        Receive<UpdateClientMessage>(ProcessUpdateClientMessage);

        Receive<DeleteClientMessage>(ProcessDeleteClientMessage);

        Receive<UpdateClientPasswordMessage>(ProcessUpdateClientPasswordMessage);

        Receive<SetIsForRetailMessage>(ProcessSetIsForRetailMessage);

        Receive<AddClientGroupMessage>(ProcessAddClientGroupMessage);

        Receive<UpdateClientGroupMessage>(ProcessUpdateClientGroupMessage);

        Receive<UpdateClientOrderExpireDaysMessage>(ProcessUpdateClientOrderExpireDaysMessage);

        Receive<AddClientWorkplaceMessage>(ProcessAddClientWorkplaceMessage);

        Receive<RemoveClientGroupFromClientMessage>(ProcessRemoveClientGroupFromClientMessage);
    }

    private void ProcessRemoveClientGroupFromClientMessage(RemoveClientGroupFromClientMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _clientRepositoriesFactory.NewWorkplaceRepository(connection).RemoveClientGroupByNetId(message.NetId);

        Sender.Tell(null);
    }

    private void ProcessAddClientWorkplaceMessage(AddClientWorkplaceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientWorkplaceRepository clientWorkplaceRepository = _clientRepositoriesFactory.NewClientWorkplaceRepository(connection);
        clientWorkplaceRepository.AddClientWorkplace(message.ClientWorkplace);

        Sender.Tell(clientWorkplaceRepository.GetWorkplacesByMainClientId(message.ClientWorkplace.MainClientId));
    }

    private void ProcessUpdateClientGroupMessage(UpdateClientGroupMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientGroupRepository clientGroupRepository = _clientRepositoriesFactory.NewClientGroupRepository(connection);
        clientGroupRepository.Update(message.ClientGroup);

        if (message.ClientGroup.Deleted) {
            IWorkplaceRepository workplaceRepository = _clientRepositoriesFactory.NewWorkplaceRepository(connection);
            IEnumerable<Workplace> workplaces = workplaceRepository.GetWorkplacesByClientGroupId(message.ClientGroup.Id);

            workplaces.ForEach(e => e.ClientGroupId = null);

            workplaceRepository.Update(workplaces);
        }

        Sender.Tell(clientGroupRepository.GetAllByClientId(message.ClientGroup.ClientId));
    }

    private void ProcessUpdateClientOrderExpireDaysMessage(UpdateClientOrderExpireDaysMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);
        clientRepository.UpdateOrderExpireDays(message.ClientNetId, message.ExpireDays);

        Sender.Tell(true);
    }

    private void ProcessAddClientGroupMessage(AddClientGroupMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientGroupRepository clientGroupRepository = _clientRepositoriesFactory.NewClientGroupRepository(connection);

        clientGroupRepository.Add(message.ClientGroup);

        Sender.Tell(clientGroupRepository.GetAllByClientId(message.ClientGroup.ClientId));
    }

    private void ProcessSwitchActiveClientStateMessage(SwitchActiveClientStateMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);

        Client client = clientRepository.GetByNetId(message.NetId);

        if (client == null) return;

        client.IsActive = !client.IsActive;

        clientRepository.Update(client);
    }

    private void ProcessAddClientMessage(AddClientMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);

        message.Client.OrderExpireDays = message.Client.ClientInRole.ClientTypeRole.OrderExpireDays != 0 ? message.Client.ClientInRole.ClientTypeRole.OrderExpireDays : 3;
        message.Client.ClearCartAfterDays = 3;

        if (message.Client.Region != null && !message.Client.Region.IsNew())
            message.Client.RegionId = message.Client.Region.Id;
        else
            message.Client.RegionId = null;

        if (message.Client.ClientInRole != null && message.Client.ClientInRole.ClientType.Type.Equals(ClientTypeType.Provider)) {
            if (message.Client.ClientBankDetails != null) {
                if (message.Client.ClientBankDetails.ClientBankDetailIbanNo != null) {
                    if (message.Client.ClientBankDetails.ClientBankDetailIbanNo.Currency != null)
                        message.Client.ClientBankDetails.ClientBankDetailIbanNo.CurrencyId = message.Client.ClientBankDetails.ClientBankDetailIbanNo.Currency.Id;

                    Currency currency = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists();

                    message.Client.ClientBankDetails.ClientBankDetailIbanNo.Currency = currency;
                    message.Client.ClientBankDetails.ClientBankDetailIbanNo.CurrencyId = currency.Id;

                    message.Client.ClientBankDetails.AccountNumber = new ClientBankDetailAccountNumber {
                        Currency = currency,
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow
                    };

                    message.Client.ClientBankDetails.ClientBankDetailIbanNoId = _clientRepositoriesFactory.NewClientBankDetailIbanNoRepository(connection)
                        .Add(message.Client.ClientBankDetails.ClientBankDetailIbanNo);
                }

                if (message.Client.ClientBankDetails.AccountNumber != null) {
                    if (message.Client.ClientBankDetails.AccountNumber.Currency != null)
                        message.Client.ClientBankDetails.AccountNumber.CurrencyId = message.Client.ClientBankDetails.AccountNumber.Currency.Id;

                    message.Client.ClientBankDetails.AccountNumberId = _clientRepositoriesFactory.NewClientBankDetailAccountNumberRepository(connection)
                        .Add(message.Client.ClientBankDetails.AccountNumber);
                }

                message.Client.ClientBankDetailsId = _clientRepositoriesFactory.NewClientBankDetailsRepository(connection).Add(message.Client.ClientBankDetails);
            }

            if (message.Client.Country != null && !message.Client.Country.IsNew())
                message.Client.CountryId = message.Client.Country.Id;
            else
                message.Client.CountryId = null;

            if (message.Client.TermsOfDelivery != null && !message.Client.TermsOfDelivery.IsNew())
                message.Client.TermsOfDeliveryId = message.Client.TermsOfDelivery.Id;
            else
                message.Client.TermsOfDeliveryId = null;

            if (message.Client.PackingMarking != null && !message.Client.PackingMarking.IsNew())
                message.Client.PackingMarkingId = message.Client.PackingMarking.Id;
            else
                message.Client.PackingMarkingId = null;

            if (message.Client.PackingMarkingPayment != null && !message.Client.PackingMarkingPayment.IsNew())
                message.Client.PackingMarkingPaymentId = message.Client.PackingMarkingPayment.Id;
            else
                message.Client.PackingMarkingPaymentId = null;
        } else {
            if (message.Client.RegionCode != null) {
                IRegionRepository regionRepository = _regionRepositoriesFactory.NewRegionRepository(connection);
                IRegionCodeRepository regionCodeRepository = _regionRepositoriesFactory.NewRegionCodeRepository(connection);

                if (message.Client.RegionCode.IsNew()) {
                    if (string.IsNullOrEmpty(message.Client.RegionCode.Value)) throw new Exception(ClientResourceNames.EMPTY_REGION_CODE);

                    if (message.Client.RegionCode.Value.Length > 10) throw new Exception(ClientResourceNames.TO_LONG_REGION_CODE);

                    string region = message.Client.RegionCode.Value.Substring(0, message.Client.RegionCode.Value.Length - 5);
                    string regionCode = message.Client.RegionCode.Value.Substring(message.Client.RegionCode.Value.Length - 5);

                    Region regionFromDb = regionRepository.GetByName(region);

                    if (regionFromDb == null) {
                        regionFromDb = new Region {
                            Name = region.ToUpper()
                        };

                        regionFromDb.Id = regionRepository.Add(regionFromDb);
                    }

                    bool isAvailable = !regionCodeRepository.IsAssignedToAnyContact($"{region}{regionCode}");

                    if (!isAvailable) throw new Exception(ClientResourceNames.UNAVAILABLE_REGION_CODE);

                    message.Client.RegionId = regionFromDb.Id;

                    message.Client.RegionCode.RegionId = regionFromDb.Id;

                    message.Client.RegionCodeId =
                        regionCodeRepository
                            .Add(message.Client.RegionCode);
                } else {
                    if (string.IsNullOrEmpty(message.Client.RegionCode.Value)) throw new Exception(ClientResourceNames.EMPTY_REGION_CODE);

                    RegionCode regionCodeFromDb = regionCodeRepository.GetById(message.Client.RegionCode.Id);

                    if (regionCodeFromDb.Value.ToUpper().Equals(message.Client.RegionCode.Value.ToUpper())) {
                        message.Client.RegionCodeId = message.Client.RegionCode.Id;

                        regionCodeRepository.Update(message.Client.RegionCode);
                    } else {
                        if (message.Client.RegionCode.Value.Length > 10) throw new Exception(ClientResourceNames.TO_LONG_REGION_CODE);

                        Regex regex = new(@"^([A-z]{2,5})(\d{1,7})$");

                        Match match = regex.Match(message.Client.RegionCode.Value);

                        if (!match.Success) throw new Exception(ClientResourceNames.INVALID_REGION_CODE);

                        string region = match.Groups[1].Value;
                        string regionCode = match.Groups[2].Value;

                        Region regionFromDb = regionRepository.GetByName(region);

                        if (regionFromDb == null) {
                            regionFromDb = new Region {
                                Name = region.ToUpper()
                            };

                            regionFromDb.Id = regionRepository.Add(regionFromDb);
                        }

                        bool isAvailable = !regionCodeRepository.IsAssignedToAnyContact($"{region}{regionCode}");

                        if (!isAvailable) throw new Exception(ClientResourceNames.UNAVAILABLE_REGION_CODE);

                        message.Client.RegionId = regionFromDb.Id;

                        message.Client.RegionCode.RegionId = regionFromDb.Id;

                        message.Client.RegionCodeId = message.Client.RegionCode.Id;

                        regionCodeRepository.Update(message.Client.RegionCode);
                    }
                }
            }
        }

        if (message.Client.IsForRetail)
            Task.Factory.StartNew(() => {
                Client retailClient = clientRepository.GetRetailClient();

                if (retailClient != null) {
                    retailClient.IsForRetail = false;
                    clientRepository.Update(retailClient);
                }
            });

        if (message.ParentNetId.Equals(Guid.Empty)) {
            message.Client.Id = clientRepository.Add(message.Client);
        } else {
            //message.Client.IsSubClient = true;

            message.Client.Id = clientRepository.Add(message.Client);

            Client parentClient = clientRepository.GetByNetId(message.ParentNetId);

            if (parentClient != null)
                _clientRepositoriesFactory.NewClientSubClientRepository(connection).Add(new ClientSubClient {
                    RootClientId = parentClient.Id,
                    SubClientId = message.Client.Id
                });
        }

        if (message.Client.ClientInRole != null) {
            message.Client.ClientInRole.ClientId = message.Client.Id;
            message.Client.ClientInRole.ClientTypeId = message.Client.ClientInRole.ClientType.Id;
            message.Client.ClientInRole.ClientTypeRoleId = message.Client.ClientInRole.ClientTypeRole.Id;

            _clientRepositoriesFactory.NewClientInRoleRepository(connection).Add(message.Client.ClientInRole);
        }

        if (message.Client.PerfectClients.Any(p => p.IsSelected))
            _clientRepositoriesFactory
                .NewClientPerfectClientRepository(connection)
                .Add(
                    message
                        .Client
                        .PerfectClients
                        .Select(perfectClient => {
                            if (perfectClient.Values.Any())
                                return new ClientPerfectClient {
                                    PerfectClientId = perfectClient.Id,
                                    IsChecked = true,
                                    ClientId = message.Client.Id,
                                    PerfectClientValueId =
                                        perfectClient.Values.FirstOrDefault(a => a.IsSelected)?.Id != null
                                            ? perfectClient.Values.FirstOrDefault(a => a.IsSelected)?.Id
                                            : perfectClient.Values.FirstOrDefault()?.Id,
                                    Value = perfectClient.Value
                                };
                            return new ClientPerfectClient {
                                PerfectClientId = perfectClient.Id,
                                IsChecked = true,
                                ClientId = message.Client.Id,
                                Value = perfectClient.Value
                            };
                        })
                );

        List<long> addedAgreementsIds = new();

        if (message.Client.ClientAgreements.Any()) {
            IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
            IProviderPricingRepository providerPricingRepository = _pricingRepositoriesFactory.NewProviderPricingRepository(connection);
            IOrganizationRepository organizationRepository = _organizationRepositoriesFactory.NewOrganizationRepository(connection);

            foreach (ClientAgreement clientAgreement in message.Client.ClientAgreements) {
                clientAgreement.Agreement.CurrencyId = clientAgreement.Agreement.Currency?.Id;
                clientAgreement.Agreement.OrganizationId = clientAgreement.Agreement.Organization?.Id;

                if (clientAgreement.Agreement.ProviderPricing != null)
                    clientAgreement.Agreement.ProviderPricingId =
                        clientAgreement.Agreement.ProviderPricing.IsNew()
                            ? providerPricingRepository.Add(clientAgreement.Agreement.ProviderPricing)
                            : clientAgreement.Agreement.ProviderPricing?.Id;
                else
                    clientAgreement.Agreement.PricingId = clientAgreement.Agreement.Pricing?.Id;

                if (clientAgreement.Agreement.Currency != null) clientAgreement.Agreement.CurrencyId = clientAgreement.Agreement.Currency.Id;

                if (clientAgreement.Agreement.TaxAccountingScheme != null)
                    clientAgreement.Agreement.TaxAccountingSchemeId = clientAgreement.Agreement.TaxAccountingScheme.Id;

                if (clientAgreement.Agreement.AgreementTypeCivilCode != null)
                    clientAgreement.Agreement.AgreementTypeCivilCodeId = clientAgreement.Agreement.AgreementTypeCivilCode.Id;

                if (!message.Client.ClientAgreements.Any(ca => ca.Agreement.IsSelected))
                    clientAgreement.Agreement.IsSelected = true;

                if (clientAgreement.Agreement.OrganizationId.HasValue) {
                    Organization organization;

                    if (clientAgreement.Agreement.OrganizationId.Equals(0L)) {
                        organization = organizationRepository.GetOrganizationByCurrentCultureIfExists();
                        clientAgreement.Agreement.OrganizationId = organization.Id;
                        clientAgreement.Agreement.Organization = organization;
                    } else {
                        organization = organizationRepository.GetById(clientAgreement.Agreement.OrganizationId.Value);
                    }

                    Agreement lastRecord = agreementRepository.GetLastRecordByOrganizationId(organization.Id);

                    clientAgreement.Agreement.Number = organization.Code;

                    try {
                        if (lastRecord != null && !string.IsNullOrEmpty(lastRecord.Number))
                            clientAgreement.Agreement.Number +=
                                string.Format(
                                    "{0:D5}",
                                    Convert.ToInt64(
                                        lastRecord
                                            .Number
                                            .Substring(
                                                organization.Code.Length,
                                                lastRecord.Number.Length - organization.Code.Length)
                                    ) + 1);
                        else
                            clientAgreement.Agreement.Number += string.Format(
                                "{0:D5}",
                                1
                            );
                    } catch (FormatException) {
                        clientAgreement.Agreement.Number += string.Format(
                            "{0:D5}",
                            1
                        );
                    }
                }

                clientAgreement.ClientId = message.Client.Id;
                clientAgreement.AgreementId = agreementRepository.Add(clientAgreement.Agreement);

                addedAgreementsIds.Add(clientAgreement.AgreementId);

                clientAgreement.Id = clientAgreementRepository.Add(clientAgreement);
            }
        }

        if (message.Client.ClientAgreements.Any(a => a.ProductGroupDiscounts.Any())) {
            IProductGroupDiscountRepository productGroupDiscountRepository = _productRepositoriesFactory.NewProductGroupDiscountRepository(connection);

            ClientAgreement[] clientAgreements = message.Client.ClientAgreements.Where(a => a.ProductGroupDiscounts.Any()).ToArray();
            List<ProductGroupDiscount> toAddDiscounts = new();

            foreach (ClientAgreement clientAgreement in clientAgreements)
            foreach (ProductGroupDiscount discount in clientAgreement.ProductGroupDiscounts) {
                if (discount.SubProductGroupDiscounts.Any())
                    foreach (ProductGroupDiscount subDiscount in discount.SubProductGroupDiscounts) {
                        subDiscount.ClientAgreement = clientAgreement;
                        subDiscount.ClientAgreementId = subDiscount.ClientAgreement.Id;
                        subDiscount.ProductGroupId = subDiscount.ProductGroup.Id;

                        toAddDiscounts.Add(subDiscount);
                    }

                discount.ClientAgreementId = clientAgreement.Id;
                discount.ProductGroupId = discount.ProductGroup.Id;

                toAddDiscounts.Add(discount);
            }

            productGroupDiscountRepository.Add(toAddDiscounts);
        }

        _clientRepositoriesFactory
            .NewClientUserProfileRepository(connection)
            .Add(
                message
                    .Client
                    .ClientManagers
                    .Select(manager => {
                        manager.ClientId = message.Client.Id;
                        manager.UserProfileId = manager.UserProfile.Id;

                        return manager;
                    })
            );

        if (message.Client.ServicePayers.Any())
            _servicePayerRepositoryFactory
                .New(connection)
                .Add(
                    message
                        .Client
                        .ServicePayers
                        .Select(payer => {
                            payer.ClientId = message.Client.Id;

                            return payer;
                        })
                );

        if (message.Client.ClientContractDocuments.Any() && message.Client.ClientInRole.ClientType.Type.Equals(ClientTypeType.Provider))
            _clientRepositoriesFactory.NewClientContractDocumentRepository(connection).Add(message.Client.ClientContractDocuments.Select(d => {
                d.ClientId = message.Client.Id;

                return d;
            }));

        Client clientFromDb = clientRepository.GetById(message.Client.Id);

        //UpdateAbbreviation
        if (!string.IsNullOrEmpty(message.Client.LastName) || !string.IsNullOrEmpty(message.Client.FirstName)) {
            if (!string.IsNullOrEmpty(message.Client.LastName)) clientFromDb.Abbreviation += message.Client.LastName.ToCharArray()[0];
            if (!string.IsNullOrEmpty(message.Client.FirstName)) clientFromDb.Abbreviation += message.Client.FirstName.ToCharArray()[0];

            clientRepository.UpdateAbbreviation(clientFromDb);
        }

        Sender.Tell(clientFromDb);

        //Audit logic for newly added agreements
        if (!addedAgreementsIds.Any()) return;

        StoreAuditDataOnAgreementsInsert(
            message,
            _agreementRepositoriesFactory.NewAgreementRepository(connection),
            addedAgreementsIds,
            clientFromDb.NetUid
        );

        //Audit logic for newly added discounts
        StoreAuditDataOnDiscountsInsert(
            message,
            _productRepositoriesFactory,
            connection,
            message.Client.ClientAgreements.Select(c => c.Id),
            clientFromDb.NetUid
        );
    }

    private void ProcessUpdateClientMessage(UpdateClientMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);
            IRegionCodeRepository regionCodeRepository = _regionRepositoriesFactory.NewRegionCodeRepository(connection);

            if (message.Client.Region != null) {
                message.Client.RegionId = message.Client.Region.Id;
            } else {
                message.Client.RegionId = null;
                message.Client.RegionCodeId = null;
            }


            //Store client role information
            if (message.Client.ClientInRole != null) {
                message.Client.ClientInRole.ClientId = message.Client.Id;

                if (!message.Client.ClientInRole.ClientTypeId.Equals(message.Client.ClientInRole.ClientType.Id)) {
                    _clientRepositoriesFactory.NewClientAgreementRepository(connection).RemoveAllByClientId(message.Client.Id);

                    _clientRepositoriesFactory.NewClientUserProfileRepository(connection).RemoveAllByClientId(message.Client.Id);
                }

                message.Client.ClientInRole.ClientTypeId = message.Client.ClientInRole.ClientType.Id;
                message.Client.ClientInRole.ClientTypeRoleId = message.Client.ClientInRole.ClientTypeRole.Id;
                message.Client.OrderExpireDays = message.Client.ClientInRole.ClientTypeRole.OrderExpireDays;

                if (message.Client.ClientInRole.IsNew())
                    _clientRepositoriesFactory.NewClientInRoleRepository(connection).Add(message.Client.ClientInRole);
                else
                    _clientRepositoriesFactory.NewClientInRoleRepository(connection).Update(message.Client.ClientInRole);

                //If client type is Provider storing provider only related info
                if (message.Client.ClientInRole.ClientType.Type.Equals(ClientTypeType.Provider)) {
                    if (message.Client.ClientBankDetails != null) {
                        if (message.Client.ClientBankDetails.ClientBankDetailIbanNo?.Currency != null &&
                            !message.Client.ClientBankDetails.ClientBankDetailIbanNo.Currency.IsNew()) {
                            message.Client.ClientBankDetails.ClientBankDetailIbanNo.CurrencyId =
                                message.Client.ClientBankDetails.ClientBankDetailIbanNo.Currency.Id;

                            if (message.Client.ClientBankDetails.ClientBankDetailIbanNo.IsNew())
                                message.Client.ClientBankDetails.ClientBankDetailIbanNoId = _clientRepositoriesFactory
                                    .NewClientBankDetailIbanNoRepository(connection)
                                    .Add(message.Client.ClientBankDetails.ClientBankDetailIbanNo);
                            else
                                _clientRepositoriesFactory
                                    .NewClientBankDetailIbanNoRepository(connection)
                                    .Update(message.Client.ClientBankDetails.ClientBankDetailIbanNo);
                        }

                        if (message.Client.ClientBankDetails.AccountNumber?.Currency != null &&
                            !message.Client.ClientBankDetails.AccountNumber.Currency.IsNew()) {
                            if (message.Client.ClientBankDetails.AccountNumber.Currency != null)
                                message.Client.ClientBankDetails.AccountNumber.CurrencyId = message.Client.ClientBankDetails.AccountNumber.Currency.Id;

                            if (message.Client.ClientBankDetails.AccountNumber.IsNew())
                                message.Client.ClientBankDetails.AccountNumberId = _clientRepositoriesFactory
                                    .NewClientBankDetailAccountNumberRepository(connection)
                                    .Add(message.Client.ClientBankDetails.AccountNumber);
                            else
                                _clientRepositoriesFactory
                                    .NewClientBankDetailAccountNumberRepository(connection)
                                    .Update(message.Client.ClientBankDetails.AccountNumber);
                        }

                        if (message.Client.ClientBankDetails.IsNew())
                            message.Client.ClientBankDetailsId = _clientRepositoriesFactory
                                .NewClientBankDetailsRepository(connection)
                                .Add(message.Client.ClientBankDetails);
                        else
                            _clientRepositoriesFactory
                                .NewClientBankDetailsRepository(connection)
                                .Update(message.Client.ClientBankDetails);
                    }

                    if (message.Client.Country != null) message.Client.CountryId = message.Client.Country.Id;

                    if (message.Client.TermsOfDelivery != null) message.Client.TermsOfDeliveryId = message.Client.TermsOfDelivery.Id;

                    if (message.Client.PackingMarking != null) message.Client.PackingMarkingId = message.Client.PackingMarking.Id;

                    if (message.Client.PackingMarkingPayment != null) message.Client.PackingMarkingPaymentId = message.Client.PackingMarkingPayment.Id;

                    if (message.Client.ClientContractDocuments.Any(d => d.IsNew()))
                        _clientRepositoriesFactory
                            .NewClientContractDocumentRepository(connection)
                            .Add(
                                message
                                    .Client
                                    .ClientContractDocuments
                                    .Where(d => d.IsNew())
                                    .Select(d => {
                                        d.ClientId = message.Client.Id;

                                        return d;
                                    })
                            );
                }
            }

            if (message.Client.RegionCode != null)
                if (message.Client.ClientInRole == null || !message.Client.ClientInRole.ClientType.Type.Equals(ClientTypeType.Provider)) {
                    IRegionRepository regionRepository = _regionRepositoriesFactory.NewRegionRepository(connection);

                    if (message.Client.RegionCode.IsNew()) {
                        //1230 if
                        if (string.IsNullOrEmpty(message.Client.RegionCode.Value)) {
                            message.Client.RegionCodeId =
                                regionCodeRepository
                                    .Add(message.Client.RegionCode);
                        } else {
                            if (string.IsNullOrEmpty(message.Client.RegionCode.Value)) throw new Exception(ClientResourceNames.EMPTY_REGION_CODE);

                            if (message.Client.RegionCode.Value.Length > 10) throw new Exception(ClientResourceNames.TO_LONG_REGION_CODE);

                            Regex regex = new(@"^([A-z]{2,5})(\d{1,7})$");

                            Match match = regex.Match(message.Client.RegionCode.Value);

                            if (!match.Success) throw new Exception(ClientResourceNames.INVALID_REGION_CODE);

                            string region = match.Groups[1].Value;
                            string regionCode = match.Groups[2].Value;

                            Region regionFromDb = regionRepository.GetByName(region);

                            if (regionFromDb == null) {
                                regionFromDb = new Region {
                                    Name = region.ToUpper()
                                };

                                regionFromDb.Id = regionRepository.Add(regionFromDb);
                            }

                            bool isAvailable = !regionCodeRepository.IsAssignedToAnyContact($"{region}{regionCode}");

                            if (!isAvailable) throw new Exception(ClientResourceNames.UNAVAILABLE_REGION_CODE);
                            if (regionFromDb == null) {
                                message.Client.RegionId = null;

                                message.Client.RegionCode.RegionId = 0;
                            } else {
                                message.Client.RegionId = regionFromDb.Id;

                                message.Client.RegionCode.RegionId = regionFromDb.Id;
                            }

                            message.Client.RegionCodeId =
                                regionCodeRepository
                                    .Add(message.Client.RegionCode);
                        }
                    } else {
                        //1215
                        //if (string.IsNullOrEmpty(message.Client.RegionCode.Value)) {
                        //    throw new Exception(ClientResourceNames.EMPTY_REGION_CODE);
                        //}

                        RegionCode regionCodeFromDb = regionCodeRepository.GetById(message.Client.RegionCode.Id);

                        if (regionCodeFromDb.Value.ToUpper().Equals(message.Client.RegionCode.Value.ToUpper())) {
                            if (message.Client.Region != null) message.Client.RegionCodeId = message.Client.RegionCode.Id;
                            regionCodeRepository.Update(message.Client.RegionCode);
                        } else {
                            if (message.Client.RegionCode.Value.Length > 10) throw new Exception(ClientResourceNames.TO_LONG_REGION_CODE);

                            Regex regex = new(@"^([A-z]{2,5})(\d{1,7})$");

                            Match match = regex.Match(message.Client.RegionCode.Value);

                            //1215
                            //if (!match.Success) {
                            //    throw new Exception(ClientResourceNames.INVALID_REGION_CODE);
                            //}

                            string region = match.Groups[1].Value;
                            string regionCode = match.Groups[2].Value;

                            Region regionFromDb = regionRepository.GetByName(region);
                            //1230
                            if (regionFromDb == null) {
                                regionFromDb = new Region {
                                    Name = region.ToUpper()
                                };

                                regionFromDb.Id = regionRepository.Add(regionFromDb);
                            }

                            message.Client.RegionId = regionFromDb.Id;

                            message.Client.RegionCode.RegionId = regionFromDb.Id;

                            message.Client.RegionCodeId = message.Client.RegionCode.Id;

                            regionCodeRepository.Update(message.Client.RegionCode);
                        }
                    }
                }

            //Update validity of ClientShoppingCart if ClearCartAfterDays was changed and cart exists
            Client clientFromDb = clientRepository.GetByNetIdWithoutIncludes(message.Client.NetUid);

            if (clientFromDb != null) {
                if (clientFromDb.IsTemporaryClient) {
                    clientRepository.SetTemporaryClientById(clientFromDb.Id);

                    message.Client.IsTemporaryClient = false;
                }

                if (!clientFromDb.ClearCartAfterDays.Equals(message.Client.ClearCartAfterDays)) {
                    IClientShoppingCartRepository clientShoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);

                    ClientShoppingCart shoppingCart = clientShoppingCartRepository.GetByClientNetId(message.Client.NetUid, false);

                    if (shoppingCart != null) {
                        shoppingCart.ValidUntil = DateTime.Now.AddDays(message.Client.ClearCartAfterDays);

                        clientShoppingCartRepository.UpdateValidUntilDate(shoppingCart);
                    }

                    shoppingCart = clientShoppingCartRepository.GetByClientNetId(message.Client.NetUid, true);

                    if (shoppingCart != null) {
                        shoppingCart.ValidUntil = DateTime.Now.AddDays(message.Client.ClearCartAfterDays);

                        clientShoppingCartRepository.UpdateValidUntilDate(shoppingCart);
                    }
                }
            }

            if (!string.IsNullOrEmpty(message.Client.LastName) || !string.IsNullOrEmpty(message.Client.FirstName)) {
                message.Client.Abbreviation = string.Empty;

                if (!string.IsNullOrEmpty(message.Client.LastName)) message.Client.Abbreviation += message.Client.LastName.ToCharArray()[0];
                if (!string.IsNullOrEmpty(message.Client.FirstName)) message.Client.Abbreviation += message.Client.FirstName.ToCharArray()[0];
            }

            clientRepository.Update(message.Client);

            //Store perfect client properties if any selected
            if (message.Client.PerfectClients.Any(p => p.IsSelected)) {
                IClientPerfectClientRepository clientPerfectClientRepository = _clientRepositoriesFactory.NewClientPerfectClientRepository(connection);

                IEnumerable<ClientPerfectClient> clientPerfectClients = clientPerfectClientRepository.GetAllByClientId(message.Client.Id);

                clientPerfectClientRepository.Update(
                    message.Client.PerfectClients.Where(p => p.IsSelected)
                        .Where(perfectClient => clientPerfectClients.Any(c => c.PerfectClientId.Equals(perfectClient.Id)))
                        .Select(perfectClient => {
                            ClientPerfectClient clientPerfectClient = clientPerfectClients.First(c => c.PerfectClientId.Equals(perfectClient.Id));

                            clientPerfectClient.Value = perfectClient.Value;

                            if (perfectClient.Values.Any()) clientPerfectClient.PerfectClientValueId = perfectClient.Values.First(c => c.IsSelected).Id;

                            return clientPerfectClient;
                        })
                );

                clientPerfectClientRepository.Add(
                    message.Client.PerfectClients.Where(p => p.IsSelected)
                        .Where(perfectClient => !clientPerfectClients.Any(c => c.PerfectClientId.Equals(perfectClient.Id)))
                        .Select(perfectClient => {
                            if (perfectClient.Type.Equals(PerfectClientType.Toggle))
                                return new ClientPerfectClient {
                                    PerfectClientId = perfectClient.Id,
                                    IsChecked = true,
                                    ClientId = message.Client.Id,
                                    PerfectClientValueId = perfectClient.Values.FirstOrDefault(a => a.IsSelected)?.Id != null
                                        ? perfectClient.Values.FirstOrDefault(a => a.IsSelected)?.Id
                                        : perfectClient.Values.FirstOrDefault()?.Id,
                                    Value = perfectClient.Value
                                };
                            return new ClientPerfectClient {
                                PerfectClientId = perfectClient.Id,
                                IsChecked = true,
                                ClientId = message.Client.Id,
                                Value = perfectClient.Value
                            };
                        })
                );

                clientPerfectClientRepository.Remove(
                    clientPerfectClients
                        .Where(c =>
                            !message
                                .Client
                                .PerfectClients
                                .Where(p => p.IsSelected)
                                .Any(p => p.Id.Equals(c.PerfectClientId))
                        )
                );
            }

            Tuple<List<ClientAgreement>, List<ClientAgreement>, List<Agreement>, List<Agreement>> agreementsUpdateResult;

            //Create/Update/Delete client agreements
            if (message.Client.ClientAgreements.Any()) {
                IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);
                IProviderPricingRepository providerPricingRepository = _pricingRepositoriesFactory.NewProviderPricingRepository(connection);
                IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
                IOrganizationRepository organizationRepository = _organizationRepositoriesFactory.NewOrganizationRepository(connection);

                List<ClientAgreement> clientAgreements = clientAgreementRepository.GetAllByClientIdWithoutIncludes(message.Client.Id);

                List<ClientAgreement> clientAgreementsToDelete = clientAgreements.Where(c => !message.Client.ClientAgreements.Any(p => p.Id.Equals(c.Id))).ToList();

                if (clientAgreementsToDelete.Any()) clientAgreementRepository.Remove(clientAgreementsToDelete);

                List<Agreement> agreementsBeforeUpdate = new();
                List<Agreement> agreementsAfterUpdate = new();

                List<ClientAgreement> clientAgreementsToAdd = new();

                foreach (ClientAgreement agreement in message.Client.ClientAgreements) {
                    agreement.Agreement.CurrencyId = agreement.Agreement.Currency?.Id;
                    agreement.Agreement.OrganizationId = agreement.Agreement.Organization?.Id;

                    if (clientAgreementRepository.GetSelectedByClientNetId(message.Client.NetUid) == null &&
                        !clientAgreementsToAdd.Any(a => a.Agreement.IsSelected))
                        agreement.Agreement.IsSelected = true;

                    if (agreement.Agreement.ProviderPricing != null) {
                        agreement.Agreement.ProviderPricing.CurrencyId = agreement.Agreement.ProviderPricing.Currency?.Id;
                        agreement.Agreement.ProviderPricing.BasePricingId = agreement.Agreement.ProviderPricing.Pricing?.Id;

                        if (agreement.Agreement.ProviderPricing.IsNew())
                            agreement.Agreement.ProviderPricingId = providerPricingRepository.Add(agreement.Agreement.ProviderPricing);
                        else
                            providerPricingRepository.Update(agreement.Agreement.ProviderPricing);
                    } else {
                        agreement.Agreement.PricingId = agreement.Agreement.Pricing?.Id;
                        agreement.Agreement.PromotionalPricingId = agreement.Agreement.PromotionalPricing?.Id;

                        if (agreement.Agreement.PromotionalPricingId.HasValue && agreement.Agreement.PromotionalPricingId.Value.Equals(0))
                            agreement.Agreement.PromotionalPricingId = null;
                    }

                    if (agreement.Agreement.Currency != null) agreement.Agreement.CurrencyId = agreement.Agreement.Currency.Id;

                    if (agreement.Agreement.TaxAccountingScheme != null) agreement.Agreement.TaxAccountingSchemeId = agreement.Agreement.TaxAccountingScheme.Id;

                    if (agreement.Agreement.AgreementTypeCivilCode != null)
                        agreement.Agreement.AgreementTypeCivilCodeId = agreement.Agreement.AgreementTypeCivilCode.Id;

                    if (agreement.IsNew()) {
                        if (agreement.Agreement.OrganizationId.HasValue) {
                            Organization organization = organizationRepository.GetById(agreement.Agreement.OrganizationId.Value);
                            Agreement lastRecord = agreementRepository.GetLastRecordByOrganizationId(organization.Id);

                            agreement.Agreement.Number = organization.Code;

                            try {
                                if (lastRecord != null && !string.IsNullOrEmpty(lastRecord.Number))
                                    agreement.Agreement.Number +=
                                        string.Format(
                                            "{0:D5}",
                                            Convert.ToInt64(
                                                lastRecord
                                                    .Number
                                                    .Substring(
                                                        organization.Code.Length,
                                                        lastRecord.Number.Length - organization.Code.Length)
                                            ) + 1);
                                else
                                    agreement.Agreement.Number += string.Format(
                                        "{0:D5}",
                                        1
                                    );
                            } catch (FormatException) {
                                agreement.Agreement.Number += string.Format(
                                    "{0:D5}",
                                    1
                                );
                            }
                        }

                        agreement.AgreementId = agreementRepository.Add(agreement.Agreement);
                        agreement.ClientId = message.Client.Id;

                        clientAgreementsToAdd.Add(agreement);
                    } else {
                        agreementsBeforeUpdate.Add(agreementRepository.GetByNetId(agreement.Agreement.NetUid));

                        agreementsAfterUpdate.Add(agreement.Agreement);

                        agreementRepository.Update(agreement.Agreement);
                    }
                }

                if (clientAgreementsToAdd.Any())
                    foreach (ClientAgreement agreement in clientAgreementsToAdd)
                        agreement.Id = clientAgreementRepository.Add(agreement);

                agreementsUpdateResult = new Tuple<List<ClientAgreement>, List<ClientAgreement>, List<Agreement>, List<Agreement>>(
                    clientAgreementsToDelete,
                    clientAgreementsToAdd,
                    agreementsBeforeUpdate,
                    agreementsAfterUpdate
                );
            } else {
                IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

                List<ClientAgreement> clientAgreementsToDelete = clientAgreementRepository.GetAllByClientIdWithoutIncludes(message.Client.Id);

                if (clientAgreementsToDelete.Any()) clientAgreementRepository.Remove(clientAgreementsToDelete);

                agreementsUpdateResult = new Tuple<List<ClientAgreement>, List<ClientAgreement>, List<Agreement>, List<Agreement>>(
                    clientAgreementsToDelete,
                    new List<ClientAgreement>(),
                    new List<Agreement>(),
                    new List<Agreement>()
                );
            }

            //Create/Update/Delete client managers
            if (message.Client.ClientManagers.Any()) {
                IClientUserProfileRepository clientUserProfileRepository = _clientRepositoriesFactory.NewClientUserProfileRepository(connection);

                List<ClientUserProfile> clientManagers = clientUserProfileRepository.GetAllByClientId(message.Client.Id);

                List<ClientUserProfile> clientManagersToDelete = clientManagers.Where(c => !message.Client.ClientManagers.Any(p => p.Id.Equals(c.Id))).ToList();

                if (clientManagersToDelete.Any()) clientUserProfileRepository.Remove(clientManagersToDelete);

                List<ClientUserProfile> clientManagersToAdd = new();
                List<ClientUserProfile> clientManagersToUpdate = new();

                foreach (ClientUserProfile clientUserProfile in message.Client.ClientManagers)
                    if (clientUserProfile.IsNew()) {
                        clientUserProfile.ClientId = message.Client.Id;
                        clientUserProfile.UserProfileId = clientUserProfile.UserProfile.Id;

                        clientManagersToAdd.Add(clientUserProfile);
                    } else {
                        clientUserProfile.UserProfileId = clientUserProfile.UserProfile.Id;

                        clientManagersToUpdate.Add(clientUserProfile);
                    }

                if (clientManagersToAdd.Any()) clientUserProfileRepository.Add(clientManagersToAdd);

                if (clientManagersToUpdate.Any()) clientUserProfileRepository.Update(clientManagersToUpdate);
            } else {
                _clientRepositoriesFactory.NewClientUserProfileRepository(connection).RemoveAllByClientId(message.Client.Id);
            }

            //Update discounts logic

            IProductGroupDiscountRepository productGroupDiscountRepository = _productRepositoriesFactory.NewProductGroupDiscountRepository(connection);

            List<ProductGroupDiscount> discountsToAdd = new();
            List<ProductGroupDiscount> discountsToUpdate = new();

            agreementsUpdateResult.Item2.Where(a => a.ProductGroupDiscounts.Any()).ToList().ForEach(a => {
                a.ProductGroupDiscounts.ToList().ForEach(d => {
                    if (d.IsNew()) {
                        d.ClientAgreement = a;
                        d.ClientAgreementId = d.ClientAgreement.Id;
                        d.ProductGroupId = d.ProductGroup.Id;

                        discountsToAdd.Add(d);
                    } else {
                        discountsToUpdate.Add(d);
                    }

                    if (d.SubProductGroupDiscounts.Any())
                        d.SubProductGroupDiscounts.ToList().ForEach(sub => {
                            if (sub.IsNew()) {
                                sub.ClientAgreement = a;
                                sub.ClientAgreementId = sub.ClientAgreement.Id;
                                sub.ProductGroupId = sub.ProductGroup.Id;

                                discountsToAdd.Add(sub);
                            } else {
                                discountsToUpdate.Add(sub);
                            }
                        });
                });
            });

            agreementsUpdateResult.Item4.ForEach(updated => {
                ClientAgreement agreement = message.Client.ClientAgreements.First(a => a.AgreementId.Equals(updated.Id));

                agreement.ProductGroupDiscounts.ToList().ForEach(d => {
                    if (d.IsNew()) {
                        d.ClientAgreement = agreement;
                        d.ClientAgreementId = d.ClientAgreement.Id;
                        d.ProductGroupId = d.ProductGroup.Id;

                        discountsToAdd.Add(d);
                    } else {
                        discountsToUpdate.Add(d);
                    }

                    if (d.SubProductGroupDiscounts.Any())
                        d.SubProductGroupDiscounts.ToList().ForEach(sub => {
                            if (sub.IsNew()) {
                                sub.ClientAgreement = agreement;
                                sub.ClientAgreementId = sub.ClientAgreement.Id;
                                sub.ProductGroupId = sub.ProductGroup.Id;

                                discountsToAdd.Add(sub);
                            } else {
                                discountsToUpdate.Add(sub);
                            }
                        });
                });
            });

            List<ProductGroupDiscount> discountsBeforeUpdate = new();
            List<ProductGroupDiscount> discountsAfterUpdate = new();

            if (discountsToUpdate.Any()) {
                IEnumerable<long> discountsToUpdateIds = discountsToUpdate.Select(d => d.Id);

                discountsBeforeUpdate = productGroupDiscountRepository.GetAllByClientAgreementIds(discountsToUpdateIds);

                productGroupDiscountRepository.Update(discountsToUpdate);

                discountsAfterUpdate = productGroupDiscountRepository.GetAllByClientAgreementIds(discountsToUpdateIds);
            }

            if (discountsToAdd.Any())
                foreach (ProductGroupDiscount discount in discountsToAdd)
                    discount.Id = productGroupDiscountRepository.Add(discount);

            Tuple<List<ProductGroupDiscount>, List<ProductGroupDiscount>, List<ProductGroupDiscount>> discountsUpdateResult
                = new(
                    discountsToAdd,
                    discountsBeforeUpdate,
                    discountsAfterUpdate
                );

            if (message.Client.ServicePayers.Any()) {
                IServicePayerRepository servicePayerRepository = _servicePayerRepositoryFactory.New(connection);

                List<ServicePayer> payersFromDb = servicePayerRepository.GetAllByClientId(message.Client.Id);

                List<ServicePayer> payersToDelete = payersFromDb.Where(d => !message.Client.ServicePayers.Any(p => p.Id.Equals(d.Id))).ToList();

                List<ServicePayer> payersToAdd = new();
                List<ServicePayer> payersToUpdate = new();

                foreach (ServicePayer payer in message.Client.ServicePayers)
                    if (payer.IsNew()) {
                        payer.ClientId = message.Client.Id;

                        payersToAdd.Add(payer);
                    } else {
                        payersToUpdate.Add(payer);
                    }

                if (payersToDelete.Any()) servicePayerRepository.Remove(payersToDelete);
                if (payersToUpdate.Any()) servicePayerRepository.Update(payersToUpdate);
                if (payersToAdd.Any()) servicePayerRepository.Add(payersToAdd);
            } else {
                IServicePayerRepository servicePayerRepository = _servicePayerRepositoryFactory.New(connection);

                List<ServicePayer> payersFromDb = servicePayerRepository.GetAllByClientId(message.Client.Id);

                if (payersFromDb.Any()) servicePayerRepository.Remove(payersFromDb);
            }

            //UpdateAbbreviation
            if (!string.IsNullOrEmpty(message.Client.LastName) || !string.IsNullOrEmpty(message.Client.FirstName)) {
                if (!string.IsNullOrEmpty(message.Client.LastName)) clientFromDb.Abbreviation = message.Client.LastName.ToCharArray()[0].ToString();
                if (!string.IsNullOrEmpty(message.Client.FirstName)) clientFromDb.Abbreviation += message.Client.FirstName.ToCharArray()[0];

                clientRepository.UpdateAbbreviation(clientFromDb);
            }

            Sender.Tell(clientRepository.GetByNetId(message.Client.NetUid));

            //Store audit on all operations with discounts after complex client update
            StoreAuditDataOnDiscountsUpdate(message, discountsUpdateResult);

            //Store audit on all operations with agreements after complex client update
            StoreAuditDataOnAgreementsUpdate(message, _agreementRepositoriesFactory, connection, agreementsUpdateResult);

            _clientRepositoriesFactory.NewClientRegistrationTaskRepository(connection).SetDoneByClientId(message.Client.Id);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessDeleteClientMessage(DeleteClientMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);
        IIdentityRepository identityRepository = _identityRepositoriesFactory.NewIdentityRepository();

        clientRepository.Remove(message.NetId);
        identityRepository.DeleteUserByNetId(message.NetId.ToString());

        Client client = clientRepository.GetByNetId(message.NetId);

        if (client != null) {
            if (client.RegionCode != null) _regionRepositoriesFactory.NewRegionCodeRepository(connection).Remove(client.RegionCode.NetUid);

            if (client.ClientAgreements.Any()) _clientRepositoriesFactory.NewClientAgreementRepository(connection).Remove(client.ClientAgreements);
        }
    }

    private void ProcessUpdateClientPasswordMessage(UpdateClientPasswordMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);

        try {
            if (string.IsNullOrEmpty(message.Password)) {
                Sender.Tell(new Tuple<bool, string>(false, ClientResourceNames.INVALID_DATA));
                ;
                return;
            }

            Client client = clientRepository.GetByNetIdWithRegionCode(message.ClientNetId);

            if (client != null) {
                if (!string.IsNullOrEmpty(message.MobileNumber)) {
                    client.MobileNumber = message.MobileNumber;
                    client.ClientNumber = message.MobileNumber;

                    clientRepository.UpdateNumbers(client);
                }

                IIdentityRepository identityRepository = _identityRepositoriesFactory.NewIdentityRepository();

                UserIdentity userIdentity;

                if (!string.IsNullOrEmpty(client.MobileNumber)) {
                    userIdentity = identityRepository.GetUserName(client.MobileNumber).Result;
                } else if (!string.IsNullOrEmpty(client.EmailAddress)) {
                    userIdentity = identityRepository.GetUserName(client.EmailAddress).Result;
                } else if (client.RegionCode != null) {
                    userIdentity = identityRepository.GetUserName(client.RegionCode.Value).Result;
                } else {
                    Sender.Tell(new Tuple<bool, string>(false, ClientResourceNames.INVALID_DATA));
                    ;
                    return;
                }

                if (!string.IsNullOrEmpty(client.MobileNumber) && userIdentity == null) userIdentity = identityRepository.GetUserName(client.MobileNumber).Result;
                if (!string.IsNullOrEmpty(client.EmailAddress) && userIdentity == null) userIdentity = identityRepository.GetUserName(client.EmailAddress).Result;
                if (client.RegionCode != null && userIdentity == null) userIdentity = identityRepository.GetUserName(client.RegionCode.Value).Result;


                //UserIdentity userIdentity = identityRepository.GetUserByNetId(message.ClientNetId.ToString()).Result;

                if (userIdentity != null) {
                    IdentityResponse response = identityRepository.ResetPassword(userIdentity.NetId.ToString(), message.Password).Result;

                    Sender.Tell(
                        response.Succeeded
                            ? new Tuple<bool, string>(true, "Success")
                            : new Tuple<bool, string>(false, response.Errors.First().Description)
                    );
                } else {
                    UserIdentity user = new() {
                        Email = client.EmailAddress,
                        UserName = !string.IsNullOrEmpty(message.MobileNumber)
                            ? message.MobileNumber
                            : !string.IsNullOrEmpty(message.Login)
                                ? message.Login
                                : !string.IsNullOrEmpty(client.EmailAddress)
                                    ? client.EmailAddress
                                    : client.RegionCode.Value,
                        PhoneNumber = client.MobileNumber,
                        NetId = client.NetUid,
                        Region = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                        UserType = IdentityUserType.Client
                    };

                    IdentityResponse response = identityRepository.CreateUser(user, message.Password, false).Result;

                    if (response.Succeeded) {
                        identityRepository.AddUserRoleAndClaims(user, IdentityRoles.ClientUa).Wait();

                        Sender.Tell(new Tuple<bool, string>(true, "Success"));
                    } else {
                        Sender.Tell(new Tuple<bool, string>(false, response.Errors.First().Description));
                    }
                }
            } else {
                Sender.Tell(new Tuple<bool, string>(false, "Such client does not exists"));
            }
        } catch (Exception exc) {
            Sender.Tell(new Tuple<bool, string>(false, exc.Message));
        }
    }

    private void ProcessSetIsForRetailMessage(SetIsForRetailMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);

            Client currentClient = clientRepository.GetRetailClient();
            if (currentClient != null && currentClient.NetUid != message.NetId) clientRepository.DeselectIsForRetailByNetId(currentClient.NetUid);

            clientRepository.SetIsForRetailByNetId(message.NetId);

            Sender.Tell(clientRepository.GetAllShopClients());
        } catch (Exception exc) {
            Sender.Tell(new Tuple<bool, string>(false, exc.Message));
        }
    }

    private void StoreAuditDataOnDiscountsInsert(
        AddClientMessage message,
        IProductRepositoriesFactory productRepositoriesFactory,
        IDbConnection connection,
        IEnumerable<long> addedClientAgreementsIds,
        Guid clientNetId) {
        if (!addedClientAgreementsIds.Any()) return;

        List<ProductGroupDiscount> addedDiscounts =
            productRepositoriesFactory.NewProductGroupDiscountRepository(connection).GetAllByClientAgreementIds(addedClientAgreementsIds);

        foreach (ProductGroupDiscount discount in addedDiscounts) {
            List<AuditEntityProperty> newProperties = new() {
                new AuditEntityProperty {
                    Type = AuditEntityPropertyType.New,
                    Name = "ProductGroup",
                    Value = discount.ProductGroup.Name
                },
                new AuditEntityProperty {
                    Type = AuditEntityPropertyType.New,
                    Name = "Agreement",
                    Value = discount.ClientAgreement.Agreement.Name
                }
            };

            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                new RetrieveAndStoreAuditDataMessage(
                    message.UpdatedByNetId,
                    clientNetId,
                    "Client.ProductGroupDiscount",
                    discount,
                    null,
                    newProperties
                )
            );
        }
    }

    private void StoreAuditDataOnDiscountsUpdate(
        UpdateClientMessage message,
        Tuple<List<ProductGroupDiscount>, List<ProductGroupDiscount>, List<ProductGroupDiscount>> discountsUpdateResult) {
        foreach (ProductGroupDiscount discount in discountsUpdateResult.Item1) {
            List<AuditEntityProperty> newProperties = new() {
                new AuditEntityProperty {
                    Type = AuditEntityPropertyType.New,
                    Name = "ProductGroup",
                    Value = discount.ProductGroup.Name
                },
                new AuditEntityProperty {
                    Type = AuditEntityPropertyType.New,
                    Name = "Agreement",
                    Value = discount.ClientAgreement.Agreement.Name
                }
            };

            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                new RetrieveAndStoreAuditDataMessage(
                    message.UpdatedByNetId,
                    message.Client.NetUid,
                    "Client.ProductGroupDiscount",
                    discount,
                    null,
                    newProperties
                )
            );
        }

        for (int i = 0; i < discountsUpdateResult.Item2.Count; i++)
            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                new RetrieveAndStoreAuditDataMessage(
                    message.UpdatedByNetId,
                    message.Client.NetUid,
                    "Client.ProductGroupDiscount",
                    discountsUpdateResult.Item3[i],
                    discountsUpdateResult.Item2[i]
                )
            );
    }

    private void StoreAuditDataOnAgreementsInsert(
        AddClientMessage message,
        IAgreementRepository agreementRepository,
        List<long> addedAgreementsIds,
        Guid clientNetId) {
        List<Agreement> addedAgreements = agreementRepository.GetAllByIds(addedAgreementsIds);

        foreach (Agreement agreement in addedAgreements) {
            List<AuditEntityProperty> newProperties = new();

            if (agreement.CurrencyId != null)
                newProperties.Add(new AuditEntityProperty {
                    Type = AuditEntityPropertyType.New,
                    Name = "Currency",
                    Value = agreement.Currency.Name
                });
            if (agreement.OrganizationId != null)
                newProperties.Add(new AuditEntityProperty {
                    Type = AuditEntityPropertyType.New,
                    Name = "Organization",
                    Value = agreement.Organization.Name
                });
            if (agreement.PricingId != null)
                newProperties.Add(new AuditEntityProperty {
                    Type = AuditEntityPropertyType.New,
                    Name = "Pricing",
                    Value = agreement.Pricing.Name
                });
            if (agreement.ProviderPricingId != null)
                newProperties.Add(new AuditEntityProperty {
                    Type = AuditEntityPropertyType.New,
                    Name = "ProviderPricing",
                    Value = agreement.ProviderPricing.Name
                });

            if (newProperties.Any())
                ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(new RetrieveAndStoreAuditDataMessage(
                    message.UpdatedByNetId,
                    clientNetId,
                    "Client.Agreement",
                    agreement,
                    null,
                    newProperties
                ));
            else
                ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(new RetrieveAndStoreAuditDataMessage(
                    message.UpdatedByNetId,
                    clientNetId,
                    "Client.Agreement",
                    agreement
                ));
        }
    }

    private void StoreAuditDataOnAgreementsUpdate(
        UpdateClientMessage message,
        IAgreementRepositoriesFactory agreementRepositoriesFactory,
        IDbConnection connection,
        Tuple<List<ClientAgreement>, List<ClientAgreement>, List<Agreement>, List<Agreement>> agreementsUpdateResult) {
        foreach (ClientAgreement deleted in agreementsUpdateResult.Item1)
            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(new RetrieveAndStoreAuditDataMessage(
                message.UpdatedByNetId,
                message.Client.NetUid,
                "Client.Agreement",
                deleted,
                deleted,
                null,
                null,
                true
            ));

        if (agreementsUpdateResult.Item2.Any()) {
            List<Agreement> addedAgreements = agreementRepositoriesFactory.NewAgreementRepository(connection)
                .GetAllByIds(agreementsUpdateResult.Item2.Select(a => a.AgreementId).ToList());

            foreach (Agreement agreement in addedAgreements) {
                List<AuditEntityProperty> newProperties = new();

                if (agreement.CurrencyId != null)
                    newProperties.Add(new AuditEntityProperty {
                        Type = AuditEntityPropertyType.New,
                        Name = "Currency",
                        Value = agreement.Currency.Name
                    });
                if (agreement.OrganizationId != null)
                    newProperties.Add(new AuditEntityProperty {
                        Type = AuditEntityPropertyType.New,
                        Name = "Organization",
                        Value = agreement.Organization.Name
                    });
                if (agreement.PricingId != null)
                    newProperties.Add(new AuditEntityProperty {
                        Type = AuditEntityPropertyType.New,
                        Name = "Pricing",
                        Value = agreement.Pricing.Name
                    });
                if (agreement.ProviderPricingId != null)
                    newProperties.Add(new AuditEntityProperty {
                        Type = AuditEntityPropertyType.New,
                        Name = "ProviderPricing",
                        Value = agreement.ProviderPricing.Name
                    });

                if (newProperties.Any())
                    ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(new RetrieveAndStoreAuditDataMessage(
                        message.UpdatedByNetId,
                        message.Client.NetUid,
                        "Client.Agreement",
                        agreement,
                        null,
                        newProperties
                    ));
                else
                    ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(new RetrieveAndStoreAuditDataMessage(
                        message.UpdatedByNetId,
                        message.Client.NetUid,
                        "Client.Agreement",
                        agreement
                    ));
            }
        }

        for (int i = 0; i < agreementsUpdateResult.Item3.Count; i++) {
            Agreement beforeUpdateAgreement = agreementsUpdateResult.Item3[i];
            Agreement updatedAgreement = agreementsUpdateResult.Item4[i];

            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(new RetrieveAndStoreAuditDataMessage(
                message.UpdatedByNetId,
                message.Client.NetUid,
                "Client.Agreement",
                updatedAgreement,
                beforeUpdateAgreement
            ));
        }
    }
}