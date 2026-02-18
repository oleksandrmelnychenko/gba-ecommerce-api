using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Akka.Actor;
using AutoMapper;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.EntityHelpers.GbaDataExportModels;
using GBA.Domain.EntityHelpers.GbaDataExportModels.DepreciatedOrders;
using GBA.Domain.EntityHelpers.GbaDataExportModels.PaidServices;
using GBA.Domain.EntityHelpers.GbaDataExportModels.ProductCapitalizations;
using GBA.Domain.EntityHelpers.GbaDataExportModels.ProductIncomeModels;
using GBA.Domain.EntityHelpers.GbaDataExportModels.ProductTransfers;
using GBA.Domain.EntityHelpers.GbaDataExportModels.ReSales;
using GBA.Domain.EntityHelpers.GbaDataExportModels.SaleReturns;
using GBA.Domain.EntityHelpers.GbaDataExportModels.Sales;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Messages.GbaData;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.Repositories.DepreciatedOrders.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.SaleReturns.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Services.Actors.Sales;
using Client = GBA.Domain.Entities.Clients.Client;
using OrderItem = GBA.Domain.Entities.Sales.OrderItem;

namespace GBA.Services.Actors.GbaData;

public class GbaDataManagementActor : ReceiveActor {
    private readonly IMapper _mapper;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoryFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IDepreciatedRepositoriesFactory _depreciatedRepositoriesFactory;
    private readonly ISaleReturnRepositoriesFactory _saleReturnRepositoriesFactory;
    private readonly IDataSyncRepositoriesFactory _dataSyncRepositoriesFactory;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;
    private const string UahCode = "980";

    public GbaDataManagementActor(
        IMapper mapper,
        IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoryFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IDepreciatedRepositoriesFactory depreciatedRepositoriesFactory,
        ISaleReturnRepositoriesFactory saleReturnRepositoriesFactory,
        IDataSyncRepositoriesFactory dataSyncRepositoriesFactory,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory) {
        _mapper = mapper;
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _saleRepositoryFactory = saleRepositoryFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _depreciatedRepositoriesFactory = depreciatedRepositoriesFactory;
        _saleReturnRepositoriesFactory = saleReturnRepositoriesFactory;
        _dataSyncRepositoriesFactory = dataSyncRepositoriesFactory;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;
        Receive<GetProductsFilteredMessage>(ProcessGetProductsFilteredMessage);

        Receive<GetAllProductsLimitedMessage>(ProcessGetAllProductsLimitedMessage);

        Receive<GetSalesWithNewStatusMessage>(ProcessGetSalesWithNewStatusMessage);

        Receive<GetInvoiceSalesFilteredMessage>(ProcessGetInvoiceSalesFilteredMessage);

        Receive<GetAllProductCapitalizationsForExportMessage>(ProcessGetAllProductCapitalizationsForExportMessage);

        Receive<GetAllSupplyInvoicesForExportMessage>(ProcessGetAllSupplyInvoicesForExportMessage);

        Receive<GetTotalProductsQtyMessage>(ProcessGetTotalProductsQtyMessage);

        Receive<GetAllClientsMessage>(ProcessGetAllClientsMessage);

        Receive<GetAllDepreciatedOrdersForExportMessage>(ProcessGetAllDepreciatedOrdersForExportMessage);

        Receive<GetAllProductTransfersForExportMessage>(ProcessGetAllProductTransfersForExportMessage);

        Receive<GetAllSaleReturnsForExportMessage>(ProcessGetAllSaleReturnsForExportMessage);

        Receive<GetAllServicesMessage>(ProcessGetAllServicesMessage);

        Receive<GetAllReSaleProductsFilteredMessage>(ProcessGetAllReSaleProductsFilteredMessage);

        Receive<GetAllDeliveryDocumentsMessage>(ProcessGetAllDeliveryDocumentsMessage);
    }

