using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.EntityHelpers.XmlDocumentModels;
using GBA.Domain.Repositories.XmlDocuments.Contracts;

namespace GBA.Domain.Repositories.XmlDocuments;

public sealed class XmlDocumentRepository : IXmlDocumentRepository {
    private readonly IDbConnection _additionalConnetion;
    private readonly IDbConnection _connection;

    public XmlDocumentRepository(IDbConnection connection, IDbConnection additionalConnetion) {
        _connection = connection;
        _additionalConnetion = additionalConnetion;
    }

    public List<Sale> GetSalesXmlDocumentByDate(DateTime from, DateTime to) {
        List<Sale> sales = new();

        string sqlQuery = "SELECT " +
                          "* " +
                          "FROM [Sale] " +
                          "LEFT JOIN [Order] " +
                          "ON [Order].[ID] = [Sale].[OrderID] " +
                          "LEFT JOIN [OrderItem] " +
                          "ON [OrderItem].[OrderID] = [Order].[ID] " +
                          "LEFT JOIN [Product] " +
                          "ON [Product].[ID] = [OrderItem].[ProductID] " +
                          "LEFT JOIN [ClientAgreement] " +
                          "ON [ClientAgreement].[ID] = [Order].[ClientAgreementID] " +
                          "LEFT JOIN [Agreement] " +
                          "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
                          "LEFT JOIN [ClientBankDetails] " +
                          "ON [ClientBankDetails].[ID] = [Client].[ClientBankDetailsID] " +
                          "LEFT JOIN [Currency] " +
                          "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                          "LEFT JOIN [ClientBankDetailAccountNumber] " +
                          "ON [ClientBankDetailAccountNumber].[ID] = [ClientBankDetails].[AccountNumberID] " +
                          "LEFT JOIN [Organization] " +
                          "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
                          "LEFT JOIN [PaymentRegister] " +
                          "ON [PaymentRegister].[OrganizationID] = [Organization].[ID] " +
                          "AND [PaymentRegister].[IsActive] = 1 " +
                          "AND [PaymentRegister].[Deleted] = 0 " +
                          "LEFT JOIN [MeasureUnit] " +
                          "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
                          "LEFT JOIN [SaleNumber] " +
                          "ON [SaleNumber].[ID] = [Sale].[SaleNumberID] " +
                          "LEFT JOIN [ProductProductGroup] " +
                          "ON [ProductProductGroup].[ProductID] = [Product].[ID] " +
                          "LEFT JOIN [TaxAccountingScheme] " +
                          "ON [TaxAccountingScheme].[ID] = [Agreement].[TaxAccountingSchemeID] " +
                          "LEFT JOIN [ProductSpecification] " +
                          "ON [ProductSpecification].[Id] = [OrderItem].[AssignedSpecificationID] " +
                          "LEFT JOIN [AgreementTypeCivilCode] " +
                          "ON [AgreementTypeCivilCode].[ID] = [Agreement].[AgreementTypeCivilCodeID] " +
                          "LEFT JOIN [ShipmentListItem] " +
                          "ON [ShipmentListItem].[SaleID] = [Sale].[ID] " +
                          "LEFT JOIN [DeliveryRecipient] " +
                          "ON [DeliveryRecipient].[ID] = [Sale].[DeliveryRecipientID] " +
                          "LEFT JOIN [DeliveryRecipientAddress] " +
                          "ON [DeliveryRecipientAddress].[ID] = [Sale].[ID] " +
                          "LEFT JOIN [Transporter] " +
                          "ON [Transporter].[ID] = [Sale].[TransporterID] " +
                          "LEFT JOIN [ProductSet] " +
                          "ON [ProductSet].[BaseProductID] = [Product].[ID] " +
                          "WHERE [Sale].[ChangedToInvoice] IS NOT NULL " +
                          "AND [Sale].[Updated] > @From " +
                          "AND [Sale].[Updated] < @To ";

        Type[] types = {
            typeof(Sale),
            typeof(Order),
            typeof(OrderItem),
            typeof(Product),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Client),
            typeof(ClientBankDetails),
            typeof(Currency),
            typeof(ClientBankDetailAccountNumber),
            typeof(Organization),
            typeof(PaymentRegister),
            typeof(MeasureUnit),
            typeof(SaleNumber),
            typeof(ProductProductGroup),
            typeof(TaxAccountingScheme),
            typeof(ProductSpecification),
            typeof(AgreementTypeCivilCode),
            typeof(ShipmentListItem),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(Transporter),
            typeof(ProductSet)
        };

