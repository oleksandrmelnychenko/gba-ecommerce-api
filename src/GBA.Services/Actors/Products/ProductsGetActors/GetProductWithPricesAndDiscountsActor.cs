using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.Products;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.Products.ProductsGetActors;

public sealed class GetProductWithPricesAndDiscountsActor : ReceiveActor {
    private const string RETAIL_PRICE = "�P";
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public GetProductWithPricesAndDiscountsActor(
        IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<GetProductWithPricesAndDiscountsMessage>(ProcessGetProductWithPricesAndDiscountsMessage);
    }

    private void ProcessGetProductWithPricesAndDiscountsMessage(GetProductWithPricesAndDiscountsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(message.ProductNetId, message.ClientAgreementNetId);

        ExchangeRate euroExchangeRate = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();

        CalculatedPricingsWithDiscounts calculatedPricing = null;

        if (product != null && product.ProductPricings.Any()) {
            IPricingRepository pricingRepository = _pricingRepositoriesFactory.NewPricingRepository(connection);

            Pricing agreementPricing = pricingRepository.GetByClientAgreementNetIdWithCalculatedExtraCharge(message.ClientAgreementNetId);
            Pricing retailPricing = pricingRepository.GetRetailPricingWithCalculatedExtraChargeByCulture();
            List<Pricing> pricings = pricingRepository.GetAllWithCalculatedExtraChargeWithDynamicDiscounts(product.NetUid);
            agreementPricing = pricings.First(p => p.Name.Equals(agreementPricing.Name));
            Pricing findRetailPrice = pricings.First(p => p.Name.Equals(RETAIL_PRICE));

            Pricing PricingProductEUR = pricingRepository.GetByNetId(
                agreementPricing.BasePricingId.HasValue
                    ? agreementPricing.BasePricing.NetUid
                    : agreementPricing.NetUid);

            while (PricingProductEUR.BasePricingId.HasValue) PricingProductEUR = pricingRepository.GetByNetId(PricingProductEUR.BasePricing.NetUid);

            decimal productPrice =
                product.ProductPricings.Any(p => p.PricingId.Equals(PricingProductEUR.Id))
                    ? product.ProductPricings.First(p => p.PricingId.Equals(PricingProductEUR.Id)).Price
                    : 0m;

            Pricing basePricing = pricingRepository.GetByNetId(
                agreementPricing.BasePricingId.HasValue
                    ? agreementPricing.BasePricing.NetUid
                    : agreementPricing.NetUid);

            while (basePricing.BasePricingId.HasValue) basePricing = pricingRepository.GetByNetId(basePricing.BasePricing.NetUid);

            decimal priceRetail =
                product.ProductPricings.Any(p => p.PricingId.Equals(basePricing.Id))
                    ? product.ProductPricings.First(p => p.PricingId.Equals(basePricing.Id)).Price
                    : 0m;

            CalculatedPricingsWithDiscounts calculatedPrice = new() {
                Pricing = findRetailPrice,
                RetailPriceEUR = findRetailPrice.ExtraCharge.HasValue
                    ? Math.Round(Convert.ToDecimal(findRetailPrice.ExtraCharge.Value) * priceRetail / 100 + priceRetail, 14)
                    : Math.Round(priceRetail, 14)
            };

            if (product.ProductProductGroups.Any()) {
                ClientAgreement clientAgreement =
                    _clientRepositoriesFactory
                        .NewClientAgreementRepository(connection)
                        .GetByNetIdWithDiscountForSpecificProduct(
                            message.ClientAgreementNetId,
                            product.ProductProductGroups.First().ProductGroupId
                        );

                if (clientAgreement != null && clientAgreement.ProductGroupDiscounts.Any()) {
                    ProductGroupDiscount productGroupDiscount = clientAgreement.ProductGroupDiscounts.First();

                    calculatedPricing = new CalculatedPricingsWithDiscounts {
                        Pricing = agreementPricing,
                        DiscountPriceEUR = Math.Round(product.CurrentPrice, 2),
                        DiscountRate = productGroupDiscount.DiscountRate,
                        PriceEUR = findRetailPrice.ExtraCharge.HasValue
                            ? Math.Round(Convert.ToDecimal(agreementPricing.ExtraCharge.Value) * productPrice / 100 + productPrice, 2)
                            : Math.Round(priceRetail, 2),
                        RetailPriceEUR = findRetailPrice.ExtraCharge.HasValue
                            ? Math.Round(Convert.ToDecimal(findRetailPrice.ExtraCharge.Value) * priceRetail / 100 + priceRetail, 2)
                            : Math.Round(priceRetail, 2)
                    };

                    calculatedPricing.RetailPriceLocal = euroExchangeRate != null
                        ? Math.Round(euroExchangeRate.Amount * calculatedPrice.RetailPriceEUR, 2)
                        : calculatedPrice.RetailPriceEUR;
                } else {
                    calculatedPricing = new CalculatedPricingsWithDiscounts {
                        Pricing = agreementPricing,
                        DiscountPriceEUR = Math.Round(product.CurrentPrice, 2),

                        RetailPriceEUR = findRetailPrice.ExtraCharge.HasValue
                            ? Math.Round(Convert.ToDecimal(findRetailPrice.ExtraCharge.Value) * priceRetail / 100 + priceRetail, 2)
                            : Math.Round(priceRetail, 2)
                    };

                    calculatedPricing.RetailPriceLocal = euroExchangeRate != null
                        ? Math.Round(euroExchangeRate.Amount * calculatedPrice.RetailPriceEUR, 2)
                        : calculatedPrice.RetailPriceEUR;
                }
            } else {
                calculatedPricing = new CalculatedPricingsWithDiscounts {
                    Pricing = agreementPricing,
                    DiscountPriceEUR = Math.Round(product.CurrentPrice, 2),
                    RetailPriceEUR = findRetailPrice.ExtraCharge.HasValue
                        ? Math.Round(Convert.ToDecimal(findRetailPrice.ExtraCharge.Value) * priceRetail / 100 + priceRetail, 2)
                        : Math.Round(priceRetail, 2)
                };

                calculatedPricing.RetailPriceLocal = euroExchangeRate != null
                    ? Math.Round(euroExchangeRate.Amount * calculatedPrice.RetailPriceEUR, 2)
                    : calculatedPrice.RetailPriceEUR;
            }
        }

        Sender.Tell(calculatedPricing);
    }
}