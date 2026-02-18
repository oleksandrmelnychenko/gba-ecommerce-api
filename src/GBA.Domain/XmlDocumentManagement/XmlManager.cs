using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Agreements;
using GBA.Domain.EntityHelpers.XmlDocumentModels;
using GBA.Domain.XmlDocumentManagement.Contracts;

namespace GBA.Domain.XmlDocumentManagement;

public sealed class XmlManager : IXmlManager {
    public XDocument GetSalesXmlDocuments(string pathToFolder, List<Sale> sales) {
        string fileName = Path.Combine(pathToFolder, "Sale.xml");

        if (File.Exists(fileName)) File.Delete(fileName);

        XDocument xmlDocument = new();

        XElement rootObjectExchangeFile = CreateRootObjectExchangeFile();

        rootObjectExchangeFile.Add(CreateExchangeRules());

        int counterReference = 0;

        foreach (Sale sale in sales) {
            Dictionary<string, int> referenceCounter = new();
            referenceCounter.Add("DocumentId", ++counterReference);

            referenceCounter.Add("CurrencyId", ++counterReference);
            XElement currency = CreateAdditionalObject(RuleNames.CURRENCIES, TypeOfExchangeFile.CURRENCIES, counterReference, true);
            XElement currencyReference = CreateReference(referenceCounter["CurrencyId"]);
            currencyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING,
                "Код",
                sale.ClientAgreement.Agreement.Currency.CodeOneC));
            currency.Add(currencyReference);
            currency.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Наименование", sale.ClientAgreement.Agreement.Currency.Name));
            currency.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "НаименованиеПолное", sale.ClientAgreement.Agreement.Currency.Name));

            referenceCounter.Add("AccountId", ++counterReference);
            XElement account = CreateAdditionalObject(RuleNames.BANK_ACOUNT, TypeOfExchangeFile.BANK_ACCOUNT, referenceCounter["AccountId"], true);
            XElement referenceAccount = CreateReference(referenceCounter["AccountId"]);
            referenceAccount.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НомерСчета",
                sale.ClientAgreement.Agreement.Organization.PaymentRegisters.FirstOrDefault()?.AccountNumber));
            account.Add(referenceAccount);
            XElement accountCurrency = CreatePropertyElement(TypeOfExchangeFile.CURRENCIES, "ВалютаДенежныхСредств");
            XElement accountCurrencyReference = CreateReference(referenceCounter["CurrencyId"]);
            XElement accountCurrencyReferenceProperty = CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                sale.ClientAgreement.Agreement.Currency.CodeOneC);
            accountCurrencyReference.Add(accountCurrencyReferenceProperty);
            accountCurrency.Add(accountCurrencyReference);
            account.Add(accountCurrency);
            account.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                sale.ClientAgreement.Agreement.Organization.PaymentRegisters.FirstOrDefault()?.Name ?? ""));

            referenceCounter.Add("TypeAgreementCivilCodeId", ++counterReference);
            XElement typeAgreementCivilCode = CreateAdditionalObject(
                RuleNames.TYPE_AGREEMENT_CIVIL_CODE,
                TypeOfExchangeFile.TYPE_AGREEMENT_CIVIL_CODE,
                referenceCounter["TypeAgreementCivilCodeId"], false);
            XElement typeAgreementCivilCodeReference = CreateReference(referenceCounter["TypeAgreementCivilCodeId"]);
            typeAgreementCivilCodeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                sale.ClientAgreement.Agreement.AgreementTypeCivilCode?.CodeOneC ?? ""));
            typeAgreementCivilCode.Add(typeAgreementCivilCodeReference);
            typeAgreementCivilCode.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Наименование",
                sale.ClientAgreement.Agreement.AgreementTypeCivilCode?.NameUK ?? ""));
            typeAgreementCivilCode.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.TYPE_AGREEMENT_CIVIL_CODE, "Родитель"));

            referenceCounter.Add("TaxAccountingSchemeId", ++counterReference);
            XElement taxAccountingScheme = CreateAdditionalObject(
                RuleNames.TAX_ACCOUNTING_SCHEME,
                TypeOfExchangeFile.TAX_ACCOUNTING_SCHEME,
                referenceCounter["TaxAccountingSchemeId"], false);
            XElement taxAccountingSchemeReference = CreateReference(referenceCounter["TaxAccountingSchemeId"]);
            taxAccountingSchemeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                sale.ClientAgreement.Agreement.TaxAccountingScheme?.CodeOneC ?? ""));
            taxAccountingScheme.Add(taxAccountingSchemeReference);
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                sale.ClientAgreement.Agreement.TaxAccountingScheme?.NameUK ?? ""));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "МоментОпределенияБазыНДСПоПокупкам",
                GetTaxBaseMomentName(sale.ClientAgreement.Agreement.TaxAccountingScheme?.PurchaseTaxBaseMoment ?? TaxBaseMoment.NotDefine)));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "МоментОпределенияБазыНДСПоПродажам",
                GetTaxBaseMomentName(sale.ClientAgreement.Agreement.TaxAccountingScheme?.SaleTaxBaseMoment ?? TaxBaseMoment.NotDefine)));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "УдалитьМоментОпределенияБазыННППоПокупкам",
                GetTaxBaseMomentName(sale.ClientAgreement.Agreement.TaxAccountingScheme?.PurchaseTaxBaseMoment ?? TaxBaseMoment.NotDefine)));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "УдалитьМоментОпределенияБазыННППоПродажам",
                GetTaxBaseMomentName(sale.ClientAgreement.Agreement.TaxAccountingScheme?.SaleTaxBaseMoment ?? TaxBaseMoment.NotDefine)));

            referenceCounter.Add("CounterpartyId", ++counterReference);
            referenceCounter.Add("AgreementId", ++counterReference);
            XElement counterParty = CreateAdditionalObject(RuleNames.COUNTERPARTY, TypeOfExchangeFile.COUNTERPARTY, referenceCounter["CounterpartyId"], true);
            XElement counterPartyReference = CreateReference(referenceCounter["CounterpartyId"]);
            counterPartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING,
                "КодПоЕДРПОУ",
                sale.ClientAgreement.Client.USREOU));
            counterParty.Add(counterPartyReference);
            counterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "ИНН", sale.ClientAgreement.Client.TIN));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                sale.ClientAgreement.Client.Name ??
                $"{sale.ClientAgreement.Client.LastName} {sale.ClientAgreement.Client.FirstName} {sale.ClientAgreement.Client.MiddleName}"));
            counterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "НаименованиеПолное",
                sale.ClientAgreement.Client.FullName));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Комментарий", sale.ClientAgreement.Client.Comment));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НомерСвидетельства"));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "ОсновнойБанковскийСчет"));
            XElement counterPartyAgreement = CreatePropertyElement(TypeOfExchangeFile.AGREEMENT, "ОсновнойДоговорКонтрагента");
            XElement counterPartyAgreementReference = CreateReference(referenceCounter["AgreementId"]);
            XElement counterPartyAgreementReferenceCounterparty = CreatePropertyElement(TypeOfExchangeFile.COUNTERPARTY, "Владелец");
            XElement counterPartyAgreementReferenceCounterpartyReference = CreateReference(referenceCounter["CounterpartyId"]);
            counterPartyAgreementReferenceCounterpartyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "КодПоЕДРПОУ",
                sale.ClientAgreement.Client.USREOU));
            counterPartyAgreementReferenceCounterparty.Add(counterPartyAgreementReferenceCounterpartyReference);
            counterPartyAgreementReference.Add(counterPartyAgreementReferenceCounterparty);
            counterPartyAgreementReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.DATE,
                "Дата",
                sale.ClientAgreement.Agreement.Created));
            counterPartyAgreementReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Номер",
                sale.ClientAgreement.Agreement.Number));
            counterPartyAgreement.Add(counterPartyAgreementReference);
            counterParty.Add(counterPartyAgreement);
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "Покупатель", true));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "Поставщик", false));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_CLIENT_IS_INVIDUALS,
                "ЮрФизЛицо", sale.ClientAgreement.Client.IsIndividual ? "ФизЛицо" : "ЮрЛицо"));

            XElement agreementCounterParty = CreateAdditionalObject(RuleNames.AGREEMENT, TypeOfExchangeFile.AGREEMENT, referenceCounter["AgreementId"], true);
            XElement agreementCounterPartyReference = CreateReference(referenceCounter["AgreementId"]);
            XElement agreementCounterPartyReferenceProperty = CreatePropertyElement(
                TypeOfExchangeFile.COUNTERPARTY, "Владелец");
            XElement agreementCounterPartyReferencePropertyReference = CreateReference(referenceCounter["CounterpartyId"]);
            agreementCounterPartyReferencePropertyReference.Add(
                CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "КодПоЕДРПОУ", sale.ClientAgreement.Client.USREOU));
            agreementCounterPartyReferenceProperty.Add(agreementCounterPartyReferencePropertyReference);
            agreementCounterPartyReference.Add(agreementCounterPartyReferenceProperty);
            agreementCounterPartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.DATE, "Дата",
                sale.ClientAgreement.Agreement.Created.ToString("yyyy-MM-ddTHH:mm:ss")));
            agreementCounterPartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Номер", sale.ClientAgreement.Agreement.Number ?? ""));
            agreementCounterParty.Add(agreementCounterPartyReference);
            XElement agreementCounterPartyProperty = CreatePropertyElement(TypeOfExchangeFile.CURRENCIES, "ВалютаВзаиморасчетов");
            XElement agreementCounterPartyPropertyReference = CreateReference(referenceCounter["CurrencyId"]);
            agreementCounterPartyPropertyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING, "Код", sale.ClientAgreement.Agreement.Currency.Id));
            agreementCounterPartyProperty.Add(agreementCounterPartyPropertyReference);
            agreementCounterParty.Add(agreementCounterPartyProperty);
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_TYPE_MUTUAL_SETTLEMENTS, "ВедениеВзаиморасчетов", "ПоДоговоруВЦелом"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ВестиПоДокументамРасчетовСКонтрагентом", true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ВестиПоДокументамРасчетовСКонтрагентомРегл", true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_TYPE_AGREEMENT,
                "ВидДоговора", "СПокупателем"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_TYPE_CONDITION_AGREEMENT,
                "ВидУсловийДоговора", "БезДополнительныхУсловий"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "Внешнеэкономический", sale.ClientAgreement.Agreement.Currency.Code.Equals("UAH") ? false : true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ДопустимаяСуммаЗадолженности", sale.ClientAgreement.Agreement.AmountDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ДопустимоеЧислоДнейЗадолженности", sale.ClientAgreement.Agreement.NumberDaysDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Комментарий"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "КонтролироватьСуммуЗадолженности", sale.ClientAgreement.Agreement.IsControlAmountDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "КонтролироватьЧислоДнейЗадолженности", sale.ClientAgreement.Agreement.IsControlNumberDaysDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НаименованиеДляПечати", sale.ClientAgreement.Agreement.Name));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.DATE,
                "СрокДействия"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "ФормаРасчетов"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование", sale.ClientAgreement.Agreement.Name));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.CASH_FLOW_ACTIVITIES_TYPES,
                "ВидДеятельностиДляДДС"));
            XElement agreementTypeCivilCode = CreatePropertyElement(TypeOfExchangeFile.TYPE_AGREEMENT_CIVIL_CODE, "ВидДоговораПоГК");
            XElement agreementTypeCivilCodeReference = CreateReference(referenceCounter["TypeAgreementCivilCodeId"]);
            agreementTypeCivilCodeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                sale.ClientAgreement.Agreement.AgreementTypeCivilCode?.CodeOneC ?? ""));
            agreementTypeCivilCode.Add(agreementTypeCivilCodeReference);
            agreementCounterParty.Add(agreementTypeCivilCode);
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ВыводитьИнформациюОСделкеПриПечатиДокументов",
                true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ДержатьРезервБезОплатыОграниченноеВремя",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "МногостороннееСоглашениеОРазделеПродукции",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "НеОтноситьНаЗатратыПоНУ",
                true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ОбособленныйУчетТоваровПоЗаказамПокупателей",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ARTICLE_CASH_FLOW,
                "ОсновнаяСтатьяДвиженияДенежныхСредств"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ПроцентКомиссионногоВознаграждения", 0));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ПроцентПредоплаты", 0));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "СложныйНалоговыйУчет",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.COMMISSION_CALCULATION_METHODS,
                "СпособРасчетаКомиссионногоВознаграждения"));
            XElement agreementTaxAccountingScheme = CreatePropertyElement(TypeOfExchangeFile.TAX_ACCOUNTING_SCHEME, "СхемаНалоговогоУчета");
            XElement agreementTaxAccountingSchemeReference = CreateReference(referenceCounter["TaxAccountingSchemeId"]);
            agreementTaxAccountingSchemeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING, "Код", sale.ClientAgreement.Agreement.TaxAccountingScheme?.CodeOneC ?? ""));
            agreementTaxAccountingScheme.Add(agreementTaxAccountingSchemeReference);
            agreementCounterParty.Add(agreementTaxAccountingScheme);
            XElement agreementTaxAccountingSchemeByContainer = CreatePropertyElement(TypeOfExchangeFile.TAX_ACCOUNTING_SCHEME, "СхемаНалоговогоУчетаПоТаре");
            XElement agreementTaxAccountingSchemeReferenceByContainer = CreateReference(referenceCounter["TaxAccountingSchemeId"]);
            agreementTaxAccountingSchemeReferenceByContainer.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING, "Код", sale.ClientAgreement.Agreement.TaxAccountingScheme?.CodeOneC ?? ""));
            agreementTaxAccountingSchemeByContainer.Add(agreementTaxAccountingSchemeReferenceByContainer);
            agreementCounterParty.Add(agreementTaxAccountingSchemeByContainer);
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.PAYMENT_FORMS, "ФормаРасчетовУпр"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER, "ЧислоДнейРезерваБезОплаты", sale.ClientAgreement.Agreement.NumberDaysDebt));

            XElement productsPart = new("ТабличнаяЧасть");
            productsPart.Add(new XAttribute("Имя", "Товары"));

            int lastIdInReferenceCounter = referenceCounter.Values.Max();

            int productId = ++lastIdInReferenceCounter;
            int unitMeasuresId = ++lastIdInReferenceCounter;
            int productSpecificationId = ++lastIdInReferenceCounter;
            int productGroupId = ++lastIdInReferenceCounter;
            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                XElement unitMeasures = CreateMeasuresUnitElement(orderItem.Product, productId, unitMeasuresId);

                XElement orderItemElement = CreateProductElementInRootObject(orderItem.Product, productId, unitMeasuresId);

                XElement productSpecification = CreateSpecificationProduct(orderItem.AssignedSpecification, productSpecificationId);

                productsPart.Add(CreateOrderItemForProductParts(orderItem, productId, unitMeasuresId, productSpecificationId));

                foreach (ProductProductGroup productProductGroup in orderItem.Product.ProductProductGroups) {
                    int rootProductGroupId = default;
                    ProductGroup rootProductGroup;
                    foreach (ProductGroup productGroup in productProductGroup.ProductGroups) {
                        rootProductGroup = productGroup;
                        if (productGroup == productProductGroup.ProductGroups.Last()) {
                            XElement productProductGroupElement = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Родитель");
                            productProductGroupElement.Add(new XAttribute("ИмяПКО", "Номенклатура"));
                            XElement productProductGroupReference = CreateReference(productGroupId);
                            productProductGroupReference.Add(CreatePropertyWithValueElement(
                                TypeOfExchangeFile.STRING,
                                "Наименование",
                                productGroup.FullName ?? productGroup.Name));
                            productProductGroupReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ЭтоГруппа", true));
                            productProductGroupReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Артикул", productGroup));
                            productProductGroupElement.Add(productProductGroupReference);
                            orderItemElement.Add(productProductGroupElement);

                            counterReference = productGroupId;
                        }

                        XElement productGroupElement;

                        if (productGroup == productProductGroup.ProductGroups.FirstOrDefault())
                            productGroupElement = CreateProductGroup(productGroup, productGroupId, true);
                        else
                            productGroupElement = CreateProductGroup(productGroup, productGroupId, false, rootProductGroupId, rootProductGroup);

                        rootProductGroupId = productGroupId;

                        rootObjectExchangeFile.Add(productGroupElement);

                        productGroupId++;
                    }
                }

                rootObjectExchangeFile.Add(unitMeasures);
                rootObjectExchangeFile.Add(orderItemElement);
                rootObjectExchangeFile.Add(productSpecification);

                productId = productGroupId++;
                unitMeasuresId = productGroupId++;
                productSpecificationId = productGroupId++;
            }

            XElement documentSale = CreateDocumentSaleObject(referenceCounter["DocumentId"]);
            XElement documentSaleReference = CreateReference(referenceCounter["DocumentId"]);
            documentSaleReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.DATE, "Дата", sale.Created.ToString("yyyy-MM-ddTHH:mm:ss")));
            documentSaleReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Номер", sale.SaleNumber.Value));
            documentSale.Add(documentSaleReference);
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "А_ФлагИнтеркомпани", false));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "АвторасчетНДС", false));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "АдресДоставки", sale.ClientAgreement.Client.DeliveryAddress));
            XElement documentSaleAccount = CreatePropertyElement(TypeOfExchangeFile.BANK_ACCOUNT, "БанковскийСчетОрганизации");
            XElement documentSaleAccountReference = CreateReference(referenceCounter["AccountId"]);
            documentSaleAccountReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НомерСчета",
                sale.ClientAgreement.Agreement.Organization.PaymentRegisters.FirstOrDefault()?.AccountNumber ?? ""));
            documentSaleAccount.Add(documentSaleAccountReference);
            documentSale.Add(documentSaleAccount);
            XElement documentSaleACurrency = CreatePropertyElement(TypeOfExchangeFile.CURRENCIES, "ВалютаДокумента");
            XElement documentSaleACurrencyReference = CreateReference(referenceCounter["CurrencyId"]);
            documentSaleACurrencyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                sale.ClientAgreement.Agreement.Currency.Id));
            documentSaleACurrency.Add(documentSaleACurrencyReference);
            documentSale.Add(documentSaleACurrency);
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "ВесБрутто"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "ВесНетто"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.ENUMERIC_TYPE_CLIENT, "ВидКлиента"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.COUNTERPARTY, "Грузоотправитель"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.RECEIVERS, "Грузополучатель"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.DATE, "ДоверенностьДата"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "ДоверенностьНомер"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "ДоверенностьСерия"));
            XElement documentSaleAgreement = CreatePropertyElement(TypeOfExchangeFile.AGREEMENT, "ДоговорКонтрагента");
            XElement documentSaleAgreementReference = CreateReference(referenceCounter["AgreementId"]);
            XElement documentSaleAgreementReferenceProperty = CreatePropertyElement(TypeOfExchangeFile.COUNTERPARTY, "Владелец");
            XElement documentSaleAgreementReferencePropertyReference = CreateReference(referenceCounter["CounterpartyId"]);
            documentSaleAgreementReferencePropertyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "КодПоЕДРПОУ",
                sale.ClientAgreement.Client.USREOU));
            documentSaleAgreementReferenceProperty.Add(documentSaleAgreementReferencePropertyReference);
            documentSaleAgreementReference.Add(documentSaleAgreementReferenceProperty);
            documentSaleAgreementReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.DATE, "Дата",
                sale.ClientAgreement.Agreement.Created.ToString("yyyy-MM-ddTHH:mm:ss")));
            documentSaleAgreementReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Номер", sale.ClientAgreement.Agreement.Number));
            documentSaleAgreement.Add(documentSaleAgreementReference);
            documentSale.Add(documentSaleAgreement);
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "ДокументПодтверждающийПолномочия"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "ДополнениеКАдресуДоставки"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ИспользоватьАртикулыКонтрагента", false));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "КоличествоПаллет", sale.ShipmentListItems.FirstOrDefault()?.QtyPlaces));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Комментарий", sale.Comment));
            XElement documentSaleCounterparty = CreatePropertyElement(TypeOfExchangeFile.COUNTERPARTY, "Контрагент");
            XElement documentSaleCounterpartyReference = CreateReference(referenceCounter["CounterpartyId"]);
            documentSaleCounterpartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "КодПоЕДРПОУ", sale.ClientAgreement.Client.USREOU));
            documentSaleCounterparty.Add(documentSaleCounterpartyReference);
            documentSale.Add(documentSaleCounterparty);
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "Корректировка", false));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "КратностьВзаиморасчетов", 1));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "КурсВзаиморасчетов",
                sale.ClientAgreement.Agreement.Currency.ExchangeRates.FirstOrDefault()?.Amount));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "Наличные", sale.IsVatSale.Equals(false)));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "НаложенныйПлатеж", false));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "НомерSAD", sale.SadId));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.CARRIERS, "Перевозчик"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Получил"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "СуммаВключаетНДС", sale.IsVatSale));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "СуммаДокумента", sale.TotalAmount));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "ТранспортноеСредство"));
            documentSale.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ТребуетОтправкиПеревозчиком",
                sale.Transporter == null || sale.Transporter?.Name != "Самовивіз"));
            documentSale.Add(productsPart);

            rootObjectExchangeFile.Add(currency);
            rootObjectExchangeFile.Add(account);
            rootObjectExchangeFile.Add(counterParty);
            rootObjectExchangeFile.Add(agreementCounterParty);
            rootObjectExchangeFile.Add(typeAgreementCivilCode);
            rootObjectExchangeFile.Add(taxAccountingScheme);
            rootObjectExchangeFile.Add(documentSale);
        }

        xmlDocument.Add(rootObjectExchangeFile);

        xmlDocument.Save(fileName);

        return xmlDocument;
    }

    public XDocument GetProductIncomeDocument(string pathToFolder, ProductIncomesModel productIncomesModel) {
        string fileName = Path.Combine(pathToFolder, "ProductIncome.xml");

        if (File.Exists(fileName)) File.Delete(fileName);

        XDocument xmlDocument = new();

        XElement rootObjectExchangeFile = CreateRootObjectExchangeFile();

        rootObjectExchangeFile.Add(CreateExchangeRules());

        int counterReference = 0;

        foreach (SupplyOrderUkraine supplyOrderUkraine in productIncomesModel.SupplyOrderUkraines) {
            Dictionary<string, int> referenceCounter = new();

            referenceCounter.Add("DocumentId", ++counterReference);

            referenceCounter.Add("CurrencyId", ++counterReference);
            XElement currency = CreateAdditionalObject(RuleNames.CURRENCIES, TypeOfExchangeFile.CURRENCIES, counterReference, true);
            XElement currencyReference = CreateReference(referenceCounter["CurrencyId"]);
            currencyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING,
                "Код",
                supplyOrderUkraine.ClientAgreement.Agreement.Currency.CodeOneC));
            currency.Add(currencyReference);
            currency.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                supplyOrderUkraine.ClientAgreement.Agreement.Currency.Name));
            currency.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НаименованиеПолное",
                supplyOrderUkraine.ClientAgreement.Agreement.Currency.Name));

            referenceCounter.Add("TypeAgreementCivilCodeId", ++counterReference);
            XElement typeAgreementCivilCode = CreateAdditionalObject(
                RuleNames.TYPE_AGREEMENT_CIVIL_CODE,
                TypeOfExchangeFile.TYPE_AGREEMENT_CIVIL_CODE,
                referenceCounter["TypeAgreementCivilCodeId"], false);
            XElement typeAgreementCivilCodeReference = CreateReference(referenceCounter["TypeAgreementCivilCodeId"]);
            typeAgreementCivilCodeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                supplyOrderUkraine.ClientAgreement.Agreement.AgreementTypeCivilCode?.CodeOneC ?? ""));
            typeAgreementCivilCode.Add(typeAgreementCivilCodeReference);
            typeAgreementCivilCode.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Наименование",
                supplyOrderUkraine.ClientAgreement.Agreement.AgreementTypeCivilCode?.NameUK ?? ""));
            typeAgreementCivilCode.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.TYPE_AGREEMENT_CIVIL_CODE, "Родитель"));

            referenceCounter.Add("TaxAccountingSchemeId", ++counterReference);
            XElement taxAccountingScheme = CreateAdditionalObject(
                RuleNames.TAX_ACCOUNTING_SCHEME,
                TypeOfExchangeFile.TAX_ACCOUNTING_SCHEME,
                referenceCounter["TaxAccountingSchemeId"], false);
            XElement taxAccountingSchemeReference = CreateReference(referenceCounter["TaxAccountingSchemeId"]);
            taxAccountingSchemeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                supplyOrderUkraine.ClientAgreement.Agreement.TaxAccountingScheme?.CodeOneC ?? ""));
            taxAccountingScheme.Add(taxAccountingSchemeReference);
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                supplyOrderUkraine.ClientAgreement.Agreement.TaxAccountingScheme?.NameUK ?? ""));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "МоментОпределенияБазыНДСПоПокупкам",
                GetTaxBaseMomentName(supplyOrderUkraine.ClientAgreement.Agreement.TaxAccountingScheme?.PurchaseTaxBaseMoment ?? TaxBaseMoment.NotDefine)));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "МоментОпределенияБазыНДСПоПродажам",
                GetTaxBaseMomentName(supplyOrderUkraine.ClientAgreement.Agreement.TaxAccountingScheme?.SaleTaxBaseMoment ?? TaxBaseMoment.NotDefine)));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "УдалитьМоментОпределенияБазыННППоПокупкам",
                GetTaxBaseMomentName(supplyOrderUkraine.ClientAgreement.Agreement.TaxAccountingScheme?.PurchaseTaxBaseMoment ?? TaxBaseMoment.NotDefine)));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "УдалитьМоментОпределенияБазыННППоПродажам",
                GetTaxBaseMomentName(supplyOrderUkraine.ClientAgreement.Agreement.TaxAccountingScheme?.SaleTaxBaseMoment ?? TaxBaseMoment.NotDefine)));

            referenceCounter.Add("CounterpartyId", ++counterReference);
            referenceCounter.Add("AgreementId", ++counterReference);
            XElement counterParty = CreateAdditionalObject(RuleNames.COUNTERPARTY, TypeOfExchangeFile.COUNTERPARTY, referenceCounter["CounterpartyId"], true);
            XElement counterPartyReference = CreateReference(referenceCounter["CounterpartyId"]);
            counterPartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING,
                "КодПоЕДРПОУ",
                supplyOrderUkraine.ClientAgreement.Client.USREOU));
            counterParty.Add(counterPartyReference);
            counterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "ИНН", supplyOrderUkraine.ClientAgreement.Client.TIN));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                supplyOrderUkraine.ClientAgreement.Client.Name ??
                $"{supplyOrderUkraine.ClientAgreement.Client.LastName} {supplyOrderUkraine.ClientAgreement.Client.FirstName} {supplyOrderUkraine.ClientAgreement.Client.MiddleName}"));
            counterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "НаименованиеПолное",
                supplyOrderUkraine.ClientAgreement.Client.FullName));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Комментарий"));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НомерСвидетельства"));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "ОсновнойБанковскийСчет"));
            XElement counterPartyAgreement = CreatePropertyElement(TypeOfExchangeFile.AGREEMENT, "ОсновнойДоговорКонтрагента");
            XElement counterPartyAgreementReference = CreateReference(referenceCounter["AgreementId"]);
            XElement counterPartyAgreementReferenceCounterparty = CreatePropertyElement(TypeOfExchangeFile.COUNTERPARTY, "Владелец");
            XElement counterPartyAgreementReferenceCounterpartyReference = CreateReference(referenceCounter["CounterpartyId"]);
            counterPartyAgreementReferenceCounterpartyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "КодПоЕДРПОУ",
                supplyOrderUkraine.ClientAgreement.Client.USREOU));
            counterPartyAgreementReferenceCounterparty.Add(counterPartyAgreementReferenceCounterpartyReference);
            counterPartyAgreementReference.Add(counterPartyAgreementReferenceCounterparty);
            counterPartyAgreementReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.DATE,
                "Дата",
                supplyOrderUkraine.ClientAgreement.Agreement.Created));
            counterPartyAgreementReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Номер",
                supplyOrderUkraine.ClientAgreement.Agreement.Number));
            counterPartyAgreement.Add(counterPartyAgreementReference);
            counterParty.Add(counterPartyAgreement);
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "Покупатель", true));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "Поставщик", false));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_CLIENT_IS_INVIDUALS,
                "ЮрФизЛицо", supplyOrderUkraine.ClientAgreement.Client.IsIndividual ? "ФизЛицо" : "ЮрЛицо"));

            XElement agreementCounterParty = CreateAdditionalObject(RuleNames.AGREEMENT, TypeOfExchangeFile.AGREEMENT, referenceCounter["AgreementId"], true);
            XElement agreementCounterPartyReference = CreateReference(referenceCounter["AgreementId"]);
            XElement agreementCounterPartyReferenceProperty = CreatePropertyElement(
                TypeOfExchangeFile.COUNTERPARTY, "Владелец");
            XElement agreementCounterPartyReferencePropertyReference = CreateReference(referenceCounter["CounterpartyId"]);
            agreementCounterPartyReferencePropertyReference.Add(
                CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "КодПоЕДРПОУ", supplyOrderUkraine.ClientAgreement.Client.USREOU));
            agreementCounterPartyReferenceProperty.Add(agreementCounterPartyReferencePropertyReference);
            agreementCounterPartyReference.Add(agreementCounterPartyReferenceProperty);
            agreementCounterPartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.DATE, "Дата",
                supplyOrderUkraine.ClientAgreement.Agreement.Created.ToString("yyyy-MM-ddTHH:mm:ss")));
            agreementCounterPartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Номер", supplyOrderUkraine.ClientAgreement.Agreement.Number ?? ""));
            agreementCounterParty.Add(agreementCounterPartyReference);
            XElement agreementCounterPartyProperty = CreatePropertyElement(TypeOfExchangeFile.CURRENCIES, "ВалютаВзаиморасчетов");
            XElement agreementCounterPartyPropertyReference = CreateReference(referenceCounter["CurrencyId"]);
            agreementCounterPartyPropertyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING, "Код", supplyOrderUkraine.ClientAgreement.Agreement.Currency.Id));
            agreementCounterPartyProperty.Add(agreementCounterPartyPropertyReference);
            agreementCounterParty.Add(agreementCounterPartyProperty);
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_TYPE_MUTUAL_SETTLEMENTS, "ВедениеВзаиморасчетов", "ПоДоговоруВЦелом"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ВестиПоДокументамРасчетовСКонтрагентом", true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ВестиПоДокументамРасчетовСКонтрагентомРегл", true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_TYPE_AGREEMENT,
                "ВидДоговора", "СПоставщиком"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_TYPE_CONDITION_AGREEMENT,
                "ВидУсловийДоговора", "БезДополнительныхУсловий"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "Внешнеэкономический", !supplyOrderUkraine.ClientAgreement.Agreement.Currency.Code.Equals("UAH")));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ДопустимаяСуммаЗадолженности", supplyOrderUkraine.ClientAgreement.Agreement.AmountDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ДопустимоеЧислоДнейЗадолженности", supplyOrderUkraine.ClientAgreement.Agreement.NumberDaysDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Комментарий"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "КонтролироватьСуммуЗадолженности", supplyOrderUkraine.ClientAgreement.Agreement.IsControlAmountDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "КонтролироватьЧислоДнейЗадолженности", supplyOrderUkraine.ClientAgreement.Agreement.IsControlNumberDaysDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НаименованиеДляПечати", supplyOrderUkraine.ClientAgreement.Agreement.Name));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.DATE,
                "СрокДействия"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "ФормаРасчетов", "Оплата с поточного рахунку"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование", supplyOrderUkraine.ClientAgreement.Agreement.Name));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.CASH_FLOW_ACTIVITIES_TYPES,
                "ВидДеятельностиДляДДС"));
            XElement agreementTypeCivilCode = CreatePropertyElement(TypeOfExchangeFile.TYPE_AGREEMENT_CIVIL_CODE, "ВидДоговораПоГК");
            XElement agreementTypeCivilCodeReference = CreateReference(referenceCounter["TypeAgreementCivilCodeId"]);
            agreementTypeCivilCodeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                supplyOrderUkraine.ClientAgreement.Agreement.AgreementTypeCivilCode?.CodeOneC ?? ""));
            agreementTypeCivilCode.Add(agreementTypeCivilCodeReference);
            agreementCounterParty.Add(agreementTypeCivilCode);
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ВыводитьИнформациюОСделкеПриПечатиДокументов",
                true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ДержатьРезервБезОплатыОграниченноеВремя",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "МногостороннееСоглашениеОРазделеПродукции",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "НеОтноситьНаЗатратыПоНУ",
                true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ОбособленныйУчетТоваровПоЗаказамПокупателей",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ARTICLE_CASH_FLOW,
                "ОсновнаяСтатьяДвиженияДенежныхСредств"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ПроцентКомиссионногоВознаграждения", 0));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ПроцентПредоплаты", 0));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "СложныйНалоговыйУчет",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.COMMISSION_CALCULATION_METHODS,
                "СпособРасчетаКомиссионногоВознаграждения"));
            XElement agreementTaxAccountingScheme = CreatePropertyElement(TypeOfExchangeFile.TAX_ACCOUNTING_SCHEME, "СхемаНалоговогоУчета");
            XElement agreementTaxAccountingSchemeReference = CreateReference(referenceCounter["TaxAccountingSchemeId"]);
            agreementTaxAccountingSchemeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING, "Код", supplyOrderUkraine.ClientAgreement.Agreement.TaxAccountingScheme?.CodeOneC ?? ""));
            agreementTaxAccountingScheme.Add(agreementTaxAccountingSchemeReference);
            agreementCounterParty.Add(agreementTaxAccountingScheme);
            XElement agreementTaxAccountingSchemeByContainer = CreatePropertyElement(TypeOfExchangeFile.TAX_ACCOUNTING_SCHEME, "СхемаНалоговогоУчетаПоТаре");
            XElement agreementTaxAccountingSchemeReferenceByContainer = CreateReference(referenceCounter["TaxAccountingSchemeId"]);
            agreementTaxAccountingSchemeReferenceByContainer.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING, "Код", supplyOrderUkraine.ClientAgreement.Agreement.TaxAccountingScheme?.CodeOneC ?? ""));
            agreementTaxAccountingSchemeByContainer.Add(agreementTaxAccountingSchemeReferenceByContainer);
            agreementCounterParty.Add(agreementTaxAccountingSchemeByContainer);
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.PAYMENT_FORMS, "ФормаРасчетовУпр"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER, "ЧислоДнейРезерваБезОплаты", supplyOrderUkraine.ClientAgreement.Agreement.NumberDaysDebt));

            XElement productsPart = new("ТабличнаяЧасть");
            productsPart.Add(new XAttribute("Имя", "Товары"));

            int lastIdInReferenceCounter = referenceCounter.Values.Max();

            int productId = ++lastIdInReferenceCounter;
            int unitMeasuresId = ++lastIdInReferenceCounter;
            int productSpecificationId = ++lastIdInReferenceCounter;
            int productGroupId = ++lastIdInReferenceCounter;
            foreach (SupplyOrderUkraineItem supplyOrderUkraineItem in supplyOrderUkraine.SupplyOrderUkraineItems) {
                XElement unitMeasures = CreateMeasuresUnitElement(supplyOrderUkraineItem.Product, productId, unitMeasuresId);

                XElement orderItemElement = CreateProductElementInRootObject(supplyOrderUkraineItem.Product, productId, unitMeasuresId);

                XElement productSpecification = CreateSpecificationProduct(supplyOrderUkraineItem.Product.ProductSpecifications?.FirstOrDefault(), productSpecificationId);

                productsPart.Add(CreateSupplyOrderUkraineItemForProductParts(supplyOrderUkraineItem, productId, unitMeasuresId, productSpecificationId));

                foreach (ProductProductGroup productProductGroup in supplyOrderUkraineItem.Product.ProductProductGroups) {
                    int rootProductGroupId = default;
                    ProductGroup rootProductGroup;
                    foreach (ProductGroup productGroup in productProductGroup.ProductGroups) {
                        rootProductGroup = productGroup;
                        if (productGroup == productProductGroup.ProductGroups.Last()) {
                            XElement productProductGroupElement = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Родитель");
                            productProductGroupElement.Add(new XAttribute("ИмяПКО", "Номенклатура"));
                            XElement productProductGroupReference = CreateReference(productGroupId);
                            productProductGroupReference.Add(CreatePropertyWithValueElement(
                                TypeOfExchangeFile.STRING,
                                "Наименование",
                                productGroup.FullName ?? productGroup.Name));
                            productProductGroupReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ЭтоГруппа", true));
                            productProductGroupReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Артикул", productGroup));
                            productProductGroupElement.Add(productProductGroupReference);
                            orderItemElement.Add(productProductGroupElement);

                            counterReference = productGroupId;
                        }

                        XElement productGroupElement;

                        if (productGroup == productProductGroup.ProductGroups.FirstOrDefault())
                            productGroupElement = CreateProductGroup(productGroup, productGroupId, true);
                        else
                            productGroupElement = CreateProductGroup(productGroup, productGroupId, false, rootProductGroupId, rootProductGroup);

                        rootProductGroupId = productGroupId;

                        rootObjectExchangeFile.Add(productGroupElement);

                        productGroupId++;
                    }
                }

                rootObjectExchangeFile.Add(unitMeasures);
                rootObjectExchangeFile.Add(orderItemElement);
                rootObjectExchangeFile.Add(productSpecification);

                productId = productGroupId++;
                unitMeasuresId = productGroupId++;
                productSpecificationId = productGroupId++;
            }

            XElement supplyOrderUkraineElement = CreateDocumentIncomeObject(referenceCounter["DocumentId"]);
            XElement documentSaleReference = CreateReference(referenceCounter["DocumentId"]);
            documentSaleReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.DATE, "Дата", supplyOrderUkraine.Created.ToString("yyyy-MM-ddTHH:mm:ss")));
            documentSaleReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Номер", supplyOrderUkraine.Number));
            supplyOrderUkraineElement.Add(documentSaleReference);
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BANK_ACCOUNT, "БанковскийСчетКонтрагента"));
            XElement documentSaleACurrency = CreatePropertyElement(TypeOfExchangeFile.CURRENCIES, "ВалютаДокумента");
            XElement documentSaleACurrencyReference = CreateReference(referenceCounter["CurrencyId"]);
            documentSaleACurrencyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                supplyOrderUkraine.ClientAgreement.Agreement.Currency.Id));
            documentSaleACurrency.Add(documentSaleACurrencyReference);
            supplyOrderUkraineElement.Add(documentSaleACurrency);
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.COUNTERPARTY, "Грузоотправитель"));
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.RECEIVERS,
                "Грузополучатель"));
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.DATE,
                "ДатаВходящегоДокумента"));
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "КоэффициентБрутто"));
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "КратностьВзаиморасчетов", 1));
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "КурсВзаиморасчетов", supplyOrderUkraine.ClientAgreement.Agreement.Currency.ExchangeRates.FirstOrDefault()?.Amount));
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "НомерВходящегоДокумента"));
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НомерOGL"));
            XElement documentSupplyOrderUkraineAgreement = CreatePropertyElement(TypeOfExchangeFile.AGREEMENT, "ДоговорКонтрагента");
            XElement documentSupplyOrderUkraineAgreementReference = CreateReference(referenceCounter["AgreementId"]);
            XElement documentSupplyOrderUkraineAgreementReferenceProperty = CreatePropertyElement(TypeOfExchangeFile.COUNTERPARTY, "Владелец");
            XElement documentSupplyOrderUkraineAgreementReferencePropertyReference = CreateReference(referenceCounter["CounterpartyId"]);
            documentSupplyOrderUkraineAgreementReferencePropertyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "КодПоЕДРПОУ",
                supplyOrderUkraine.ClientAgreement.Client.USREOU));
            documentSupplyOrderUkraineAgreementReferenceProperty.Add(documentSupplyOrderUkraineAgreementReferencePropertyReference);
            documentSupplyOrderUkraineAgreementReference.Add(documentSupplyOrderUkraineAgreementReferenceProperty);
            documentSupplyOrderUkraineAgreementReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.DATE, "Дата",
                supplyOrderUkraine.ClientAgreement.Agreement.Created.ToString("yyyy-MM-ddTHH:mm:ss")));
            documentSupplyOrderUkraineAgreementReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Номер",
                supplyOrderUkraine.ClientAgreement.Agreement.Number));
            documentSupplyOrderUkraineAgreement.Add(documentSupplyOrderUkraineAgreementReference);
            supplyOrderUkraineElement.Add(documentSupplyOrderUkraineAgreement);
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Комментарий", supplyOrderUkraine.Comment));
            XElement documentSupplyOrderUkraineCounterparty = CreatePropertyElement(TypeOfExchangeFile.COUNTERPARTY, "Контрагент");
            XElement documentSupplyOrderUkraineCounterpartyReference = CreateReference(referenceCounter["CounterpartyId"]);
            documentSupplyOrderUkraineCounterpartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "КодПоЕДРПОУ",
                supplyOrderUkraine.ClientAgreement.Client.USREOU));
            documentSupplyOrderUkraineCounterparty.Add(documentSupplyOrderUkraineCounterpartyReference);
            supplyOrderUkraineElement.Add(documentSupplyOrderUkraineCounterparty);
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "СуммаВключаетНДС", false));
            supplyOrderUkraineElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "СуммаДокумента", supplyOrderUkraine.TotalNetPrice));
            supplyOrderUkraineElement.Add(productsPart);

            rootObjectExchangeFile.Add(currency);
            rootObjectExchangeFile.Add(counterParty);
            rootObjectExchangeFile.Add(agreementCounterParty);
            rootObjectExchangeFile.Add(typeAgreementCivilCode);
            rootObjectExchangeFile.Add(taxAccountingScheme);
            rootObjectExchangeFile.Add(supplyOrderUkraineElement);
        }

        foreach (SupplyOrder supplyOrder in productIncomesModel.SupplyOrders) {
            Dictionary<string, int> referenceCounter = new();
            referenceCounter.Add("DocumentId", ++counterReference);

            referenceCounter.Add("CurrencyId", ++counterReference);
            XElement currency = CreateAdditionalObject(RuleNames.CURRENCIES, TypeOfExchangeFile.CURRENCIES, counterReference, true);
            XElement currencyReference = CreateReference(referenceCounter["CurrencyId"]);
            currencyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING,
                "Код",
                supplyOrder.ClientAgreement.Agreement.Currency.CodeOneC));
            currency.Add(currencyReference);
            currency.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                supplyOrder.ClientAgreement.Agreement.Currency.Name));
            currency.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НаименованиеПолное",
                supplyOrder.ClientAgreement.Agreement.Currency.Name));

            referenceCounter.Add("TypeAgreementCivilCodeId", ++counterReference);
            XElement typeAgreementCivilCode = CreateAdditionalObject(
                RuleNames.TYPE_AGREEMENT_CIVIL_CODE,
                TypeOfExchangeFile.TYPE_AGREEMENT_CIVIL_CODE,
                referenceCounter["TypeAgreementCivilCodeId"], false);
            XElement typeAgreementCivilCodeReference = CreateReference(referenceCounter["TypeAgreementCivilCodeId"]);
            typeAgreementCivilCodeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                supplyOrder.ClientAgreement.Agreement.AgreementTypeCivilCode?.CodeOneC ?? ""));
            typeAgreementCivilCode.Add(typeAgreementCivilCodeReference);
            typeAgreementCivilCode.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Наименование",
                supplyOrder.ClientAgreement.Agreement.AgreementTypeCivilCode?.NameUK ?? ""));
            typeAgreementCivilCode.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.TYPE_AGREEMENT_CIVIL_CODE, "Родитель"));

            referenceCounter.Add("TaxAccountingSchemeId", ++counterReference);
            XElement taxAccountingScheme = CreateAdditionalObject(
                RuleNames.TAX_ACCOUNTING_SCHEME,
                TypeOfExchangeFile.TAX_ACCOUNTING_SCHEME,
                referenceCounter["TaxAccountingSchemeId"], false);
            XElement taxAccountingSchemeReference = CreateReference(referenceCounter["TaxAccountingSchemeId"]);
            taxAccountingSchemeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                supplyOrder.ClientAgreement.Agreement.TaxAccountingScheme?.CodeOneC ?? ""));
            taxAccountingScheme.Add(taxAccountingSchemeReference);
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                supplyOrder.ClientAgreement.Agreement.TaxAccountingScheme?.NameUK ?? ""));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "МоментОпределенияБазыНДСПоПокупкам",
                GetTaxBaseMomentName(supplyOrder.ClientAgreement.Agreement.TaxAccountingScheme?.PurchaseTaxBaseMoment ?? TaxBaseMoment.NotDefine)));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "МоментОпределенияБазыНДСПоПродажам",
                GetTaxBaseMomentName(supplyOrder.ClientAgreement.Agreement.TaxAccountingScheme?.SaleTaxBaseMoment ?? TaxBaseMoment.NotDefine)));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "УдалитьМоментОпределенияБазыННППоПокупкам",
                GetTaxBaseMomentName(supplyOrder.ClientAgreement.Agreement.TaxAccountingScheme?.PurchaseTaxBaseMoment ?? TaxBaseMoment.NotDefine)));
            taxAccountingScheme.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
                "УдалитьМоментОпределенияБазыННППоПродажам",
                GetTaxBaseMomentName(supplyOrder.ClientAgreement.Agreement.TaxAccountingScheme?.SaleTaxBaseMoment ?? TaxBaseMoment.NotDefine)));

            referenceCounter.Add("CounterpartyId", ++counterReference);
            referenceCounter.Add("AgreementId", ++counterReference);
            XElement counterParty = CreateAdditionalObject(RuleNames.COUNTERPARTY, TypeOfExchangeFile.COUNTERPARTY, referenceCounter["CounterpartyId"], true);
            XElement counterPartyReference = CreateReference(referenceCounter["CounterpartyId"]);
            counterPartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING,
                "КодПоЕДРПОУ",
                supplyOrder.Client.USREOU));
            counterParty.Add(counterPartyReference);
            counterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "ИНН", supplyOrder.Client.TIN));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                supplyOrder.Client.Name ??
                $"{supplyOrder.Client.LastName} {supplyOrder.Client.FirstName} {supplyOrder.Client.MiddleName}"));
            counterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "НаименованиеПолное",
                supplyOrder.Client.FullName));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Комментарий", supplyOrder.Client.Comment));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НомерСвидетельства"));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "ОсновнойБанковскийСчет"));
            XElement counterPartyAgreement = CreatePropertyElement(TypeOfExchangeFile.AGREEMENT, "ОсновнойДоговорКонтрагента");
            XElement counterPartyAgreementReference = CreateReference(referenceCounter["AgreementId"]);
            XElement counterPartyAgreementReferenceCounterparty = CreatePropertyElement(TypeOfExchangeFile.COUNTERPARTY, "Владелец");
            XElement counterPartyAgreementReferenceCounterpartyReference = CreateReference(referenceCounter["CounterpartyId"]);
            counterPartyAgreementReferenceCounterpartyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "КодПоЕДРПОУ",
                supplyOrder.Client.USREOU));
            counterPartyAgreementReferenceCounterparty.Add(counterPartyAgreementReferenceCounterpartyReference);
            counterPartyAgreementReference.Add(counterPartyAgreementReferenceCounterparty);
            counterPartyAgreementReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.DATE,
                "Дата",
                supplyOrder.ClientAgreement.Agreement.Created));
            counterPartyAgreementReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Номер",
                supplyOrder.ClientAgreement.Agreement.Number));
            counterPartyAgreement.Add(counterPartyAgreementReference);
            counterParty.Add(counterPartyAgreement);
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "Покупатель", false));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "Поставщик", true));
            counterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_CLIENT_IS_INVIDUALS,
                "ЮрФизЛицо", supplyOrder.Client.IsIndividual ? "ФизЛицо" : "ЮрЛицо"));

            XElement agreementCounterParty = CreateAdditionalObject(RuleNames.AGREEMENT, TypeOfExchangeFile.AGREEMENT, referenceCounter["AgreementId"], true);
            XElement agreementCounterPartyReference = CreateReference(referenceCounter["AgreementId"]);
            XElement agreementCounterPartyReferenceProperty = CreatePropertyElement(
                TypeOfExchangeFile.COUNTERPARTY, "Владелец");
            XElement agreementCounterPartyReferencePropertyReference = CreateReference(referenceCounter["CounterpartyId"]);
            agreementCounterPartyReferencePropertyReference.Add(
                CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "КодПоЕДРПОУ", supplyOrder.Client.USREOU));
            agreementCounterPartyReferenceProperty.Add(agreementCounterPartyReferencePropertyReference);
            agreementCounterPartyReference.Add(agreementCounterPartyReferenceProperty);
            agreementCounterPartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.DATE, "Дата",
                supplyOrder.ClientAgreement.Agreement.Created.ToString("yyyy-MM-ddTHH:mm:ss")));
            agreementCounterPartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Номер", supplyOrder.ClientAgreement.Agreement.Number ?? ""));
            agreementCounterParty.Add(agreementCounterPartyReference);
            XElement agreementCounterPartyProperty = CreatePropertyElement(TypeOfExchangeFile.CURRENCIES, "ВалютаВзаиморасчетов");
            XElement agreementCounterPartyPropertyReference = CreateReference(referenceCounter["CurrencyId"]);
            agreementCounterPartyPropertyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING, "Код", supplyOrder.ClientAgreement.Agreement.Currency.Id));
            agreementCounterPartyProperty.Add(agreementCounterPartyPropertyReference);
            agreementCounterParty.Add(agreementCounterPartyProperty);
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_TYPE_MUTUAL_SETTLEMENTS, "ВедениеВзаиморасчетов", "ПоДоговоруВЦелом"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ВестиПоДокументамРасчетовСКонтрагентом", true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ВестиПоДокументамРасчетовСКонтрагентомРегл", true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_TYPE_AGREEMENT,
                "ВидДоговора", "СПоставщиком"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ENUMERIC_TYPE_CONDITION_AGREEMENT,
                "ВидУсловийДоговора", "БезДополнительныхУсловий"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "Внешнеэкономический", !supplyOrder.ClientAgreement.Agreement.Currency.Code.Equals("UAH")));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ДопустимаяСуммаЗадолженности", supplyOrder.ClientAgreement.Agreement.AmountDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ДопустимоеЧислоДнейЗадолженности", supplyOrder.ClientAgreement.Agreement.NumberDaysDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Комментарий"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "КонтролироватьСуммуЗадолженности", supplyOrder.ClientAgreement.Agreement.IsControlAmountDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "КонтролироватьЧислоДнейЗадолженности", supplyOrder.ClientAgreement.Agreement.IsControlNumberDaysDebt));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НаименованиеДляПечати", supplyOrder.ClientAgreement.Agreement.Name));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.DATE,
                "СрокДействия"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "ФормаРасчетов", "Оплата с поточного рахунку"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование", supplyOrder.ClientAgreement.Agreement.Name));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.CASH_FLOW_ACTIVITIES_TYPES,
                "ВидДеятельностиДляДДС"));
            XElement agreementTypeCivilCode = CreatePropertyElement(TypeOfExchangeFile.TYPE_AGREEMENT_CIVIL_CODE, "ВидДоговораПоГК");
            XElement agreementTypeCivilCodeReference = CreateReference(referenceCounter["TypeAgreementCivilCodeId"]);
            agreementTypeCivilCodeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                supplyOrder.ClientAgreement.Agreement.AgreementTypeCivilCode?.CodeOneC ?? ""));
            agreementTypeCivilCode.Add(agreementTypeCivilCodeReference);
            agreementCounterParty.Add(agreementTypeCivilCode);
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ВыводитьИнформациюОСделкеПриПечатиДокументов",
                true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ДержатьРезервБезОплатыОграниченноеВремя",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "МногостороннееСоглашениеОРазделеПродукции",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "НеОтноситьНаЗатратыПоНУ",
                true));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ОбособленныйУчетТоваровПоЗаказамПокупателей",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.ARTICLE_CASH_FLOW,
                "ОсновнаяСтатьяДвиженияДенежныхСредств"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ПроцентКомиссионногоВознаграждения", 0));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "ПроцентПредоплаты", 0));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "СложныйНалоговыйУчет",
                false));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.COMMISSION_CALCULATION_METHODS,
                "СпособРасчетаКомиссионногоВознаграждения"));
            XElement agreementTaxAccountingScheme = CreatePropertyElement(TypeOfExchangeFile.TAX_ACCOUNTING_SCHEME, "СхемаНалоговогоУчета");
            XElement agreementTaxAccountingSchemeReference = CreateReference(referenceCounter["TaxAccountingSchemeId"]);
            agreementTaxAccountingSchemeReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING, "Код", supplyOrder.ClientAgreement.Agreement.TaxAccountingScheme?.CodeOneC ?? ""));
            agreementTaxAccountingScheme.Add(agreementTaxAccountingSchemeReference);
            agreementCounterParty.Add(agreementTaxAccountingScheme);
            XElement agreementTaxAccountingSchemeByContainer = CreatePropertyElement(TypeOfExchangeFile.TAX_ACCOUNTING_SCHEME, "СхемаНалоговогоУчетаПоТаре");
            XElement agreementTaxAccountingSchemeReferenceByContainer = CreateReference(referenceCounter["TaxAccountingSchemeId"]);
            agreementTaxAccountingSchemeReferenceByContainer.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING, "Код", supplyOrder.ClientAgreement.Agreement.TaxAccountingScheme?.CodeOneC ?? ""));
            agreementTaxAccountingSchemeByContainer.Add(agreementTaxAccountingSchemeReferenceByContainer);
            agreementCounterParty.Add(agreementTaxAccountingSchemeByContainer);
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.PAYMENT_FORMS, "ФормаРасчетовУпр"));
            agreementCounterParty.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER, "ЧислоДнейРезерваБезОплаты", supplyOrder.ClientAgreement.Agreement.NumberDaysDebt));

            XElement productsPart = new("ТабличнаяЧасть");
            productsPart.Add(new XAttribute("Имя", "Товары"));

            int lastIdInReferenceCounter = referenceCounter.Values.Max();

            int productId = ++lastIdInReferenceCounter;
            int unitMeasuresId = ++lastIdInReferenceCounter;
            int productSpecificationId = ++lastIdInReferenceCounter;
            int productGroupId = ++lastIdInReferenceCounter;
            foreach (SupplyOrderItem supplyOrderItem in supplyOrder.SupplyOrderItems) {
                XElement unitMeasures = CreateMeasuresUnitElement(supplyOrderItem.Product, productId, unitMeasuresId);

                XElement orderItemElement = CreateProductElementInRootObject(supplyOrderItem.Product, productId, unitMeasuresId);

                XElement productSpecification = CreateSpecificationProduct(supplyOrderItem.Product.ProductSpecifications?.FirstOrDefault(), productSpecificationId);

                productsPart.Add(CreateSupplyOrderItemForProductParts(supplyOrderItem, productId, unitMeasuresId, productSpecificationId));

                foreach (ProductProductGroup productProductGroup in supplyOrderItem.Product.ProductProductGroups) {
                    int rootProductGroupId = default;
                    ProductGroup rootProductGroup;
                    foreach (ProductGroup productGroup in productProductGroup.ProductGroups) {
                        rootProductGroup = productGroup;
                        if (productGroup == productProductGroup.ProductGroups.Last()) {
                            XElement productProductGroupElement = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Родитель");
                            productProductGroupElement.Add(new XAttribute("ИмяПКО", "Номенклатура"));
                            XElement productProductGroupReference = CreateReference(productGroupId);
                            productProductGroupReference.Add(CreatePropertyWithValueElement(
                                TypeOfExchangeFile.STRING,
                                "Наименование",
                                productGroup.FullName ?? productGroup.Name));
                            productProductGroupReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ЭтоГруппа", true));
                            productProductGroupReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Артикул", productGroup));
                            productProductGroupElement.Add(productProductGroupReference);
                            orderItemElement.Add(productProductGroupElement);

                            counterReference = productGroupId;
                        }

                        XElement productGroupElement;

                        if (productGroup == productProductGroup.ProductGroups.FirstOrDefault())
                            productGroupElement = CreateProductGroup(productGroup, productGroupId, true);
                        else
                            productGroupElement = CreateProductGroup(productGroup, productGroupId, false, rootProductGroupId, rootProductGroup);

                        rootProductGroupId = productGroupId;

                        rootObjectExchangeFile.Add(productGroupElement);

                        productGroupId++;
                    }
                }

                rootObjectExchangeFile.Add(unitMeasures);
                rootObjectExchangeFile.Add(orderItemElement);
                rootObjectExchangeFile.Add(productSpecification);

                productId = productGroupId++;
                unitMeasuresId = productGroupId++;
                productSpecificationId = productGroupId++;
            }

            XElement supplyOrderElement = CreateDocumentIncomeObject(referenceCounter["DocumentId"]);
            XElement documentSaleReference = CreateReference(referenceCounter["DocumentId"]);
            documentSaleReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.DATE, "Дата", supplyOrder.Created.ToString("yyyy-MM-ddTHH:mm:ss")));
            documentSaleReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Номер", supplyOrder.SupplyOrderNumber.Number));
            supplyOrderElement.Add(documentSaleReference);
            supplyOrderElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BANK_ACCOUNT, "БанковскийСчетКонтрагента"));
            XElement documentSupplyOrderACurrency = CreatePropertyElement(TypeOfExchangeFile.CURRENCIES, "ВалютаДокумента");
            XElement documentSupplyOrderACurrencyReference = CreateReference(referenceCounter["CurrencyId"]);
            documentSupplyOrderACurrencyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Код",
                supplyOrder.ClientAgreement.Agreement.Currency.Id));
            documentSupplyOrderACurrency.Add(documentSupplyOrderACurrencyReference);
            supplyOrderElement.Add(documentSupplyOrderACurrency);
            supplyOrderElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.COUNTERPARTY, "Грузоотправитель"));
            supplyOrderElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.RECEIVERS,
                "Грузополучатель"));
            supplyOrderElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.DATE,
                "ДатаВходящегоДокумента"));
            supplyOrderElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "КоэффициентБрутто"));
            supplyOrderElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "КратностьВзаиморасчетов", 1));
            supplyOrderElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "КурсВзаиморасчетов", supplyOrder.ClientAgreement.Agreement.Currency.ExchangeRates.FirstOrDefault()?.Amount));
            supplyOrderElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.NUMBER,
                "НомерВходящегоДокумента"));
            supplyOrderElement.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "НомерOGL"));
            XElement documentSupplyOrderUkraineAgreement = CreatePropertyElement(TypeOfExchangeFile.AGREEMENT, "ДоговорКонтрагента");
            XElement documentSupplyOrderAgreementReference = CreateReference(referenceCounter["AgreementId"]);
            XElement documentSupplyOrderAgreementReferenceProperty = CreatePropertyElement(TypeOfExchangeFile.COUNTERPARTY, "Владелец");
            XElement documentSupplyOrderAgreementReferencePropertyReference = CreateReference(referenceCounter["CounterpartyId"]);
            documentSupplyOrderAgreementReferencePropertyReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "КодПоЕДРПОУ",
                supplyOrder.Client.USREOU));
            documentSupplyOrderAgreementReferenceProperty.Add(documentSupplyOrderAgreementReferencePropertyReference);
            documentSupplyOrderAgreementReference.Add(documentSupplyOrderAgreementReferenceProperty);
            documentSupplyOrderAgreementReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.DATE, "Дата",
                supplyOrder.ClientAgreement.Agreement.Created.ToString("yyyy-MM-ddTHH:mm:ss")));
            documentSupplyOrderAgreementReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Номер", supplyOrder.ClientAgreement.Agreement.Number));
            documentSupplyOrderUkraineAgreement.Add(documentSupplyOrderAgreementReference);
            supplyOrderElement.Add(documentSupplyOrderUkraineAgreement);
            supplyOrderElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Комментарий", supplyOrder.Comment));
            XElement documentSupplyOrderCounterparty = CreatePropertyElement(TypeOfExchangeFile.COUNTERPARTY, "Контрагент");
            XElement documentSupplyOrderCounterpartyReference = CreateReference(referenceCounter["CounterpartyId"]);
            documentSupplyOrderCounterpartyReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "КодПоЕДРПОУ", supplyOrder.Client.USREOU));
            documentSupplyOrderCounterparty.Add(documentSupplyOrderCounterpartyReference);
            supplyOrderElement.Add(documentSupplyOrderCounterparty);
            supplyOrderElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "СуммаВключаетНДС", false));
            supplyOrderElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "СуммаДокумента", supplyOrder.NetPrice));
            supplyOrderElement.Add(productsPart);

            rootObjectExchangeFile.Add(currency);
            rootObjectExchangeFile.Add(counterParty);
            rootObjectExchangeFile.Add(agreementCounterParty);
            rootObjectExchangeFile.Add(typeAgreementCivilCode);
            rootObjectExchangeFile.Add(taxAccountingScheme);
            rootObjectExchangeFile.Add(supplyOrderElement);
        }

        xmlDocument.Add(rootObjectExchangeFile);

        xmlDocument.Save(fileName);

        return xmlDocument;
    }

    private XElement CreateRootObjectExchangeFile() {
        XElement exchangeFile = new("ФайлОбмена");
        exchangeFile.Add(new XAttribute("Комментарий", ""));
        exchangeFile.Add(new XAttribute("ИдПравилКонвертации", "2b353ca9-03dd-4630-bace-1762242b1346"));
        exchangeFile.Add(new XAttribute("ИмяКонфигурацииПриемника", "УправлениеТорговымПредприятиемДляУкраины"));
        exchangeFile.Add(new XAttribute("ИмяКонфигурацииИсточника", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")));
        exchangeFile.Add(new XAttribute("ОкончаниеПериодаВыгрузки", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")));
        exchangeFile.Add(new XAttribute("НачалоПериодаВыгрузки", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")));
        exchangeFile.Add(new XAttribute("ДатаВыгрузки", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")));
        exchangeFile.Add(new XAttribute("ВерсияФормата", 2.0));
        return exchangeFile;
    }

    private XElement CreateAdditionalObject(string ruleName, string typeName, int referenceCount, bool isReplace) {
        XElement additionalObject = new("Объект");
        additionalObject.Add(new XAttribute("Нпп", referenceCount));
        additionalObject.Add(new XAttribute("Тип", typeName));
        additionalObject.Add(new XAttribute("ИмяПравила", ruleName));
        if (isReplace)
            additionalObject.Add(new XAttribute("НеЗамещать", true));

        return additionalObject;
    }

    private XElement CreateDocumentSaleObject(int referenceCount) {
        XElement documentSale = new("Объект");
        documentSale.Add(new XAttribute("ИмяПравила", RuleNames.SALE));
        documentSale.Add(new XAttribute("Тип", TypeOfExchangeFile.DOCUMENT_SALE));
        documentSale.Add(new XAttribute("Нпп", referenceCount));
        return documentSale;
    }

    private XElement CreateDocumentIncomeObject(int referenceCount) {
        XElement documentSale = new("Объект");
        documentSale.Add(new XAttribute("ИмяПравила", RuleNames.INCOME));
        documentSale.Add(new XAttribute("Тип", TypeOfExchangeFile.DOCUMENT_INCOME));
        documentSale.Add(new XAttribute("Нпп", referenceCount));
        return documentSale;
    }

    private XElement CreateReference(int referenceCount) {
        XElement reference = new("Ссылка");
        reference.Add(new XAttribute("Нпп", referenceCount));

        return reference;
    }

    private XElement CreatePropertyWithValueElement(string typeName, object name, object value = null) {
        XElement propertyElement = new("Свойство");
        propertyElement.Add(new XAttribute("Тип", typeName));
        propertyElement.Add(new XAttribute("Имя", name));

        if (value != null) {
            XElement valueProperty = new("Значение");
            valueProperty.Value = value.ToString();
            propertyElement.Add(valueProperty);
        } else {
            propertyElement.Add(new XElement("Пусто"));
        }

        return propertyElement;
    }

    private XElement CreatePropertyElement(string typeName, string name) {
        XElement propertyElement = new("Свойство");
        propertyElement.Add(new XAttribute("Тип", typeName));
        propertyElement.Add(new XAttribute("Имя", name));

        return propertyElement;
    }

    private XElement CreateOrderItemForProductParts(OrderItem orderItem, int productReferenceId, int unitMeasuresReferenceId, int productSpecificationId) {
        XElement product = new("Запись");
        XElement productProperty = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Номенклатура");
        productProperty.Add(new XAttribute("ИмяПКО", "Номенклатура"));
        XElement productReference = CreateReference(productReferenceId);
        productReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Наименование", orderItem.Product.Name));
        productReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ЭтоГруппа", false));
        productReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Артикул", orderItem.Product.VendorCode));
        productProperty.Add(productReference);
        product.Add(productProperty);
        XElement productSpecification = CreatePropertyElement(TypeOfExchangeFile.SPECIFICATION_PRODUCT, "А_КодУКТВЭД");
        XElement productSpecificationReference = CreateReference(productSpecificationId);
        productSpecificationReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.STRING,
            "Код",
            orderItem.AssignedSpecification?.SpecificationCode));
        productSpecificationReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.TYPE_CODE_PRODUCT_SPECIFICATION,
            "Вид",
            "КодТовараИмпортированного"));
        productSpecification.Add(productSpecificationReference);
        product.Add(productSpecification);
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "А_МитнаСтавка", orderItem.AssignedSpecification?.DutyPercent));
        XElement productUnitMeasures = CreatePropertyElement(TypeOfExchangeFile.UNIT_MEASURES, "ЕдиницаИзмерения");
        XElement productUnitMeasuresReferece = CreateReference(unitMeasuresReferenceId);
        XElement productUnitMeasuresRefereceProduct = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Владелец");
        productUnitMeasuresRefereceProduct.Add(new XAttribute("ИмяПКО", "Номенклатура"));
        XElement productUnitMeasuresRefereceProductReference = CreateReference(productReferenceId);
        productUnitMeasuresRefereceProductReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Наименование", orderItem.Product.Name));
        productUnitMeasuresRefereceProductReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ЭтоГруппа", false));
        productUnitMeasuresRefereceProductReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Артикул", orderItem.Product.VendorCode));
        productUnitMeasuresRefereceProduct.Add(productUnitMeasuresRefereceProductReference);
        productUnitMeasuresReferece.Add(productUnitMeasuresRefereceProduct);
        productUnitMeasures.Add(productUnitMeasuresReferece);
        product.Add(productUnitMeasures);
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Коэффициент", 1));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.DATE, "ДатаИзменения", orderItem.Updated.ToString("yyyy-MM-ddTHH:mm:ss")));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Количество", orderItem.Qty));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Цена", orderItem.PricePerItem));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "ПроцентАвтоматическихСкидок"));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "ПроцентСкидкиНаценки", orderItem.DiscountAmount));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Сумма", orderItem.TotalAmount));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "СуммаНДС",
            (orderItem.PricePerItem - orderItem.PricePerItemWithoutVat) * Convert.ToDecimal(orderItem.Qty)));

        return product;
    }

    private XElement CreateSupplyOrderUkraineItemForProductParts(SupplyOrderUkraineItem supplyOrderUkraineItem, int productReferenceId, int unitMeasuresReferenceId,
        int productSpecificationId) {
        XElement product = new("Запись");
        XElement productProperty = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Номенклатура");
        productProperty.Add(new XAttribute("ИмяПКО", "Номенклатура"));
        XElement productReference = CreateReference(productReferenceId);
        productReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Наименование", supplyOrderUkraineItem.Product.Name));
        productReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ЭтоГруппа", false));
        productReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Артикул", supplyOrderUkraineItem.Product.VendorCode));
        productProperty.Add(productReference);
        product.Add(productProperty);
        XElement productSpecification = CreatePropertyElement(TypeOfExchangeFile.SPECIFICATION_PRODUCT, "А_КодУКТВЭД");
        XElement productSpecificationReference = CreateReference(productSpecificationId);
        productSpecificationReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.STRING,
            "Код",
            supplyOrderUkraineItem.Product.ProductSpecifications.FirstOrDefault()?.SpecificationCode ?? ""));
        productSpecificationReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.TYPE_CODE_PRODUCT_SPECIFICATION,
            "Вид",
            "КодТовараИмпортированного"));
        productSpecification.Add(productSpecificationReference);
        product.Add(productSpecification);
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "А_МитнаСтавка",
            supplyOrderUkraineItem.Product.ProductSpecifications?.FirstOrDefault()?.DutyPercent));
        XElement productUnitMeasures = CreatePropertyElement(TypeOfExchangeFile.UNIT_MEASURES, "ЕдиницаИзмерения");
        XElement productUnitMeasuresReferece = CreateReference(unitMeasuresReferenceId);
        XElement productUnitMeasuresRefereceProduct = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Владелец");
        productUnitMeasuresRefereceProduct.Add(new XAttribute("ИмяПКО", "Номенклатура"));
        XElement productUnitMeasuresRefereceProductReference = CreateReference(productReferenceId);
        productUnitMeasuresRefereceProductReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Наименование", supplyOrderUkraineItem.Product.Name));
        productUnitMeasuresRefereceProductReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ЭтоГруппа", false));
        productUnitMeasuresRefereceProductReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Артикул", supplyOrderUkraineItem.Product.VendorCode));
        productUnitMeasuresRefereceProduct.Add(productUnitMeasuresRefereceProductReference);
        productUnitMeasuresReferece.Add(productUnitMeasuresRefereceProduct);
        productUnitMeasures.Add(productUnitMeasuresReferece);
        product.Add(productUnitMeasures);
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Коэффициент", 1));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Количество", supplyOrderUkraineItem.Qty));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Цена", supplyOrderUkraineItem.UnitPrice));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Сумма",
            decimal.Round(Convert.ToDecimal(supplyOrderUkraineItem.Qty) * supplyOrderUkraineItem.UnitPrice, MidpointRounding.AwayFromZero)));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "СуммаНДС",
            decimal.Round((supplyOrderUkraineItem.GrossPrice - supplyOrderUkraineItem.UnitPrice) * Convert.ToDecimal(supplyOrderUkraineItem.Qty),
                MidpointRounding.AwayFromZero)));

        return product;
    }

    private XElement CreateSupplyOrderItemForProductParts(SupplyOrderItem supplyOrderItem, int productReferenceId, int unitMeasuresReferenceId,
        int productSpecificationId) {
        XElement product = new("Запись");
        XElement productProperty = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Номенклатура");
        productProperty.Add(new XAttribute("ИмяПКО", "Номенклатура"));
        XElement productReference = CreateReference(productReferenceId);
        productReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Наименование", supplyOrderItem.Product.Name));
        productReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ЭтоГруппа", false));
        productReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Артикул", supplyOrderItem.Product.VendorCode));
        productProperty.Add(productReference);
        product.Add(productProperty);
        XElement productSpecification = CreatePropertyElement(TypeOfExchangeFile.SPECIFICATION_PRODUCT, "А_КодУКТВЭД");
        XElement productSpecificationReference = CreateReference(productSpecificationId);
        productSpecificationReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.STRING,
            "Код",
            supplyOrderItem.Product.ProductSpecifications.FirstOrDefault()?.SpecificationCode ?? ""));
        productSpecificationReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.TYPE_CODE_PRODUCT_SPECIFICATION,
            "Вид",
            "КодТовараИмпортированного"));
        productSpecification.Add(productSpecificationReference);
        product.Add(productSpecification);
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "А_МитнаСтавка",
            supplyOrderItem.Product.ProductSpecifications?.FirstOrDefault()?.DutyPercent));
        XElement productUnitMeasures = CreatePropertyElement(TypeOfExchangeFile.UNIT_MEASURES, "ЕдиницаИзмерения");
        XElement productUnitMeasuresReferece = CreateReference(unitMeasuresReferenceId);
        XElement productUnitMeasuresRefereceProduct = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Владелец");
        productUnitMeasuresRefereceProduct.Add(new XAttribute("ИмяПКО", "Номенклатура"));
        XElement productUnitMeasuresRefereceProductReference = CreateReference(productReferenceId);
        productUnitMeasuresRefereceProductReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Наименование", supplyOrderItem.Product.Name));
        productUnitMeasuresRefereceProductReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ЭтоГруппа", false));
        productUnitMeasuresRefereceProductReference.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Артикул", supplyOrderItem.Product.VendorCode));
        productUnitMeasuresRefereceProduct.Add(productUnitMeasuresRefereceProductReference);
        productUnitMeasuresReferece.Add(productUnitMeasuresRefereceProduct);
        productUnitMeasures.Add(productUnitMeasuresReferece);
        product.Add(productUnitMeasures);
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Коэффициент", 1));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Количество", supplyOrderItem.Qty));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Цена", supplyOrderItem.UnitPrice));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Сумма",
            decimal.Round(Convert.ToDecimal(supplyOrderItem.Qty) * supplyOrderItem.UnitPrice, MidpointRounding.AwayFromZero)));
        product.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "СуммаНДС"));

        return product;
    }

    private XElement CreateProductElementInRootObject(Product product, int productId, int unitMeasuresId) {
        XElement orderItemElement = CreateAdditionalObject(RuleNames.PRODUCTS, TypeOfExchangeFile.PRODUCT, productId, true);
        XElement orderItemElementReference = CreateReference(productId);
        orderItemElementReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.STRING,
            "Наименование",
            product.Name));
        orderItemElementReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.BOOLEAN,
            "ЭтоГруппа",
            false));
        orderItemElementReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.STRING,
            "Артикул",
            product.VendorCode));
        orderItemElement.Add(orderItemElementReference);
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "БланкСтрогогоУчета"));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "Весовой", false));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "ВесовойКоэффициентВхождения", 1));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ВестиПартионныйУчетПоСериям", false));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ВестиСерийныеНомера", false));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ВестиУчетПоСериям", false));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ВестиУчетПоХарактеристикам", false));
        XElement typeProduct = CreatePropertyElement(TypeOfExchangeFile.TYPE_PRODUCT, "ВидНоменклатуры");
        XElement typeProductValue = new("Выражение");
        typeProductValue.Value = "Справочники.ВидыНоменклатуры.НайтиПоНаименованию" + @"(""Товар"")";
        typeProduct.Add(typeProductValue);
        orderItemElement.Add(typeProduct);
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ГабаритнийТовар", false));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "ДополнительноеОписаниеНоменклатуры", product.Description));
        XElement productUnitMeasuresForReport = CreatePropertyElement(TypeOfExchangeFile.UNIT_MEASURES, "ЕдиницаДляОтчетов");
        XElement productUnitMeasuresForReportReference = CreateReference(unitMeasuresId);
        XElement productUnitMeasuresForReportReferenceProduct = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Владелец");
        productUnitMeasuresForReportReferenceProduct.Add(new XAttribute("ИмяПКО", "Номенклатура"));
        XElement productUnitMeasuresForReportReferenceProductReference = CreateReference(productId);
        productUnitMeasuresForReportReferenceProductReference.Add(
            CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                product.Name));
        productUnitMeasuresForReportReferenceProductReference.Add(
            CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ЭтоГруппа",
                false));
        productUnitMeasuresForReportReferenceProductReference.Add(
            CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Артикул",
                product.VendorCode));
        productUnitMeasuresForReportReferenceProduct.Add(productUnitMeasuresForReportReferenceProductReference);
        productUnitMeasuresForReportReference.Add(productUnitMeasuresForReportReferenceProduct);
        productUnitMeasuresForReport.Add(productUnitMeasuresForReportReference);
        orderItemElement.Add(productUnitMeasuresForReport);
        XElement productUnitMeasuresForPlacing = CreatePropertyElement(TypeOfExchangeFile.UNIT_MEASURES, "ЕдиницаИзмеренияМест");
        orderItemElement.Add(productUnitMeasuresForPlacing);
        XElement productUnitMeasuresForRemaining = CreatePropertyElement(TypeOfExchangeFile.UNIT_MEASURES, "ЕдиницаХраненияОстатков");
        XElement productUnitMeasuresForRemainingReference = CreateReference(unitMeasuresId);
        XElement productUnitMeasuresForRemainingReferenceProduct = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Владелец");
        productUnitMeasuresForRemainingReferenceProduct.Add(new XAttribute("ИмяПКО", "Номенклатура"));
        XElement productUnitMeasuresForRemainingReferenceProductReference = CreateReference(productId);
        productUnitMeasuresForRemainingReferenceProductReference.Add(
            CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                product.Name));
        productUnitMeasuresForRemainingReferenceProductReference.Add(
            CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ЭтоГруппа",
                false));
        productUnitMeasuresForRemainingReferenceProductReference.Add(
            CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Артикул",
                product.VendorCode));
        productUnitMeasuresForRemainingReferenceProduct.Add(productUnitMeasuresForRemainingReferenceProductReference);
        productUnitMeasuresForRemainingReference.Add(productUnitMeasuresForRemainingReferenceProduct);
        productUnitMeasuresForRemaining.Add(productUnitMeasuresForRemainingReference);
        orderItemElement.Add(productUnitMeasuresForRemaining);
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "КодЛьготы"));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Комментарий"));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "Комплект", product.HasComponent));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "ЛьготаНДС"));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "Набор", product.HasComponent));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "НаименованиеПолное", product.Name));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ТранспортнаяУслуга", false));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "Услуга", false));
        orderItemElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "УчитываетсяПоНоминальнойСтоимости", false));

        return orderItemElement;
    }

    private XElement CreateProductGroup(
        ProductGroup productGroup,
        int productGroupId,
        bool isRootProductGroup,
        int rootProductGroupId = default,
        ProductGroup rootProductGroup = null) {
        XElement productGroupElement = CreateAdditionalObject(RuleNames.PRODUCTS, TypeOfExchangeFile.PRODUCT, productGroupId, true);
        XElement productGroupElementReference = CreateReference(productGroupId);
        productGroupElementReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.STRING,
            "Наименование", productGroup.Name ?? productGroup.FullName));
        productGroupElementReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.BOOLEAN,
            "ЭтоГруппа", true));
        productGroupElementReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.STRING,
            "Артикул"));
        productGroupElement.Add(productGroupElementReference);
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "БланкСтрогогоУчета"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "Весовой"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "ВесовойКоэффициентВхождения"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "ВестиПартионныйУчетПоСериям"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "ВестиСерийныеНомера"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "ВестиУчетПоСериям"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "ВестиУчетПоХарактеристикам"));
        XElement typeProduct = CreatePropertyElement(TypeOfExchangeFile.TYPE_PRODUCT, "ВидНоменклатуры");
        XElement typeProductValue = new("Выражение");
        typeProductValue.Value = "Справочники.ВидыНоменклатуры.НайтиПоНаименованию" + @"(""" + "Товар" + @""")";
        typeProduct.Add(typeProductValue);
        productGroupElement.Add(typeProduct);
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ГабаритнийТовар"));
        productGroupElement.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.STRING,
            "ДополнительноеОписаниеНоменклатуры",
            productGroup.Description));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "КодЛьготы"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Комментарий"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "Комплект"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "ЛьготаНДС"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "Набор"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "НаименованиеПолное", productGroup.FullName));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "ТранспортнаяУслуга"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "Услуга"));
        productGroupElement.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.BOOLEAN, "УчитываетсяПоНоминальнойСтоимости"));
        XElement rootProductGroupElement = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Родитель");
        rootProductGroupElement.Add(new XAttribute("ИмяПКО", "Номенклатура"));

        if (isRootProductGroup) {
            productGroupElement.Add(rootProductGroupElement);
        } else {
            XElement rootProductGroupElementReference = CreateReference(rootProductGroupId);
            rootProductGroupElementReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING, "Номенклатура",
                rootProductGroup.Name ??
                rootProductGroup.FullName));
            rootProductGroupElementReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN, "ЭтоГруппа",
                true));
            rootProductGroupElementReference.Add(CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING, "Артикул"));
            rootProductGroupElement.Add(rootProductGroupElementReference);
            productGroupElement.Add(rootProductGroupElement);
        }

        return productGroupElement;
    }

    private XElement CreateExchangeRules() {
        XElement exchangeRule = new("ПравилаОбмена");
        exchangeRule.Add(new XElement("ВерсияФормата", 2.01));
        exchangeRule.Add(new XElement("Ид", "2b353ca9-03dd-4630-bace-1762242b1346"));
        exchangeRule.Add(new XElement("Наименование", "АМГ --> АМГ"));
        exchangeRule.Add(new XElement("ДатаВремяСоздания", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")));
        exchangeRule.Add(new XElement("Источник", "УправлениеТорговымПредприятиемДляУкраины"));
        exchangeRule.Add(new XElement("Приемник", "УправлениеТорговымПредприятиемДляУкраины"));
        exchangeRule.Add(new XElement("Параметры"));
        exchangeRule.Add(new XElement("Обработки"));
        XElement ruleConverterObjects = new("ПравилаКонвертацииОбъектов");

        ruleConverterObjects.Add(AddRule(
            "ПоступлениеТоваровУслуг",
            "Объект.ВидОперации = Перечисления.ВидыОперацийПоступлениеТоваровУслуг.ПокупкаКомиссия; " +
            @"Объект.Организация = Справочники.Организации.НайтиПоКоду(""000000022""); " +
            @"Объект.Подразделение = Справочники.Подразделения.НайтиПоКоду(""000000005""); " +
            @"Объект.ОтражатьВУправленческомУчете = Истина; Объект.ОтражатьВБухгалтерскомУчете = Истина; ТекПользователь = глЗначениеПеременной(""глТекущийПользователь""); " +
            @"Объект.Ответственный = ТекПользователь; Объект.ВидПоступления = Перечисления.ВидыПоступленияТоваров.НаСклад; Объект.СкладОрдер = " +
            @"Справочники.Склады.НайтиПоКоду(""000000019""); Объект.УчитыватьНДС = (Объект.Товары.Итог(""СуммаНДС"")>0); " +
            "Если Объект.УчитыватьНДС Тогда Для Каждого сс Из Объект.Товары Цикл сс.СтавкаНДС = Перечисления.СтавкиНДС.НДС20; КонецЦикла; " +
            "КонецЕсли; СчетаУчета = УправлениеВзаиморасчетами.ПолучитьСчетаРасчетовСКонтрагентом(Объект.Организация, " +
            "Объект.Контрагент, Объект.ДоговорКонтрагента); Объект.СчетУчетаРасчетовСКонтрагентом = СчетаУчета.СчетРасчетовПокупателя; " +
            "Объект.СчетУчетаРасчетовПоАвансам = СчетаУчета.СчетАвансовПокупателя; Объект.СчетУчетаРасчетовПоТаре = СчетаУчета.СчетУчетаТарыПокупателя; " +
            "Объект.СчетУчетаРасчетовПоТареПоАвансам = СчетаУчета.СчетАвансовПоТареПокупателя; Объект.СчетУчетаНДС = СчетаУчета.СчетУчетаНДСПродаж; " +
            @"Объект.СчетУчетаНДСПодтвержденный = СчетаУчета.СчетУчетаНДСПродажПодтвержденный; Объект.ЗаполнитьСчетаУчетаВТабЧасти(Объект.Товары, ""Товары"", Истина, Истина); " +
            @"Объект.ЗаполнитьСчетаУчетаВТабЧасти(Объект.Услуги, ""Услуги"", Истина, Истина); Попытка Объект.Проведен = Истина; Объект.Записать(РежимЗаписиДокумента.Проведение); " +
            "Исключение Объект.Проведен = ложь; Объект.Записать(РежимЗаписиДокумента.Запись); КонецПопытки; ОбъектМодифицирован = ложь;",
            TypeOfExchangeFile.DOCUMENT_INCOME,
            TypeOfExchangeFile.DOCUMENT_INCOME));

        ruleConverterObjects.Add(AddRule(
            "РеализацияТоваровУслуг",
            @"Объект.Организация = Справочники.Организации.НайтиПоКоду(""000000022""); " +
            @"Объект.Подразделение = Справочники.Подразделения.НайтиПоКоду(""000000005""); " +
            "Объект.ОтражатьВУправленческомУчете = Истина; Объект.ОтражатьВБухгалтерскомУчете = Истина; " +
            @"ТекПользователь = глЗначениеПеременной(""глТекущийПользователь""); Объект.Ответственный = " +
            "ТекПользователь; Объект.ВидОперации = Перечисления.ВидыОперацийРеализацияТоваров.ПродажаКомиссия; " +
            @"Объект.ВидПередачи = Перечисления.ВидыПередачиТоваров.СоСклада; лСклад = Справочники.Склады.НайтиПоКоду(""000000019""); " +
            @"Объект.Склад = лСклад; Объект.УчитыватьНДС = (Объект.Товары.Итог(""СуммаНДС"")>0); " +
            "Если Объект.УчитыватьНДС Тогда Для Каждого сс Из Объект.Товары Цикл сс.СтавкаНДС = Перечисления.СтавкиНДС.НДС20; КонецЦикла; КонецЕсли; " +
            "СчетаУчета = УправлениеВзаиморасчетами.ПолучитьСчетаРасчетовСКонтрагентом(Объект.Организация, " +
            "Объект.Контрагент, Объект.ДоговорКонтрагента); Объект.СчетУчетаРасчетовСКонтрагентом = СчетаУчета.СчетРасчетовПокупателя; " +
            "Объект.СчетУчетаРасчетовПоАвансам = СчетаУчета.СчетАвансовПокупателя; Объект.СчетУчетаРасчетовПоТаре = СчетаУчета.СчетУчетаТарыПокупателя; " +
            "Объект.СчетУчетаРасчетовПоТареПоАвансам = СчетаУчета.СчетАвансовПоТареПокупателя; Объект.СчетУчетаНДС = " +
            "СчетаУчета.СчетУчетаНДСПродаж; Объект.СчетУчетаНДСПодтвержденный = СчетаУчета.СчетУчетаНДСПродажПодтвержденный; " +
            @"Объект.ЗаполнитьСчетаУчетаВТабЧасти(Объект.Товары, ""Товары"", Истина, Истина); " +
            @"Объект.ЗаполнитьСчетаУчетаВТабЧасти(Объект.Услуги, ""Услуги"", Истина, Истина); " +
            "Попытка Объект.Проведен = Истина; Объект.Записать(РежимЗаписиДокумента.Проведение); " +
            "Исключение Объект.Проведен = ложь; Объект.Записать(РежимЗаписиДокумента.Запись); КонецПопытки; ОбъектМодифицирован = ложь;",
            TypeOfExchangeFile.DOCUMENT_SALE,
            TypeOfExchangeFile.DOCUMENT_SALE));

        ruleConverterObjects.Add(AddRule(
            "Валюты",
            source: TypeOfExchangeFile.CURRENCIES,
            recieve: TypeOfExchangeFile.CURRENCIES,
            isReplace: true));

        ruleConverterObjects.Add(AddRule(
            "Банки",
            source: TypeOfExchangeFile.BANKS,
            recieve: TypeOfExchangeFile.BANKS,
            isReplace: true));

        ruleConverterObjects.Add(AddRule(
            "БанковскиеСчета",
            source: TypeOfExchangeFile.BANK_ACCOUNT,
            recieve: TypeOfExchangeFile.BANK_ACCOUNT,
            isReplace: true,
            generateNewNumber: true));

        ruleConverterObjects.Add(AddRule(
            "Контрагенты",
            source: TypeOfExchangeFile.COUNTERPARTY,
            recieve: TypeOfExchangeFile.COUNTERPARTY,
            isReplace: true,
            generateNewNumber: true));

        ruleConverterObjects.Add(AddRule(
            "ДоговорыКонтрагентов",
            @"Объект.Организация = Справочники.Организации.НайтиПоКоду(""000000022"");",
            TypeOfExchangeFile.AGREEMENT,
            TypeOfExchangeFile.AGREEMENT,
            generateNewNumber: true));

        ruleConverterObjects.Add(AddRule(
            "Номенклатура",
            "",
            TypeOfExchangeFile.PRODUCT,
            TypeOfExchangeFile.PRODUCT,
            true,
            true,
            @"Если СвойстваПоиска[""ЭтоГруппа""] = Истина Тогда СтрокаИменСвойствПоиска = 
                ""Наименование, ЭтоГруппа""; ИначеСтрокаИменСвойствПоиска = ""Артикул""; КонецЕсли;"));

        ruleConverterObjects.Add(AddRule(
            "КлассификаторЕдиницИзмерения",
            source: TypeOfExchangeFile.CLASSIFIER_UNIT_MEASURES,
            recieve: TypeOfExchangeFile.CLASSIFIER_UNIT_MEASURES,
            isReplace: true));

        ruleConverterObjects.Add(AddRule(
            "ЕдиницыИзмерения",
            source: TypeOfExchangeFile.UNIT_MEASURES,
            recieve: TypeOfExchangeFile.UNIT_MEASURES,
            isReplace: true,
            generateNewNumber: true));

        ruleConverterObjects.Add(AddRule(
            "КлассификаторУКТВЭД",
            source: TypeOfExchangeFile.SPECIFICATION_PRODUCT,
            recieve: TypeOfExchangeFile.SPECIFICATION_PRODUCT,
            isReplace: true,
            generateNewNumber: true));

        ruleConverterObjects.Add(AddRule(
            "Грузополучатели",
            source: TypeOfExchangeFile.RECEIVERS,
            recieve: TypeOfExchangeFile.RECEIVERS));

        ruleConverterObjects.Add(AddRule(
            "Перевозчики",
            source: TypeOfExchangeFile.CARRIERS,
            recieve: TypeOfExchangeFile.CARRIERS,
            generateNewNumber: true));

        ruleConverterObjects.Add(AddRule(
            RuleNames.TYPE_AGREEMENT_CIVIL_CODE,
            source: TypeOfExchangeFile.TYPE_AGREEMENT_CIVIL_CODE,
            recieve: TypeOfExchangeFile.TYPE_AGREEMENT_CIVIL_CODE));

        ruleConverterObjects.Add(AddRule(
            "ФормыРасчетов",
            source: TypeOfExchangeFile.PAYMENT_FORMS,
            recieve: TypeOfExchangeFile.PAYMENT_FORMS));

        ruleConverterObjects.Add(AddRule(
            RuleNames.TAX_ACCOUNTING_SCHEME,
            source: TypeOfExchangeFile.TAX_ACCOUNTING_SCHEME,
            recieve: TypeOfExchangeFile.TAX_ACCOUNTING_SCHEME));

        ruleConverterObjects.Add(AddRule(
            "СтатьиДвиженияДенежныхСредств",
            source: TypeOfExchangeFile.ARTICLE_CASH_FLOW,
            recieve: TypeOfExchangeFile.ARTICLE_CASH_FLOW));

        ruleConverterObjects.Add(AddRule(
            "ВидыОперацийПоступлениеТоваровУслуг",
            source: "ПеречислениеСсылка.ВидыОперацийПоступлениеТоваровУслуг",
            recieve: "ПеречислениеСсылка.ВидыОперацийПоступлениеТоваровУслуг"));

        ruleConverterObjects.Add(AddRule(
            "ВидыПоступленияТоваров",
            source: "ПеречислениеСсылка.ВидыПоступленияТоваров",
            recieve: "ПеречислениеСсылка.ВидыПоступленияТоваров"));

        ruleConverterObjects.Add(AddRule(
            "ЮрФизЛицо",
            source: TypeOfExchangeFile.ENUMERIC_CLIENT_IS_INVIDUALS,
            recieve: TypeOfExchangeFile.ENUMERIC_CLIENT_IS_INVIDUALS));

        ruleConverterObjects.Add(AddRule(
            "ВедениеВзаиморасчетовПоДоговорам",
            source: TypeOfExchangeFile.ENUMERIC_TYPE_MUTUAL_SETTLEMENTS,
            recieve: TypeOfExchangeFile.ENUMERIC_TYPE_MUTUAL_SETTLEMENTS));

        ruleConverterObjects.Add(AddRule(
            "ВидыДоговоровКонтрагентов",
            source: TypeOfExchangeFile.ENUMERIC_TYPE_AGREEMENT,
            recieve: TypeOfExchangeFile.ENUMERIC_TYPE_AGREEMENT));

        ruleConverterObjects.Add(AddRule(
            "ВидыУсловийДоговоровВзаиморасчетов",
            source: TypeOfExchangeFile.ENUMERIC_TYPE_CONDITION_AGREEMENT,
            recieve: TypeOfExchangeFile.ENUMERIC_TYPE_CONDITION_AGREEMENT));

        ruleConverterObjects.Add(AddRule(
            "ВидыКодовДляНалоговойНакладной",
            source: TypeOfExchangeFile.TYPE_CODE_PRODUCT_SPECIFICATION,
            recieve: TypeOfExchangeFile.TYPE_CODE_PRODUCT_SPECIFICATION));

        ruleConverterObjects.Add(AddRule(
            "ФормированиеПрефиксаПоВидуКлиента",
            source: TypeOfExchangeFile.ENUMERIC_TYPE_CLIENT,
            recieve: TypeOfExchangeFile.ENUMERIC_TYPE_CLIENT));

        ruleConverterObjects.Add(AddRule(
            "ДниНедели",
            source: "ПеречислениеСсылка.ДниНедели",
            recieve: "ПеречислениеСсылка.ДниНедели"));

        ruleConverterObjects.Add(AddRule(
            "ВидыДеятельностиДляДДС",
            source: TypeOfExchangeFile.CASH_FLOW_ACTIVITIES_TYPES,
            recieve: TypeOfExchangeFile.CASH_FLOW_ACTIVITIES_TYPES));

        ruleConverterObjects.Add(AddRule(
            "СпособыРасчетаКомиссионногоВознаграждения",
            source: TypeOfExchangeFile.COMMISSION_CALCULATION_METHODS,
            recieve: TypeOfExchangeFile.COMMISSION_CALCULATION_METHODS));

        ruleConverterObjects.Add(AddRule(
            "МоментыОпределенияНалоговойБазы",
            source: TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING,
            recieve: TypeOfExchangeFile.TAX_BASE_MOMENT_DETERMINING));

        exchangeRule.Add(ruleConverterObjects);
        exchangeRule.Add(new XElement("ПравилаОчисткиДанных"));
        exchangeRule.Add(new XElement("Алгоритмы"));
        exchangeRule.Add(new XElement("Запросы"));
        return exchangeRule;
    }

    private XElement AddRule(
        string code = "",
        string afterLoaded = "",
        string source = "",
        string recieve = "",
        bool isReplace = false,
        bool generateNewNumber = false,
        string orderSearchFields = "") {
        XElement rule = new("Правило");

        if (!string.IsNullOrEmpty(code))
            rule.Add(new XElement("Код", code));
        if (!string.IsNullOrEmpty(afterLoaded))
            rule.Add(new XElement("ПослеЗагрузки", afterLoaded));
        if (!string.IsNullOrEmpty(source))
            rule.Add(new XElement("Источник", source));
        if (!string.IsNullOrEmpty(recieve))
            rule.Add(new XElement("Приемник", recieve));
        if (isReplace)
            rule.Add(new XElement("НеЗамещать", true));
        if (generateNewNumber)
            rule.Add(new XElement("ГенерироватьНовыйНомерИлиКодЕслиНеУказан", true));
        if (!string.IsNullOrEmpty(orderSearchFields))
            rule.Add(new XElement("ПоследовательностьПолейПоиска", orderSearchFields));

        return rule;
    }

    private string GetTaxBaseMomentName(TaxBaseMoment taxBaseMoment) {
        switch (taxBaseMoment) {
            case TaxBaseMoment.Payment:
                return "ПоОплате";
            case TaxBaseMoment.Shipment:
                return "ПоОтгрузке";
            case TaxBaseMoment.FirstEvent:
                return "ПоПервомуСобытию";
            default:
                return "НеОпределять";
        }
    }

    private XElement CreateSpecificationProduct(ProductSpecification productSpecification, int productSpecificationId) {
        XElement toReturn = CreateAdditionalObject(
            RuleNames.CLASSIFIER_PRODUCT_SPECIFICATION,
            TypeOfExchangeFile.CLASSIFIER_PRODUCT_SPECIFICATION,
            productSpecificationId,
            true);
        XElement toReturnReference = CreateReference(productSpecificationId);
        toReturnReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.STRING,
            "Код",
            productSpecification?.SpecificationCode ?? ""));
        toReturnReference.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.TYPE_CODE_PRODUCT_SPECIFICATION,
            "Вид",
            "КодТовараИмпортированного"));
        toReturn.Add(toReturnReference);
        toReturn.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.STRING,
            "НаименованиеПолное",
            productSpecification?.Name ?? ""));
        toReturn.Add(CreatePropertyWithValueElement(
            TypeOfExchangeFile.STRING,
            "Наименование",
            productSpecification?.Name ?? ""));

        return toReturn;
    }

    private XElement CreateMeasuresUnitElement(Product product, int productId, int unitMeasuresId) {
        XElement unitMeasures = CreateAdditionalObject(RuleNames.UNIT_MEASURES, TypeOfExchangeFile.UNIT_MEASURES, unitMeasuresId, true);
        XElement unitMeasuresReference = CreateReference(unitMeasuresId);
        XElement unitMeasuresForReportReferenceProduct = CreatePropertyElement(TypeOfExchangeFile.PRODUCT, "Владелец");
        unitMeasuresForReportReferenceProduct.Add(new XAttribute("ИмяПКО", "Номенклатура"));
        XElement unitMeasuresForReportReferenceProductReference = CreateReference(productId);
        unitMeasuresForReportReferenceProductReference.Add(
            CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Наименование",
                product.Name));
        unitMeasuresForReportReferenceProductReference.Add(
            CreatePropertyWithValueElement(
                TypeOfExchangeFile.BOOLEAN,
                "ЭтоГруппа",
                false));
        unitMeasuresForReportReferenceProductReference.Add(
            CreatePropertyWithValueElement(
                TypeOfExchangeFile.STRING,
                "Артикул",
                product.VendorCode));
        unitMeasuresForReportReferenceProduct.Add(unitMeasuresForReportReferenceProductReference);
        unitMeasuresReference.Add(unitMeasuresForReportReferenceProduct);
        unitMeasures.Add(unitMeasuresReference);
        unitMeasures.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Код", product.MeasureUnit.CodeOneC));
        unitMeasures.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Вес"));
        unitMeasures.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Коэффициент", 1));
        unitMeasures.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.NUMBER, "Объем"));
        unitMeasures.Add(CreatePropertyWithValueElement(TypeOfExchangeFile.STRING, "Наименование", product.MeasureUnit.Name));

        return unitMeasures;
    }
}