        Func<object[], Sale> mapper = objects => {
            Sale sale = (Sale)objects[0];
            Order order = (Order)objects[1];
            OrderItem orderItem = (OrderItem)objects[2];
            Product product = (Product)objects[3];
            ClientAgreement clientAgreement = (ClientAgreement)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Client client = (Client)objects[6];
            ClientBankDetails clientBankDetails = (ClientBankDetails)objects[7];
            Currency currency = (Currency)objects[8];
            ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[9];
            Organization organization = (Organization)objects[10];
            PaymentRegister paymentRegister = (PaymentRegister)objects[11];
            MeasureUnit measureUnit = (MeasureUnit)objects[12];
            SaleNumber saleNumber = (SaleNumber)objects[13];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[14];
            TaxAccountingScheme taxAccountingScheme = (TaxAccountingScheme)objects[15];
            ProductSpecification productSpecification = (ProductSpecification)objects[16];
            AgreementTypeCivilCode agreementTypeCivilCode = (AgreementTypeCivilCode)objects[17];
            ShipmentListItem shipmentListItem = (ShipmentListItem)objects[18];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[19];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[20];
            Transporter transporter = (Transporter)objects[21];
            ProductSet productSet = (ProductSet)objects[22];

            if (sales.Any(x => x.Id == sale.Id)) {
                sale = sales.First(x => x.Id == sale.Id);
            } else {
                sales.Add(sale);

                sale.DeliveryRecipient = deliveryRecipient;

                if (transporter != null)
                    sale.Transporter = transporter;

                sale.DeliveryRecipientAddress = deliveryRecipientAddress;

                sale.Order = order;

                sale.SaleNumber = saleNumber;

                if (shipmentListItem != null)
                    sale.ShipmentListItems.Add(shipmentListItem);

                organization.PaymentRegisters.Add(paymentRegister);

                agreement.Organization = organization;

                currency.ExchangeRates.Add(new ExchangeRate {
                    Amount = GetCurrencyExhangeRate(sale.Created, currency.Id)
                });

                agreement.Currency = currency;

                agreement.TaxAccountingScheme = taxAccountingScheme;

                agreement.AgreementTypeCivilCode = agreementTypeCivilCode;

                if (clientBankDetails == null)
                    clientBankDetails = new ClientBankDetails { AccountNumber = new ClientBankDetailAccountNumber() };
                else
                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;

                client.ClientBankDetails = clientBankDetails;
                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;
                sale.ClientAgreement = clientAgreement;
            }

            if (sale.Order.OrderItems.Any(x => x.Id == orderItem.Id)) {
                orderItem = sale.Order.OrderItems.First(x => x.Id == orderItem.Id);
            } else {
                order.OrderItems.Add(orderItem);

                orderItem.AssignedSpecification = productSpecification;

                orderItem.TotalAmount = Convert.ToDecimal(orderItem.Qty) * orderItem.PricePerItem;

                orderItem.Qty -= orderItem.ReturnedQty;

                product.MeasureUnit = measureUnit;
            }

            if (orderItem.Product == null)
                orderItem.Product = product;
            else
                product = orderItem.Product;

            product.HasComponent = productSet != null;

            if (productProductGroup == null) return sale;

            product.ProductProductGroups.Add(GetProductGroups(productProductGroup.ProductGroupId));

            return sale;
        };

        _connection.Query(sqlQuery, types, mapper, new { From = from, To = to, Culture = "uk" }, commandTimeout: 3600);