    private void ProcessGetAllDeliveryDocumentsMessage(GetAllDeliveryDocumentsMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            List<DeliveryDocumentDto> toReturn = new List<DeliveryDocumentDto>();
            // List<DeliveryProductProtocol> deliveryProductProtocols = _supplyRepositoriesFactory
            //     .NewDeliveryProductProtocolRepository(connection)
            //     .AllFiltered(message.FromDate, message.ToDate);

            IGbaDataExportRepository gbaDataExportRepository = _dataSyncRepositoriesFactory.NewGbaDataExportRepository(connection);

            ICurrencyRepository currencyRepository =
                _currencyRepositoriesFactory.NewCurrencyRepository(connection);

            Currency uah = currencyRepository.GetUAHCurrencyIfExists();
            Currency eur = currencyRepository.GetEURCurrencyIfExists();

            List<PackingList> packingLists = gbaDataExportRepository.GetPackingListForSpecification(message.FromDate, message.ToDate);
            foreach (PackingList packingList in packingLists) {
                SupplyInvoice invoice =
                    gbaDataExportRepository
                        .GetSupplyInvoiceByPackingListNetId(packingList.NetUid);
                GovExchangeRate govExchangeRate =
                    _exchangeRateRepositoriesFactory
                        .NewGovExchangeRateRepository(connection)
                        .GetByCurrencyIdAndCode(uah.Id, eur.Code,
                            invoice.DateCustomDeclaration.HasValue ? invoice.DateCustomDeclaration.Value : invoice.Created);
                
                RecalculateAll(packingList, govExchangeRate.Amount);
                DeliveryDocumentDto deliveryDocumentDto = _mapper.Map<DeliveryDocumentDto>(packingList);
                deliveryDocumentDto.CustomsAgreement = _mapper.Map<SupplyOrganizationAgreementDto>(invoice.SupplyOrganizationAgreement);
                deliveryDocumentDto.Customs = _mapper.Map<SupplyOrganizationDto>(invoice.SupplyOrganization);
                deliveryDocumentDto.DocumentDate = invoice.DateCustomDeclaration ?? invoice.Created;
                deliveryDocumentDto.CustomsNumber = invoice.NumberCustomDeclaration;
                deliveryDocumentDto.Client = _mapper.Map<ExtendedClientDto>(invoice.SupplyOrder.Client);
                deliveryDocumentDto.Agreement = _mapper.Map<ExtendedAgreementGTDDto>(invoice.SupplyOrder.ClientAgreement.Agreement);
                // deliveryDocumentDto.exchangeRate = govExchangeRate.Amount;
                
               
                deliveryDocumentDto.OrganizationName = invoice.SupplyOrder.Organization.Name;
                deliveryDocumentDto.OrganizationUSREOU = invoice.SupplyOrder.Organization.USREOU;
                deliveryDocumentDto.Comment = invoice.Comment;
                long currencyid = (long)invoice.SupplyOrder.ClientAgreement.Agreement.CurrencyId;
                if (currencyid != null) {
                    Currency currencyDb = currencyRepository.GetById(currencyid);
                    if (currencyDb.CodeOneC.Equals(UahCode))
                        deliveryDocumentDto.exchangeRate = 1m;
                    else {
                        GovExchangeRate exchangeRate =
                            _exchangeRateRepositoriesFactory
                                .NewGovExchangeRateRepository(connection)
                                .GetByCurrencyIdAndCode(
                                    uah.Id, eur.Code, invoice.DateCustomDeclaration.HasValue ? invoice.DateCustomDeclaration.Value : invoice.Created);

                        deliveryDocumentDto.exchangeRate = exchangeRate.Amount;
                    }
                    deliveryDocumentDto.Agreement.CurrencyDocument = new CurrencyDto {
                        Code = currencyDb.Code,
                        CodeOneC = currencyDb.CodeOneC,
                        Name = currencyDb.Name
                    };
                }
               
                Currency currencyDocumentDb = currencyRepository.GetById(invoice.SupplyOrganizationAgreement.CurrencyId);
               
                deliveryDocumentDto.CustomsAgreement.Currency  = new CurrencyDto {
                    Code = currencyDocumentDb.Code,
                    CodeOneC = currencyDocumentDb.CodeOneC,
                    Name = currencyDocumentDb.Name
                };
                
                
                toReturn.Add(deliveryDocumentDto);
                //Customs - Таможня
                //CustomsAgreement - договор
                // Фактурана вартість - це Вартість нетто інвойса*курс НБУ на дату МД по валюті договору (тобто це треба обрахувати)
                // paclkingListItem.UnitPrice * govExchangeRate.Value
            }

            Sender.Tell(toReturn);
        } catch (DbException e) {
            Sender.Tell(e.Message);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    public static void RecalculateAll(PackingList packingList, decimal govExchangeRateUahToEur) {
        // reset totals
        packingList.TotalQuantity = 0;
        packingList.TotalNetPrice = 0;
        packingList.TotalGrossPrice = 0;
        packingList.TotalGrossPriceEur = 0;
        packingList.AccountingTotalGrossPrice = 0;
        packingList.AccountingTotalGrossPriceEur = 0;
        packingList.TotalNetWeight = 0;
        packingList.TotalGrossWeight = 0;
        packingList.TotalCustomValue = 0;
        packingList.TotalVatAmount = 0;
        packingList.TotalDuty = 0;

        foreach (PackingListPackageOrderItem item in packingList.PackingListPackageOrderItems) {
            CalculateItem(item, govExchangeRateUahToEur);

            // accumulate totals
            packingList.TotalQuantity += item.Qty;
            packingList.TotalNetPrice += item.TotalNetPrice;
            packingList.TotalGrossPrice += item.TotalGrossPrice;
            packingList.AccountingTotalGrossPrice += item.AccountingTotalGrossPrice;
            packingList.AccountingTotalGrossPriceEur += item.AccountingTotalGrossPriceEur;
            packingList.TotalGrossPriceEur += item.TotalGrossPriceEur;

            packingList.TotalNetWeight += item.TotalNetWeight;
            packingList.TotalGrossWeight += item.TotalGrossWeight;

            // specifications
            if (item.SupplyInvoiceOrderItem?.Product?.ProductSpecifications != null) {
                foreach (ProductSpecification spec in item.SupplyInvoiceOrderItem.Product.ProductSpecifications) {
                    packingList.TotalCustomValue += spec.CustomsValue;
                    packingList.TotalVatAmount += spec.VATValue;
                    packingList.TotalDuty += spec.Duty;
                }
            }
        }

        packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight, 3);
        packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight, 3);
    }

    public static void CalculateItem(
        PackingListPackageOrderItem item,
        decimal rate) {
        // weights
        item.TotalNetWeight = item.Qty * item.NetWeight;
        item.TotalGrossWeight = item.Qty * item.GrossWeight;

        // net price
        item.TotalNetPrice = (decimal)item.Qty * item.UnitPrice;
        item.TotalNetPriceEur = (decimal)item.Qty * item.UnitPriceEur;

        // accounting price EUR
        item.AccountingTotalGrossPriceEur =
            (decimal)item.Qty * item.AccountingGrossUnitPriceEur;

        item.AccountingGeneralTotalGrossPriceEur =
            (decimal)item.Qty * item.AccountingGeneralGrossUnitPriceEur;

        // total EUR
        item.TotalGrossPriceEur =
            (decimal)item.Qty * item.GrossUnitPriceEur
            + item.AccountingTotalGrossPriceEur
            + item.AccountingGeneralTotalGrossPriceEur;

        // convert EUR → UAH
        if (rate == 0) {
            item.AccountingTotalGrossPrice = 0;
            item.AccountingGeneralTotalGrossPrice = 0;
            item.TotalGrossPrice = 0;
            return;
        }

        if (rate == 1) {
            item.AccountingTotalGrossPrice = item.AccountingTotalGrossPriceEur;
            item.AccountingGeneralTotalGrossPrice = item.AccountingGeneralTotalGrossPriceEur;
            item.TotalGrossPrice = item.TotalGrossPriceEur;
            return;
        }

        decimal effectiveRate = rate > 0 ? rate : 1 / rate;

        item.AccountingTotalGrossPrice =
            item.AccountingTotalGrossPriceEur * effectiveRate;

        item.AccountingGeneralTotalGrossPrice =
            item.AccountingGeneralTotalGrossPriceEur * effectiveRate;

        item.TotalGrossPrice =
            item.TotalGrossPriceEur * effectiveRate;
    }

    private void ProcessGetAllReSaleProductsFilteredMessage(GetAllReSaleProductsFilteredMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            IReSaleAvailabilityRepository reSaleAvailabilityRepository = _reSaleRepositoriesFactory.NewReSaleAvailabilityRepository(connection);

            ReSaleAvailabilityWithTotalsModel reSaleAvailabilityWithTotalsModel = reSaleAvailabilityRepository.GetAllItemsForExport(message.FromDate, message.ToDate);

            TotalReSaleAvailabilitiesDto mappedReSaleAvailability = _mapper.Map<TotalReSaleAvailabilitiesDto>(reSaleAvailabilityWithTotalsModel);

            Sender.Tell(mappedReSaleAvailability);
        } catch (DbException e) {
            Sender.Tell(e.Message);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetAllServicesMessage(GetAllServicesMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            IMergedServiceRepository mergedServiceRepository = _supplyRepositoriesFactory.NewMergedServiceRepository(connection);

            List<MergedService> allForExport = mergedServiceRepository.GetAllForExport(message.From, message.To);

            List<PaidServiceDto> mappedPaidServices = _mapper.Map<List<PaidServiceDto>>(allForExport);

            Sender.Tell(mappedPaidServices);
        } catch (DbException e) {
            Sender.Tell(e.Message);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetAllSaleReturnsForExportMessage(GetAllSaleReturnsForExportMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
            ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

            List<SaleReturn> saleReturns = _saleReturnRepositoriesFactory
                .NewSaleReturnRepository(connection)
                .GetAllFiltered(
                    message.FromDate,
                    message.ToDate
                );

            Currency eur = currencyRepository.GetEURCurrencyIfExists();
            Currency uah = currencyRepository.GetUAHCurrencyIfExists();

            foreach (SaleReturn saleReturn in saleReturns) {
                if (saleReturn.Currency.CodeOneC.Equals(UahCode))
                    saleReturn.ExchangeRate = 1m;
                else {
                    ExchangeRate exchangeRate =
                        exchangeRateRepository
                            .GetByCurrencyIdAndCode(
                                uah.Id, eur.Code, saleReturn.FromDate);

                    saleReturn.ExchangeRate = exchangeRate.Amount;
                }
            }

            List<SaleReturnDto> mappedSaleReturns = _mapper.Map<List<SaleReturnDto>>(saleReturns);

            Sender.Tell(mappedSaleReturns);
        } catch (DbException e) {
            Sender.Tell(e.Message);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetAllProductTransfersForExportMessage(GetAllProductTransfersForExportMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
            ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

            List<ProductTransfer> productTransfers = _productRepositoriesFactory
                .NewProductTransferRepository(connection)
                .GetAllFiltered(
                    message.FromDate,
                    message.ToDate
                );

            Currency eur = currencyRepository.GetEURCurrencyIfExists();
            Currency uah = currencyRepository.GetUAHCurrencyIfExists();

            foreach (ProductTransfer productTransfer in productTransfers) {
                if (productTransfer.Organization.Currency.CodeOneC.Equals(UahCode))
                    productTransfer.ExchangeRate = 1m;
                else {
                    ExchangeRate exchangeRate =
                        exchangeRateRepository
                            .GetByCurrencyIdAndCode(
                                uah.Id, eur.Code, productTransfer.FromDate);

                    productTransfer.ExchangeRate = exchangeRate.Amount;
                }

                productTransfer.Currency = productTransfer.Organization.Currency;
            }

            List<ProductTransferDto> mappedProductTransfers = _mapper.Map<List<ProductTransferDto>>(productTransfers);

            Sender.Tell(mappedProductTransfers);
        } catch (DbException e) {
            Sender.Tell(e.Message);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetAllDepreciatedOrdersForExportMessage(GetAllDepreciatedOrdersForExportMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
            ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

            List<DepreciatedOrder> depreciatedOrders = _depreciatedRepositoriesFactory
                .NewDepreciatedOrderRepository(connection)
                .GetAllFiltered(
                    message.FromDate,
                    message.ToDate
                );

            Currency eur = currencyRepository.GetEURCurrencyIfExists();
            Currency uah = currencyRepository.GetUAHCurrencyIfExists();

            foreach (DepreciatedOrder depreciatedOrder in depreciatedOrders) {
                if (depreciatedOrder.Organization.Currency.CodeOneC.Equals(UahCode)) {
                    depreciatedOrder.ExchangeRate = 1m;
                } else {
                    ExchangeRate exchangeRate =
                        exchangeRateRepository
                            .GetByCurrencyIdAndCode(
                                uah.Id, eur.Code, depreciatedOrder.FromDate);

                    depreciatedOrder.ExchangeRate = exchangeRate.Amount;
                }

                depreciatedOrder.Currency = depreciatedOrder.Organization.Currency;

                depreciatedOrder.Amount = depreciatedOrder.DepreciatedOrderItems.Sum(item => item.PerUnitPrice);
            }

            List<DepreciatedOrderDto> mappedDepreciatedOrders = _mapper.Map<List<DepreciatedOrderDto>>(depreciatedOrders);

            Sender.Tell(mappedDepreciatedOrders);
        } catch (DbException e) {
            Sender.Tell(e.Message);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetAllSupplyInvoicesForExportMessage(GetAllSupplyInvoicesForExportMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IProductIncomeRepository productIncomeRepository = _productRepositoriesFactory.NewProductIncomeRepository(connection);

            // List<SupplyOrder> supplyOrders = _supplyRepositoriesFactory
            //     .NewSupplyOrderRepository(connection)
            //     .GetAll(message.From, message.To);
            //

            List<ProductIncome> productIncomes = productIncomeRepository.GetAllFiltered(message.From, message.To);

            // ProductIncome supplyOrderProductIncomeByNetId = _productRepositoriesFactory
            //     .NewProductIncomeRepository(connection)
            //     .GetSupplyOrderProductIncomeByNetId(message.NetId);

            // List<ProductIncome> toReturn = supplyOrders.Select(supplyOrder => productIncomeRepository.GetBySupplyOrderNetId(supplyOrder.NetUid)).ToList();

            // _productRepositoriesFactory
            //     .NewProductIncomeRepository(connection)
            //     .GetSupplyOrderProductIncomeByNetId(message.NetId);
            //
            // ISupplyInvoiceRepository supplyInvoiceRepository = _supplyRepositoriesFactory.NewSupplyInvoiceRepository(connection);
            //
            // List<SupplyInvoice> invoicesFiltered = supplyInvoiceRepository.GetAllIncomeInvoicesFiltered(message.From, message.To);
            //
            // List<SupplyInvoice> toReturn = invoicesFiltered.Select(invoice => supplyInvoiceRepository.GetByNetIdWithAllIncludes(invoice.NetUid)).ToList();

            List<ProductIncomeDto> mappedProductIncomes = _mapper.Map<List<ProductIncomeDto>>(productIncomes);

            Sender.Tell(mappedProductIncomes);
        } catch (DbException e) {
            Sender.Tell(e.Message);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetAllProductCapitalizationsForExportMessage(GetAllProductCapitalizationsForExportMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);
            ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

            List<ProductCapitalization> productCapitalizations = _productRepositoriesFactory
                .NewProductCapitalizationRepository(connection)
                .GetAllFiltered(
                    message.From,
                    message.To
                );

            Currency eur = currencyRepository.GetEURCurrencyIfExists();
            Currency uah = currencyRepository.GetUAHCurrencyIfExists();

            foreach (ProductCapitalization productCapitalization in productCapitalizations) {
                ExchangeRate exchangeRate =
                    exchangeRateRepository
                        .GetByCurrencyIdAndCode(
                            uah.Id, eur.Code, productCapitalization.FromDate);

                productCapitalization.ExchangeRate = exchangeRate.Amount;
                productCapitalization.Currency = eur;
            }

            List<ProductCapitalizationDto> mappedProductCapitalizations = _mapper.Map<List<ProductCapitalizationDto>>(productCapitalizations);
            Sender.Tell(mappedProductCapitalizations);
        } catch (DbException e) {
            Sender.Tell(e.Message);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetAllClientsMessage(GetAllClientsMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);
            ISupplyOrganizationRepository supplyOrganizationRepository = _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection);

            List<Client> clients = clientRepository.GetAllForExport("ORDER BY Client.ID DESC ", string.Empty, string.Empty)
                .Where(client => client.ClientAgreements.Any(clientAgreement => clientAgreement.Agreement.Organization.VatRateId != null)).ToList(); // temp where

            clients.ForEach(client =>
                client.ClientAgreements = client.ClientAgreements.Where(clientAgreement => clientAgreement.Agreement.Organization.VatRateId != null).ToList()); // Temp where

            List<ExtendedClientDto> listToReturn = _mapper.Map<List<ExtendedClientDto>>(clients);

            List<SupplyOrganization> supplyOrganizations = supplyOrganizationRepository.GetAll()
                .Where(so => so.SupplyOrganizationAgreements.Any(sa => sa.Organization.VatRateId != null)).ToList();

            supplyOrganizations.ForEach(so => so.SupplyOrganizationAgreements = so.SupplyOrganizationAgreements.Where(sa => sa.Organization.VatRateId != null).ToList());

            List<ExtendedClientDto> mappedSupplyOrganizations = _mapper.Map<List<ExtendedClientDto>>(supplyOrganizations);

            listToReturn.AddRange(mappedSupplyOrganizations);

            List<Client> manufacturers = clientRepository.GetAllForExport("ORDER BY Client.ID DESC ", string.Empty, string.Empty, false)
                .Where(c => c.ClientAgreements.Any(clientAgreement => clientAgreement.Agreement.Organization.VatRateId != null)).ToList();

            manufacturers.ForEach(client =>
                client.ClientAgreements = client.ClientAgreements.Where(clientAgreement => clientAgreement.Agreement.Organization.VatRateId != null).ToList());

            List<ExtendedClientDto> mappedManufacturers = _mapper.Map<List<ExtendedClientDto>>(manufacturers);

            listToReturn.AddRange(mappedManufacturers);

            Sender.Tell(listToReturn);
        } catch (DbException e) {
            Sender.Tell(e.Message);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetAllProductsLimitedMessage(GetAllProductsLimitedMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            List<ProductGroup> productGroups = _productRepositoriesFactory.NewProductGroupRepository(connection).GetAllFiltered(string.Empty).ProductGroups;

            List<Product> products = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection).GetAllLimited(message.Limit, message.Offset);

            Sender.Tell(new { Groups = _mapper.Map<List<ProductGroupDto>>(productGroups), Products = _mapper.Map<List<BaseProductExportModel>>(products) });
        } catch (DbException e) {
            Sender.Tell(e.Message);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetTotalProductsQtyMessage(GetTotalProductsQtyMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            Sender.Tell(new { Qty = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection).GetTotalQty() });
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetProductsFilteredMessage(GetProductsFilteredMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            List<Product> products = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection)
                .GetAllByUpdatedDates(message.DateFrom, message.DateTo, message.Limit, message.Offset);

            Sender.Tell(_mapper.Map<List<ProductDto>>(products));
        } catch (DbException e) {
            Sender.Tell(e.Message);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetSalesWithNewStatusMessage(GetSalesWithNewStatusMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            List<Sale> sales = _saleRepositoryFactory.NewSaleRepository(connection).GetAllRanged(
                message.DateFrom,
                message.DateTo,
                SaleLifeCycleType.New);

            foreach (Sale sale in sales) {
                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    IEnumerable<ProductAvailability> productAvailabilities =
                        productAvailabilityRepository
                            .GetByProductAndOrganizationIds(
                                orderItem.Product.Id,
                                sale.ClientAgreement.Agreement.Organization.Id,
                                sale.ClientAgreement.Agreement.Organization.Culture != "pl" && sale.ClientAgreement.Agreement.WithVATAccounting,
                                true,
                                sale.ClientAgreement.Agreement.Organization.StorageId
                            );

                    if (orderItem.Storage == null && orderItem.IsFromReSale) {
                        orderItem.Storage = productAvailabilities.FirstOrDefault(p => p.IsReSaleAvailability)?.Storage;
                    } else if (orderItem.Storage == null) {
                        orderItem.Storage = productAvailabilities.FirstOrDefault()?.Storage;
                    }

                    decimal currentExchangeRateAmountFiltered = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCultureFiltered(
                        orderItem.Product.NetUid,
                        sale.RetailClientId.HasValue || sale.IsVatSale,
                        orderItem.IsFromReSale,
                        sale.ClientAgreement.Agreement.Currency.Id
                    );

                    orderItem.ExchangeRateAmount = currentExchangeRateAmountFiltered;
                }
            }

            SaleActorsHelpers.CalculatePricingsForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                _currencyRepositoriesFactory.NewCurrencyRepository(connection));

            List<SaleDto> exportModels = _mapper.Map<List<SaleDto>>(sales);

            Sender.Tell(exportModels);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }

    private void ProcessGetInvoiceSalesFilteredMessage(GetInvoiceSalesFilteredMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            List<Sale> sales = _saleRepositoryFactory.NewSaleRepository(connection).GetAllRanged(
                message.FromDate,
                message.ToDate);

            foreach (Sale sale in sales) {
                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    IEnumerable<ProductAvailability> productAvailabilities =
                        productAvailabilityRepository
                            .GetByProductAndOrganizationIds(
                                orderItem.Product.Id,
                                sale.ClientAgreement.Agreement.Organization.Id,
                                sale.ClientAgreement.Agreement.Organization.Culture != "pl" && sale.ClientAgreement.Agreement.WithVATAccounting,
                                true,
                                sale.ClientAgreement.Agreement.Organization.StorageId
                            );

                    if (orderItem.Storage == null && orderItem.IsFromReSale) {
                        orderItem.Storage = productAvailabilities.FirstOrDefault(p => p.IsReSaleAvailability)?.Storage;
                    } else if (orderItem.Storage == null) {
                        orderItem.Storage = productAvailabilities.FirstOrDefault()?.Storage;
                    }

                    decimal currentExchangeRateAmountFiltered = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCultureFiltered(
                        orderItem.Product.NetUid,
                        sale.RetailClientId.HasValue || sale.IsVatSale,
                        orderItem.IsFromReSale,
                        sale.ClientAgreement.Agreement.Currency.Id
                    );

                    orderItem.ExchangeRateAmount = currentExchangeRateAmountFiltered;
                }
            }

            SaleActorsHelpers.CalculatePricingsForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                _currencyRepositoriesFactory.NewCurrencyRepository(connection));

            List<InvoiceDto> exportModels = _mapper.Map<List<InvoiceDto>>(sales);

            Sender.Tell(exportModels);
        } catch (Exception e) {
            Sender.Tell(new Exception("Critical error. Contact developers"));
        }
    }
}