        return sales;
    }

    public ProductIncomesModel GetProductIncomesXmlDocumentByDate(DateTime from, DateTime to) {
        ProductIncomesModel productIncomesModel = new();

        string condition = "WHERE [ProductIncome].[Deleted] = 0 " +
                           "AND ([ProductIncome].[ProductIncomeType] = 0 " +
                           "OR [ProductIncome].[ProductIncomeType] = 1) " +
                           "AND [ProductIncome].[Updated] > @From " +
                           "AND [ProductIncome].[Updated] < @To ";

        string sqlQuerySupplyOrderUkraine = "SELECT " +
                                            "* " +
                                            "FROM [ProductIncome] " +
                                            "LEFT JOIN [ProductIncomeItem] " +
                                            "ON [ProductIncomeItem].[ProductIncomeID] = [ProductIncome].[ID] " +
                                            "LEFT JOIN [SupplyOrderUkraineItem] " +
                                            "ON [ProductIncomeItem].[SupplyOrderUkraineItemID] = [SupplyOrderUkraineItem].[ID] " +
                                            "LEFT JOIN [SupplyOrderUkraine] " +
                                            "ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID] " +
                                            "LEFT JOIN [Product] " +
                                            "ON [Product].[ID] = [SupplyOrderUkraineItem].[ProductID] " +
                                            "LEFT JOIN [MeasureUnit] " +
                                            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
                                            "LEFT JOIN [ProductProductGroup] " +
                                            "ON [ProductProductGroup].[ProductID] = [Product].[ID] " +
                                            "LEFT JOIN [ClientAgreement] " +
                                            "ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
                                            "LEFT JOIN [Client] " +
                                            "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
                                            "LEFT JOIN [Agreement] " +
                                            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                                            "LEFT JOIN [TaxAccountingScheme] " +
                                            "ON [TaxAccountingScheme].[ID] = [Agreement].[TaxAccountingSchemeID] " +
                                            "LEFT JOIN [AgreementTypeCivilCode] " +
                                            "ON [AgreementTypeCivilCode].[ID] = [Agreement].[AgreementTypeCivilCodeID] " +
                                            "LEFT JOIN [ClientBankDetails] " +
                                            "ON [ClientBankDetails].[ID] = [Client].[ClientBankDetailsID] " +
                                            "LEFT JOIN [ClientBankDetailAccountNumber] " +
                                            "ON [ClientBankDetailAccountNumber].[ID] = [ClientBankDetails].[AccountNumberID] " +
                                            "LEFT JOIN [Organization] " +
                                            "ON [Organization].[ID] = [SupplyOrderUkraine].[OrganizationID] " +
                                            "LEFT JOIN [PaymentRegister] " +
                                            "ON [PaymentRegister].[OrganizationID] = [Organization].[ID] " +
                                            "AND [PaymentRegister].[Deleted] = 0 " +
                                            "AND [PaymentRegister].[IsActive] = 1 " +
                                            "LEFT JOIN [Currency] " +
                                            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                                            "LEFT JOIN [Sad] " +
                                            "ON [Sad].[SupplyOrderUkraineID] = [SupplyOrderUkraine].[ID] " +
                                            "LEFT JOIN [ProductSet] " +
                                            "ON [ProductSet].[BaseProductID] = [Product].[ID] " +
                                            condition +
                                            "AND [ProductIncomeItem].[SupplyOrderUkraineItemID] IS NOT NULL ";

        string sqlQuerySupplyOrder = "SELECT " +
                                     "* " +
                                     "FROM [ProductIncome] " +
                                     "LEFT JOIN [ProductIncomeItem] " +
                                     "ON [ProductIncomeItem].[ProductIncomeID] = [ProductIncome].[ID] " +
                                     "LEFT JOIN [PackingListPackageOrderItem] " +
                                     "ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID] " +
                                     "LEFT JOIN [SupplyInvoiceOrderItem] " +
                                     "ON [SupplyInvoiceOrderItem].[ID] = [PackingListPackageOrderItem].[SupplyInvoiceOrderItemID] " +
                                     "LEFT JOIN [SupplyOrderItem] " +
                                     "ON [SupplyOrderItem].[ID] = [SupplyInvoiceOrderItem].[SupplyOrderItemID] " +
                                     "LEFT JOIN [Product] " +
                                     "ON [Product].[ID] = [SupplyInvoiceOrderItem].[ProductID] " +
                                     "LEFT JOIN [MeasureUnit] " +
                                     "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
                                     "LEFT JOIN [ProductProductGroup] " +
                                     "ON [ProductProductGroup].[ProductID] = [Product].[ID] " +
                                     "LEFT JOIN [SupplyOrder] " +
                                     "ON [SupplyOrder].[ID] = [SupplyOrderItem].[SupplyOrderID] " +
                                     "LEFT JOIN [SupplyOrderNumber] " +
                                     "ON [SupplyOrderNumber].[ID] = [SupplyOrder].[SupplyOrderNumberID] " +
                                     "LEFT JOIN [Client] " +
                                     "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
                                     "LEFT JOIN [ClientBankDetails] " +
                                     "ON [ClientBankDetails].[ID] = [Client].[ClientBankDetailsID] " +
                                     "LEFT JOIN [ClientBankDetailAccountNumber] " +
                                     "ON [ClientBankDetailAccountNumber].[ID] = [ClientBankDetails].[AccountNumberID] " +
                                     "LEFT JOIN [Organization] " +
                                     "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
                                     "LEFT JOIN [PaymentRegister] " +
                                     "ON [PaymentRegister].[OrganizationID] = [Organization].[ID] " +
                                     "AND [PaymentRegister].[Deleted] = 0 " +
                                     "AND [PaymentRegister].[IsActive] = 1 " +
                                     "LEFT JOIN [ClientAgreement] " +
                                     "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
                                     "LEFT JOIN [Agreement] " +
                                     "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                                     "LEFT JOIN [TaxAccountingScheme] " +
                                     "ON [TaxAccountingScheme].[ID] = [Agreement].[TaxAccountingSchemeID] " +
                                     "LEFT JOIN [AgreementTypeCivilCode] " +
                                     "ON [AgreementTypeCivilCode].[ID] = [Agreement].[AgreementTypeCivilCodeID] " +
                                     "LEFT JOIN [Currency] " +
                                     "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                                     "LEFT JOIN [SupplyInvoice] " +
                                     "ON [SupplyInvoice].[SupplyOrderID] = [SupplyOrder].[ID] " +
                                     "LEFT JOIN [ProductSet] " +
                                     "ON [ProductSet].[BaseProductID] = [Product].[ID] " +
                                     condition +
                                     "AND [ProductIncomeItem].[PackingListPackageOrderItemID] IS NOT NULL ";

        Type[] typeSupplyOrderUkraine = {
            typeof(ProductIncome),
            typeof(ProductIncomeItem),
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductProductGroup),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Agreement),
            typeof(TaxAccountingScheme),
            typeof(AgreementTypeCivilCode),
            typeof(ClientBankDetails),
            typeof(ClientBankDetailAccountNumber),
            typeof(Organization),
            typeof(PaymentRegister),
            typeof(Currency),
            typeof(Sad),
            typeof(ProductSet)
        };

        Type[] typesSupplyOrder = {
            typeof(ProductIncome),
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductProductGroup),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client),
            typeof(ClientBankDetails),
            typeof(ClientBankDetailAccountNumber),
            typeof(Organization),
            typeof(PaymentRegister),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(TaxAccountingScheme),
            typeof(AgreementTypeCivilCode),
            typeof(Currency),
            typeof(SupplyInvoice),
            typeof(ProductSet)
        };

        Func<object[], ProductIncome> mapperSupplyOrderUkraine = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            SupplyOrderUkraineItem supplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[2];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[6];
            ClientAgreement clientAgreement = (ClientAgreement)objects[7];
            Client client = (Client)objects[8];
            Agreement agreement = (Agreement)objects[9];
            TaxAccountingScheme taxAccountingScheme = (TaxAccountingScheme)objects[10];
            AgreementTypeCivilCode agreementTypeCivilCode = (AgreementTypeCivilCode)objects[11];
            ClientBankDetails clientBankDetails = (ClientBankDetails)objects[12];
            ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[13];
            Organization organization = (Organization)objects[14];
            PaymentRegister paymentRegister = (PaymentRegister)objects[15];
            Currency currency = (Currency)objects[16];
            Sad sad = (Sad)objects[17];
            ProductSet productSet = (ProductSet)objects[18];

            if (productIncomesModel.SupplyOrderUkraines.Any(x => x.Id == supplyOrderUkraine.Id))
                supplyOrderUkraine = productIncomesModel.SupplyOrderUkraines.FirstOrDefault(x => x.Id == supplyOrderUkraine.Id);
            else
                productIncomesModel.SupplyOrderUkraines.Add(supplyOrderUkraine);

            if (supplyOrderUkraine == null) return productIncome;

            if (organization.PaymentRegisters.All(x => x.Id != paymentRegister.Id))
                organization.PaymentRegisters.Add(paymentRegister);

            supplyOrderUkraine.Organization = organization;

            currency.ExchangeRates.Add(new ExchangeRate {
                Amount = GetCurrencyExhangeRate(supplyOrderUkraine.Created, currency.Id)
            });

            agreement.Currency = currency;

            agreement.TaxAccountingScheme = taxAccountingScheme;

            agreement.AgreementTypeCivilCode = agreementTypeCivilCode;

            clientAgreement.Agreement = agreement;

            if (clientBankDetails != null)
                clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
            else
                clientBankDetails = new ClientBankDetails { AccountNumber = clientBankDetailAccountNumber };

            client.ClientBankDetails = clientBankDetails;

            clientAgreement.Client = client;

            supplyOrderUkraine.ClientAgreement = clientAgreement;

            if (supplyOrderUkraine.SupplyOrderUkraineItems.Any(x => x.Id == supplyOrderUkraineItem.Id))
                supplyOrderUkraineItem = supplyOrderUkraine.SupplyOrderUkraineItems.FirstOrDefault(x => x.Id == supplyOrderUkraineItem.Id);
            else
                supplyOrderUkraine.SupplyOrderUkraineItems.Add(supplyOrderUkraineItem);

            if (supplyOrderUkraineItem == null) return productIncome;

            supplyOrderUkraineItem.TotalNetWeight = supplyOrderUkraineItem.Qty * supplyOrderUkraineItem.NetWeight;

            supplyOrderUkraine.TotalNetWeight += supplyOrderUkraineItem.TotalNetWeight;

            supplyOrderUkraine.TotalQty += supplyOrderUkraineItem.Qty;

            supplyOrderUkraine.TotalNetPrice += decimal.Round(Convert.ToDecimal(supplyOrderUkraineItem.Qty) * supplyOrderUkraineItem.NetPrice);
            supplyOrderUkraine.TotalGrossPrice += decimal.Round(Convert.ToDecimal(supplyOrderUkraineItem.Qty) * supplyOrderUkraineItem.GrossPrice);

            product.MeasureUnit = measureUnit;

            if (productProductGroup != null)
                product.ProductProductGroups.Add(GetProductGroups(productProductGroup.ProductGroupId));

            if (sad != null) {
                supplyOrderUkraine.Sad = sad;
                product.ProductSpecifications.Add(GetProductSpecificationThroughSad(sad.Id, product.Id));
            }

            product.HasComponent = productSet != null;

            supplyOrderUkraineItem.Product = product;

            return productIncome;
        };

        Func<object[], ProductIncome> mapperSupplyOrder = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[4];
            Product product = (Product)objects[5];
            MeasureUnit measureUnit = (MeasureUnit)objects[6];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[7];
            SupplyOrder supplyOrder = (SupplyOrder)objects[8];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[9];
            Client client = (Client)objects[10];
            ClientBankDetails clientBankDetails = (ClientBankDetails)objects[11];
            ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[12];
            Organization organization = (Organization)objects[13];
            PaymentRegister paymentRegister = (PaymentRegister)objects[14];
            ClientAgreement clientAgreement = (ClientAgreement)objects[15];
            Agreement agreement = (Agreement)objects[16];
            TaxAccountingScheme taxAccountingScheme = (TaxAccountingScheme)objects[17];
            AgreementTypeCivilCode agreementTypeCivilCode = (AgreementTypeCivilCode)objects[18];
            Currency currency = (Currency)objects[19];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[20];
            ProductSet productSet = (ProductSet)objects[21];

            if (productIncomesModel.SupplyOrders.Any(x => x.Id == supplyOrder.Id))
                supplyOrder = productIncomesModel.SupplyOrders.FirstOrDefault(x => x.Id == supplyOrder.Id);
            else
                productIncomesModel.SupplyOrders.Add(supplyOrder);

            if (supplyOrder == null) return productIncome;

            supplyOrder.SupplyOrderNumber = supplyOrderNumber;

            if (clientBankDetails != null)
                clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
            else
                clientBankDetails = new ClientBankDetails { AccountNumber = clientBankDetailAccountNumber };

            client.ClientBankDetails = clientBankDetails;

            supplyOrder.Client = client;

            currency.ExchangeRates.Add(new ExchangeRate {
                Amount = GetCurrencyExhangeRate(supplyOrder.Created, currency.Id)
            });

            agreement.Currency = currency;

            agreement.TaxAccountingScheme = taxAccountingScheme;

            agreement.AgreementTypeCivilCode = agreementTypeCivilCode;

            clientAgreement.Agreement = agreement;

            supplyOrder.ClientAgreement = clientAgreement;

            if (organization.PaymentRegisters.All(x => x.Id != paymentRegister.Id))
                organization.PaymentRegisters.Add(paymentRegister);

            supplyOrder.Organization = organization;

            if (supplyOrder.SupplyOrderItems.Any(x => x.Id == supplyOrderItem.Id))
                supplyOrderItem = supplyOrder.SupplyOrderItems.FirstOrDefault(x => x.Id == supplyOrderItem.Id);
            else
                supplyOrder.SupplyOrderItems.Add(supplyOrderItem);

            if (supplyOrderItem == null) return productIncome;

            if (supplyInvoice != null)
                product.ProductSpecifications.Add(GetProductSpecificationThroughInvoice(supplyInvoice.Id, product.Id));

            product.MeasureUnit = measureUnit;

            product.ProductProductGroups.Add(GetProductGroups(productProductGroup.Id));

            product.HasComponent = productSet != null;

            if (supplyOrderItem != null)
                supplyOrderItem.Product = product;

            return productIncome;
        };

        using SqlMapper.GridReader reader = _connection.QueryMultiple(sqlQuerySupplyOrderUkraine + sqlQuerySupplyOrder, new { From = from, To = to }, commandTimeout: 3600);
        reader.Read(typeSupplyOrderUkraine, mapperSupplyOrderUkraine);
        reader.Read(typesSupplyOrder, mapperSupplyOrder);


        return productIncomesModel;
    }

    private ProductProductGroup GetProductGroups(long productGroupId) {
        string sqlQueryProductGroup = "DECLARE @ProductGroupId bigint = @Id; " +
                                      ";WITH [Roots_CTE] " +
                                      "AS ( " +
                                      "SELECT [ProductGroup].ID " +
                                      ", 0 AS [Count] " +
                                      "FROM [ProductGroup] " +
                                      "WHERE [ProductGroup].ID = @ProductGroupId " +
                                      "UNION ALL " +
                                      "SELECT [ProductSubGroup].RootProductGroupID " +
                                      ", [Roots_CTE].[Count] + 1 AS [Count] " +
                                      "FROM [ProductSubGroup] " +
                                      "INNER JOIN [Roots_CTE] " +
                                      "ON [Roots_CTE].ID = [ProductSubGroup].SubProductGroupID " +
                                      "WHERE [ProductSubGroup].Deleted = 0 " +
                                      ") " +
                                      "SELECT [ProductGroup].* " +
                                      "FROM [ProductGroup] " +
                                      "INNER JOIN [Roots_CTE] " +
                                      "ON [Roots_CTE].ID = [ProductGroup].ID " +
                                      "ORDER BY [Roots_CTE].[Count] DESC";

        List<ProductGroup> productGroups = _additionalConnetion.Query<ProductGroup>(sqlQueryProductGroup, new { Id = productGroupId }).ToList();

        return new ProductProductGroup { ProductGroups = productGroups };
    }

    private ProductSpecification GetProductSpecificationThroughInvoice(long supplyInvoiceId, long productId) {
        return _additionalConnetion.Query<ProductSpecification>(
            "SELECT [ProductSpecification].* " +
            "FROM [ProductSpecification] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].[ProductSpecificationId] = [ProductSpecification].[ID] " +
            "WHERE [OrderProductSpecification].[SupplyInvoiceId] = @SupplyInvoiceId " +
            "AND [ProductSpecification].[ProductID] = @ProductID " +
            "AND [ProductSpecification].[Deleted] = 0 " +
            "AND [ProductSpecification].[IsActive] = 1 ",
            new { SupplyInvoiceId = supplyInvoiceId, ProductID = productId }).FirstOrDefault();
    }

    private ProductSpecification GetProductSpecificationThroughSad(long sadId, long productId) {
        return _additionalConnetion.Query<ProductSpecification>(
            "SELECT [ProductSpecification].* " +
            "FROM [ProductSpecification] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].[ProductSpecificationId] = [ProductSpecification].[ID] " +
            "WHERE [OrderProductSpecification].[SadId] = @SadId " +
            "AND [ProductSpecification].[ProductID] = @ProductID " +
            "AND [ProductSpecification].[Deleted] = 0 " +
            "AND [ProductSpecification].[IsActive] = 1 ",
            new { SadId = sadId, ProductID = productId }).FirstOrDefault();
    }

    private decimal GetCurrencyExhangeRate(DateTime documentDate, long currencyId) {
        return _additionalConnetion.Query<decimal>(
            "SELECT  TOP 1 [ExchangeRateHistory].[Amount] FROM [Currency] " +
            "LEFT JOIN [ExchangeRate] " +
            "ON [ExchangeRate].[Code] = [Currency].[Code] " +
            "LEFT JOIN [ExchangeRateHistory] " +
            "ON [ExchangeRateHistory].[ExchangeRateID] = [ExchangeRate].[ID] " +
            "WHERE [Currency].[ID] = @CurrencyId " +
            "AND [ExchangeRate].[Culture] = @Culture " +
            "AND [ExchangeRateHistory].[Created] < @Date " +
            "ORDER BY [ExchangeRateHistory].ID DESC",
            new {
                CurrencyId = currencyId,
                Date = documentDate,
                Culture = "uk"
            }).FirstOrDefault();
    }
}