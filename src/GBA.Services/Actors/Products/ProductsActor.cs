using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Akka.Actor;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.ProductModels;
using GBA.Domain.Helpers.Products;
using GBA.Domain.Messages.Auditing;
using GBA.Domain.Messages.Products;
using GBA.Domain.Repositories.Categories.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Measures.Contracts;
using GBA.Domain.Repositories.OriginalNumbers.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Products;

public sealed class ProductsActor : ReceiveActor {
    private static readonly Regex SpecialCharactersReplace = new("[$&+,:;=?@#|/\\\\'\"�<>. ^*()%!\\-]", RegexOptions.Compiled);
    private readonly ICategoryRepositoryFactory _categoryRepositoryFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IMeasureRepositoriesFactory _measureRepositoriesFactory;
    private readonly IOriginalNumberRepositoryFactory _originalNumberRepositoryFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public ProductsActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ICategoryRepositoryFactory categoryRepositoryFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory,
        IMeasureRepositoriesFactory measureRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IOriginalNumberRepositoryFactory originalNumberRepositoryFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _categoryRepositoryFactory = categoryRepositoryFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;
        _measureRepositoriesFactory = measureRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _originalNumberRepositoryFactory = originalNumberRepositoryFactory;

        Receive<AddProductMessage>(ProcessAddProductMessage);

        Receive<UpdateProductMessage>(ProcessUpdateProductMessage);

        Receive<UploadProductsFromFileMessage>(ProcessUploadProductsFromFileMessage);

        Receive<UploadAnaloguesFromFileMessage>(ProcessUploadAnaloguesFromFileMessage);

        Receive<UploadOriginalNumbersFromFileMessage>(ProcessUploadOriginalNumbersFromFileMessage);

        Receive<UploadComponentsFromFileMessage>(ProcessUploadComponentsFromFileMessage);

        Receive<RemoveProductAnaloguesMessage>(ProcessRemoveProductAnaloguesMessage);

        Receive<RemoveProductComponentsMessage>(ProcessRemoveProductComponentsMessage);

        Receive<DeleteProductMessage>(ProcessDeleteProductMessage);

        Receive<AddProductSpecificationCodeToProductMessage>(ProcessAddProductSpecificationCodeToProductMessage);

        Receive<AddNewProductSpecificationForCurrentProductMessage>(ProcessAddNewProductSpecificationForCurrentProductMessage);

        Receive<AddNewProductSpecificationForAllProductsMessage>(ProcessAddNewProductSpecificationForAllProductsMessage);

        Receive<AddNewSpecificationCodesFromFileMessage>(ProcessAddNewSpecificationCodesFromFileMessage);
    }

    private void ProcessAddProductMessage(AddProductMessage message) {
        if (message.Product == null) {
            Sender.Tell(new Tuple<Product, string>(null, "Product entity can not be empty"));
        } else if (!message.Product.IsNew()) {
            Sender.Tell(new Tuple<Product, string>(null, "Existing product is not valid input for current request"));
        } else if (message.Product.MeasureUnit == null || message.Product.MeasureUnit.IsNew()) {
            Sender.Tell(new Tuple<Product, string>(null, ProductResourceNames.MEASURE_UNIT_NOT_SPECIFIED));
        } else {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IProductRepository productRepository = _productRepositoriesFactory.NewProductRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);

            if (message.Product.MeasureUnit != null) message.Product.MeasureUnitId = message.Product.MeasureUnit.Id;

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("uk")) {
                message.Product.NameUA = message.Product.Name;
                message.Product.DescriptionUA = message.Product.Description;
            } else {
                message.Product.NamePL = message.Product.Name;
                message.Product.DescriptionPL = message.Product.Description;
            }

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl"))
                message.Product.NotesPL = message.Product.Notes;
            else
                message.Product.NotesUA = message.Product.Notes;

            message.Product.Id = productRepository.Add(message.Product);

            if (message.Product.ProductCategories.Any()) {
                ICategoryRepository categoryRepository = _categoryRepositoryFactory.NewCategoryRepository(connection);

                _productRepositoriesFactory
                    .NewProductCategoryRepository(connection)
                    .Add(
                        message
                            .Product
                            .ProductCategories
                            .Where(c => c.Category != null)
                            .Select(productCategory => {
                                productCategory.CategoryId = productCategory.Category.IsNew()
                                    ? categoryRepository.Add(productCategory.Category)
                                    : productCategory.Category.Id;

                                productCategory.ProductId = message.Product.Id;

                                return productCategory;
                            })
                    );
            }

            if (message.Product.ProductOriginalNumbers.Any()) {
                IOriginalNumberRepository originalNumberRepository = _originalNumberRepositoryFactory.NewOriginalNumberRepository(connection);

                _productRepositoriesFactory
                    .NewProductOriginalNumberRepository(connection)
                    .Add(
                        message
                            .Product
                            .ProductOriginalNumbers
                            .Where(n => n.OriginalNumber != null)
                            .Select(productOriginalNumber => {
                                productOriginalNumber.OriginalNumberId = productOriginalNumber.OriginalNumber.IsNew()
                                    ? originalNumberRepository.Add(productOriginalNumber.OriginalNumber)
                                    : productOriginalNumber.OriginalNumber.Id;

                                productOriginalNumber.ProductId = message.Product.Id;

                                return productOriginalNumber;
                            })
                    );

                if (message.Product.ProductOriginalNumbers.Where(n => n.OriginalNumber != null).Any(n => n.IsMainOriginalNumber && !n.Deleted))
                    message.Product.MainOriginalNumber =
                        message.Product.ProductOriginalNumbers.Where(n => n.OriginalNumber != null).First(n => n.IsMainOriginalNumber && !n.Deleted).OriginalNumber
                            .Number;
            }

            if (message.Product.ProductProductGroups.Any()) {
                IProductGroupRepository productGroupDiscountRepository = _productRepositoriesFactory.NewProductGroupRepository(connection);

                _productRepositoriesFactory
                    .NewProductProductGroupRepository(connection)
                    .Add(
                        message
                            .Product
                            .ProductProductGroups
                            .Where(c => c.ProductGroup != null)
                            .Select(productProductGroup => {
                                productProductGroup.ProductGroupId = productProductGroup.ProductGroup.IsNew()
                                    ? productGroupDiscountRepository.Add(productProductGroup.ProductGroup)
                                    : productProductGroup.ProductGroup.Id;

                                productProductGroup.ProductId = message.Product.Id;

                                return productProductGroup;
                            })
                    );
            }

            _productRepositoriesFactory
                .NewProductAnalogueRepository(connection)
                .Add(
                    message
                        .Product
                        .AnalogueProducts
                        .Where(p => p.AnalogueProduct != null && !p.AnalogueProduct.IsNew())
                        .Select(productAnalogue => {
                            productAnalogue.BaseProductId = message.Product.Id;
                            productAnalogue.AnalogueProductId = productAnalogue.AnalogueProduct.Id;

                            return productAnalogue;
                        })
                );

            _productRepositoriesFactory
                .NewProductSetRepository(connection)
                .Add(
                    message
                        .Product
                        .ComponentProducts
                        .Where(p => p.ComponentProduct != null && !p.ComponentProduct.IsNew())
                        .Select(productSet => {
                            productSet.BaseProductId = message.Product.Id;
                            productSet.ComponentProductId = productSet.ComponentProduct.Id;

                            return productSet;
                        })
                );

            _productRepositoriesFactory
                .NewProductPricingRepository(connection)
                .Add(
                    message
                        .Product
                        .ProductPricings
                        .Where(p => p.Pricing != null && !p.Pricing.IsNew())
                        .Select(productPricing => {
                            productPricing.PricingId = productPricing.Pricing.Id;
                            productPricing.ProductId = message.Product.Id;

                            return productPricing;
                        })
                );

            _productRepositoriesFactory
                .NewProductImageRepository(connection)
                .Add(
                    message
                        .Product
                        .ProductImages
                        .Select(image => {
                            image.ProductId = message.Product.Id;

                            return image;
                        })
                );

            if (message.Product.ProductImages.Any(i => i.IsMainImage && !i.Deleted)) {
                message.Product.HasImage = true;
                message.Product.Image = message.Product.ProductImages.First(i => i.IsMainImage && !i.Deleted).ImageUrl;
            }

            message.Product = getSingleProductRepository.GetById(message.Product.Id);

            Sender.Tell(new Tuple<Product, string>(message.Product, string.Empty));

            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                new RetrieveAndStoreAuditDataMessage(
                    message.CreatedByNetId,
                    message.Product.NetUid,
                    "Product",
                    message.Product
                )
            );
        }
    }

    private void ProcessUpdateProductMessage(UpdateProductMessage message) {
        if (message.Product == null) {
            Sender.Tell(new Tuple<Product, string>(null, "Product entity can not be empty"));
        } else if (message.Product.IsNew()) {
            Sender.Tell(new Tuple<Product, string>(null, "New product is not valid input for current request"));
        } else if (message.Product.MeasureUnit == null || message.Product.MeasureUnit.IsNew()) {
            Sender.Tell(new Tuple<Product, string>(null, ProductResourceNames.MEASURE_UNIT_NOT_SPECIFIED));
        } else {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IProductRepository productRepository = _productRepositoriesFactory.NewProductRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);

            Product fromDb = getSingleProductRepository.GetByNetIdWithoutIncludes(message.Product.NetUid);

            if (message.DescriptionOnly) {
                if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("uk")) {
                    fromDb.DescriptionUA = message.Product.Description ?? string.Empty;
                    fromDb.SearchDescriptionUA = SpecialCharactersReplace.Replace(fromDb.DescriptionUA, string.Empty);
                } else {
                    fromDb.DescriptionPL = message.Product.Description ?? string.Empty;
                    fromDb.SearchDescriptionPL = SpecialCharactersReplace.Replace(fromDb.DescriptionPL, string.Empty);
                }

                fromDb.SearchVendorCode = SpecialCharactersReplace.Replace(fromDb.VendorCode, string.Empty);

                productRepository.Update(fromDb);

                fromDb = getSingleProductRepository.GetByNetId(message.Product.NetUid);
            } else {
                fromDb.SearchVendorCode = SpecialCharactersReplace.Replace(fromDb.VendorCode, string.Empty);

                if (message.Product.MeasureUnit != null) fromDb.MeasureUnitId = message.Product.MeasureUnit.Id;

                if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("uk")) {
                    fromDb.NameUA = message.Product.Name ?? string.Empty;
                    fromDb.SearchNameUA = SpecialCharactersReplace.Replace(fromDb.NameUA, string.Empty);

                    fromDb.DescriptionUA = message.Product.Description ?? string.Empty;
                    fromDb.SearchDescriptionUA = SpecialCharactersReplace.Replace(fromDb.DescriptionUA, string.Empty);
                } else {
                    fromDb.NamePL = message.Product.Name ?? string.Empty;
                    fromDb.SearchNamePL = SpecialCharactersReplace.Replace(fromDb.NamePL, string.Empty);

                    fromDb.DescriptionPL = message.Product.Description ?? string.Empty;
                    fromDb.SearchDescriptionPL = SpecialCharactersReplace.Replace(fromDb.DescriptionPL, string.Empty);
                }

                if (message.Product.ProductCategories.Any()) {
                    IProductCategoryRepository productCategoryRepository = _productRepositoriesFactory.NewProductCategoryRepository(connection);
                    ICategoryRepository categoryRepository = _categoryRepositoryFactory.NewCategoryRepository(connection);

                    productCategoryRepository
                        .RemoveAllByIds(
                            message
                                .Product
                                .ProductCategories
                                .Where(c => !c.IsNew() && c.Deleted)
                                .Select(c => c.Id)
                        );

                    productCategoryRepository
                        .Add(
                            message
                                .Product
                                .ProductCategories
                                .Where(c => c.IsNew() && !c.Deleted && c.Category != null)
                                .Select(productCategory => {
                                    productCategory.CategoryId = productCategory.Category.IsNew()
                                        ? categoryRepository.Add(productCategory.Category)
                                        : productCategory.Category.Id;

                                    productCategory.ProductId = message.Product.Id;

                                    return productCategory;
                                })
                        );
                }

                if (message.Product.ProductOriginalNumbers.Any()) {
                    IProductOriginalNumberRepository productOriginalNumberRepository = _productRepositoriesFactory.NewProductOriginalNumberRepository(connection);
                    IOriginalNumberRepository originalNumberRepository = _originalNumberRepositoryFactory.NewOriginalNumberRepository(connection);

                    productOriginalNumberRepository
                        .RemoveAllByIds(
                            message
                                .Product
                                .ProductOriginalNumbers
                                .Where(n => !n.IsNew() && n.Deleted)
                                .Select(n => n.Id)
                        );

                    productOriginalNumberRepository.SetNotMainByProductId(message.Product.Id);

                    productOriginalNumberRepository
                        .Add(
                            message
                                .Product
                                .ProductOriginalNumbers
                                .Where(n => n.IsNew() && !n.Deleted && n.OriginalNumber != null)
                                .Select(productOriginalNumber => {
                                    if (productOriginalNumber.OriginalNumber.IsNew()) {
                                        productOriginalNumber.OriginalNumber.MainNumber = productOriginalNumber.OriginalNumber.Number;

                                        if (!string.IsNullOrEmpty(productOriginalNumber.OriginalNumber.MainNumber))
                                            productOriginalNumber.OriginalNumber.Number =
                                                SpecialCharactersReplace.Replace(productOriginalNumber.OriginalNumber.MainNumber, string.Empty);

                                        productOriginalNumber.OriginalNumberId = originalNumberRepository.Add(productOriginalNumber.OriginalNumber);
                                    } else {
                                        productOriginalNumber.OriginalNumberId = productOriginalNumber.OriginalNumber.Id;
                                    }

                                    productOriginalNumber.ProductId = message.Product.Id;

                                    return productOriginalNumber;
                                })
                        );

                    productOriginalNumberRepository
                        .Update(
                            message
                                .Product
                                .ProductOriginalNumbers
                                .Where(n => !n.IsNew() && !n.Deleted)
                        );
                }

                if (message.Product.ProductProductGroups.Any()) {
                    IProductProductGroupRepository productProductGroupRepository = _productRepositoriesFactory.NewProductProductGroupRepository(connection);
                    IProductGroupRepository productGroupDiscountRepository = _productRepositoriesFactory.NewProductGroupRepository(connection);

                    productProductGroupRepository
                        .RemoveAllByIds(
                            message
                                .Product
                                .ProductProductGroups
                                .Where(c => !c.IsNew() && c.Deleted)
                                .Select(c => c.Id)
                        );

                    productProductGroupRepository
                        .Add(
                            message
                                .Product
                                .ProductProductGroups
                                .Where(c => c.IsNew() && !c.Deleted && c.ProductGroup != null)
                                .Select(productProductGroup => {
                                    productProductGroup.ProductGroupId = productProductGroup.ProductGroup.IsNew()
                                        ? productGroupDiscountRepository.Add(productProductGroup.ProductGroup)
                                        : productProductGroup.ProductGroup.Id;

                                    productProductGroup.ProductId = message.Product.Id;

                                    return productProductGroup;
                                })
                        );
                }

                if (message.Product.AnalogueProducts.Any()) {
                    IProductAnalogueRepository productAnalogueRepository = _productRepositoriesFactory.NewProductAnalogueRepository(connection);

                    productAnalogueRepository
                        .RemoveAllByIds(
                            message
                                .Product
                                .AnalogueProducts
                                .Where(p => !p.IsNew() && p.Deleted)
                                .Select(p => p.Id)
                        );

                    productAnalogueRepository
                        .Add(
                            message
                                .Product
                                .AnalogueProducts
                                .Where(p => p.IsNew() && !p.Deleted && p.AnalogueProduct != null && !p.AnalogueProduct.IsNew())
                                .Select(productAnalogue => {
                                    productAnalogue.BaseProductId = message.Product.Id;
                                    productAnalogue.AnalogueProductId = productAnalogue.AnalogueProduct.Id;

                                    return productAnalogue;
                                })
                        );
                }

                if (message.Product.ComponentProducts.Any()) {
                    IProductSetRepository productSetRepository = _productRepositoriesFactory.NewProductSetRepository(connection);

                    productSetRepository
                        .RemoveAllByIds(
                            message
                                .Product
                                .ComponentProducts
                                .Where(p => !p.IsNew() && p.Deleted)
                                .Select(p => p.Id)
                        );

                    productSetRepository
                        .Add(
                            message
                                .Product
                                .ComponentProducts
                                .Where(p => p.IsNew() && !p.Deleted && p.ComponentProduct != null && !p.ComponentProduct.IsNew())
                                .Select(productSet => {
                                    productSet.BaseProductId = message.Product.Id;
                                    productSet.ComponentProductId = productSet.ComponentProduct.Id;

                                    return productSet;
                                })
                        );
                }

                if (message.Product.ProductPricings.Any()) {
                    IProductPricingRepository productPricingRepository = _productRepositoriesFactory.NewProductPricingRepository(connection);

                    productPricingRepository
                        .RemoveAllByIds(
                            message
                                .Product
                                .ProductPricings
                                .Where(p => !p.IsNew() && p.Deleted)
                                .Select(p => p.Id)
                        );

                    productPricingRepository
                        .Add(
                            message
                                .Product
                                .ProductPricings
                                .Where(p => p.IsNew() && !p.Deleted && p.Pricing != null && !p.Pricing.IsNew())
                                .Select(productPricing => {
                                    productPricing.PricingId = productPricing.Pricing.Id;
                                    productPricing.ProductId = message.Product.Id;

                                    return productPricing;
                                })
                        );
                }

                if (message.Product.ProductImages.Any()) {
                    IProductImageRepository productImageRepository = _productRepositoriesFactory.NewProductImageRepository(connection);

                    productImageRepository
                        .RemoveAllByIds(
                            message
                                .Product
                                .ProductImages
                                .Where(i => !i.IsNew() && i.Deleted)
                                .Select(i => i.Id)
                        );

                    productImageRepository
                        .Add(
                            message
                                .Product
                                .ProductImages
                                .Where(i => i.IsNew() && !i.Deleted)
                                .Select(image => {
                                    image.ProductId = message.Product.Id;

                                    return image;
                                })
                        );

                    productImageRepository
                        .Update(
                            message
                                .Product
                                .ProductImages
                                .Where(i => !i.IsNew() && !i.Deleted)
                        );

                    if (message.Product.ProductImages.Any(i => i.IsMainImage && !i.Deleted)) {
                        fromDb.HasImage = true;
                        fromDb.Image = message.Product.ProductImages.First(i => i.IsMainImage && !i.Deleted).ImageUrl;
                    }
                }

                fromDb.SearchNamePL =
                    fromDb.NamePL
                        .Replace("a", "a")
                        .Replace("c", "c")
                        .Replace("e", "e")
                        .Replace("l", "l")
                        .Replace("n", "n")
                        .Replace("o", "o")
                        .Replace("s", "s")
                        .Replace("z", "z")
                        .Replace("z", "z");

                fromDb.SearchDescriptionPL =
                    fromDb.DescriptionPL
                        .Replace("a", "a")
                        .Replace("c", "c")
                        .Replace("e", "e")
                        .Replace("l", "l")
                        .Replace("n", "n")
                        .Replace("o", "o")
                        .Replace("s", "s")
                        .Replace("z", "z")
                        .Replace("z", "z");

                if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl"))
                    fromDb.NotesPL = message.Product.Notes;
                else
                    fromDb.NotesUA = message.Product.Notes;

                fromDb.Top = message.Product.Top;
                fromDb.IsForSale = message.Product.IsForSale;
                fromDb.IsForZeroSale = message.Product.IsForZeroSale;
                fromDb.Volume = message.Product.Volume;
                fromDb.PackingStandard = message.Product.PackingStandard;
                fromDb.Weight = message.Product.Weight;
                fromDb.Size = message.Product.Size;
                fromDb.SynonymsUA = message.Product.SynonymsUA;
                fromDb.SynonymsPL = message.Product.SynonymsPL;

                fromDb.SearchSize = !string.IsNullOrEmpty(fromDb.Size)
                    ? SpecialCharactersReplace.Replace(fromDb.Size, string.Empty)
                    : string.Empty;

                fromDb.SearchSynonymsUA = !string.IsNullOrEmpty(fromDb.SynonymsUA)
                    ? SpecialCharactersReplace.Replace(fromDb.SynonymsUA, string.Empty)
                    : string.Empty;

                fromDb.SearchSynonymsPL = !string.IsNullOrEmpty(fromDb.SynonymsPL)
                    ? SpecialCharactersReplace.Replace(fromDb.SynonymsPL, string.Empty)
                    : string.Empty;

                productRepository.Update(fromDb);

                fromDb = getSingleProductRepository.GetByNetId(message.Product.NetUid);
            }

            Product toReturnProduct = getSingleProductRepository.GetByNetId(message.Product.NetUid);

            IPricingRepository pricingRepository = _pricingRepositoriesFactory.NewPricingRepository(connection);

            ExchangeRate exchangeRate;

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl"))
                exchangeRate =
                    _exchangeRateRepositoriesFactory
                        .NewExchangeRateRepository(connection)
                        .GetByCurrencyIdAndCode(
                            _currencyRepositoriesFactory
                                .NewCurrencyRepository(connection)
                                .GetPLNCurrencyIfExists()
                                .Id,
                            "EUR",
                            DateTime.UtcNow.AddDays(-1)
                        );
            else
                exchangeRate = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();

            List<Pricing> pricings = pricingRepository.GetAllWithCalculatedExtraChargeWithDynamicDiscounts(toReturnProduct.NetUid);

            foreach (Pricing pricing in pricings)
                if (!pricing.BasePricingId.HasValue) {
                    decimal price =
                        toReturnProduct.ProductPricings.Any(p => p.PricingId.Equals(pricing.Id))
                            ? toReturnProduct.ProductPricings.First(p => p.PricingId.Equals(pricing.Id)).Price
                            : 0m;

                    CalculatedPricingsWithDiscounts calculatedPrice = new() {
                        Pricing = pricing,
                        RetailPriceEUR = pricing.ExtraCharge.HasValue
                            ? Math.Round(Convert.ToDecimal(pricing.ExtraCharge.Value) * price / 100 + price, 14)
                            : Math.Round(price, 14)
                    };

                    calculatedPrice.RetailPriceLocal = exchangeRate != null
                        ? Math.Round(exchangeRate.Amount * calculatedPrice.RetailPriceEUR, 14)
                        : calculatedPrice.RetailPriceEUR;

                    toReturnProduct.CalculatedPrices.Add(calculatedPrice);
                } else {
                    Pricing basePricing = pricingRepository.GetByNetId(pricing.BasePricing.NetUid);

                    while (basePricing.BasePricingId.HasValue) basePricing = pricingRepository.GetByNetId(basePricing.BasePricing.NetUid);

                    decimal price =
                        toReturnProduct.ProductPricings.Any(p => p.PricingId.Equals(basePricing.Id))
                            ? toReturnProduct.ProductPricings.First(p => p.PricingId.Equals(basePricing.Id)).Price
                            : 0m;

                    CalculatedPricingsWithDiscounts calculatedPrice = new() {
                        Pricing = pricing,
                        RetailPriceEUR = pricing.ExtraCharge.HasValue
                            ? Math.Round(Convert.ToDecimal(pricing.ExtraCharge.Value) * price / 100 + price, 14)
                            : Math.Round(price, 14)
                    };

                    calculatedPrice.RetailPriceLocal = exchangeRate != null
                        ? Math.Round(exchangeRate.Amount * calculatedPrice.RetailPriceEUR, 14)
                        : calculatedPrice.RetailPriceEUR;

                    toReturnProduct.CalculatedPrices.Add(calculatedPrice);
                }

            Sender.Tell(new Tuple<Product, string>(toReturnProduct, string.Empty));

            message.Product.CurrentPrice = fromDb.CurrentPrice;
            message.Product.CurrentLocalPrice = fromDb.CurrentLocalPrice;

            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                new RetrieveAndStoreAuditDataMessage(
                    message.UpdatedByNetId,
                    message.Product.NetUid,
                    "Product",
                    message.Product,
                    fromDb
                )
            );
        }
    }

    private void ProcessUploadProductsFromFileMessage(UploadProductsFromFileMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            List<ProductForUpload> productForUploads =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetProductsForUploadByConfiguration(message.PathToFile, message.Configuration);

            IProductRepository productRepository = _productRepositoriesFactory.NewProductRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IPricingRepository pricingRepository = _pricingRepositoriesFactory.NewPricingRepository(connection);
            IMeasureUnitRepository measureUnitRepository = _measureRepositoriesFactory.NewMeasureUnitRepository(connection);
            IProductGroupRepository productGroupRepository = _productRepositoriesFactory.NewProductGroupRepository(connection);
            IProductProductGroupRepository productProductGroupRepository = _productRepositoriesFactory.NewProductProductGroupRepository(connection);

            foreach (ProductForUpload productForUpload in productForUploads) {
                Product product = getSingleProductRepository.GetProductByVendorCode(productForUpload.VendorCode);

                switch (message.Configuration.Mode) {
                    case ProductUploadMode.Add:
                        if (product != null) //throw new Exception($"Product with VendorCode '{productForUpload.VendorCode}' already exists");
                            productForUpload.Skipped = true;
                        break;
                    case ProductUploadMode.Update:
                        if (product == null) //throw new Exception($"Product with VendorCode '{productForUpload.VendorCode}' does not exists");
                            productForUpload.Skipped = true;
                        else
                            productForUpload.Id = product.Id;
                        if (message.Configuration.WithNewVendorCode) {
                            Product updateProduct = getSingleProductRepository.GetProductByVendorCode(productForUpload.NewVendorCode);

                            if (updateProduct != null) //throw new Exception($"Product with VendorCode '{productForUpload.VendorCode}' already exists");
                                productForUpload.Skipped = true;
                        }

                        break;
                    case ProductUploadMode.Remove:
                        if (product == null) //throw new Exception($"Product with VendorCode '{productForUpload.VendorCode}' does not exists");
                            productForUpload.Skipped = true;
                        else
                            productForUpload.Id = product.Id;
                        break;
                }

                if (!string.IsNullOrEmpty(productForUpload.MeasureUnit)) {
                    MeasureUnit measureUnit = measureUnitRepository.GetByName(productForUpload.MeasureUnit);

                    if (measureUnit == null) throw new LocalizedException(ProductResourceNames.MEASURE_UNIT_DOES_NOT_EXISTS, productForUpload.MeasureUnit);
                    //throw new Exception($"MeasureUnit with name '{productForUpload.MeasureUnit}' does not exists");
                    productForUpload.MeasureUnitId = measureUnit.Id;
                }

                if (string.IsNullOrEmpty(productForUpload.ProductGroup)) continue;

                ProductGroup productGroup = productGroupRepository.GetByName(productForUpload.ProductGroup);

                if (productGroup == null) throw new LocalizedException(ProductResourceNames.PRODUCT_GROUP_DOES_NOT_EXISTS, productForUpload.ProductGroup);
                //throw new Exception($"ProductGroup with name '{productForUpload.ProductGroup}' does not exists");
                productForUpload.ProductGroupId = productGroup.Id;
            }

            if (message.Configuration.WithPrices)
                foreach (ProductForUploadPricing forUploadPricing in productForUploads.First().Pricings) {
                    Pricing pricing = pricingRepository.GetById(forUploadPricing.PricingId);

                    if (pricing == null) throw new LocalizedException(ProductResourceNames.PRICING_DOES_NOT_EXISTS, forUploadPricing.Name);
                    //throw new Exception($"No such pricing with Name '{forUploadPricing.Name}'");
                    if (pricing.BasePricingId.HasValue) throw new LocalizedException(ProductResourceNames.CAN_NOT_SET_VALUE_TO_PRICING, forUploadPricing.Name);
                    //throw new Exception($"You can not set price for pricing with name '{forUploadPricing.Name}'");
                }

            IProductPricingRepository productPricingRepository = _productRepositoriesFactory.NewProductPricingRepository(connection);
            IOriginalNumberRepository originalNumberRepository = _originalNumberRepositoryFactory.NewOriginalNumberRepository(connection);
            IProductOriginalNumberRepository productOriginalNumberRepository = _productRepositoriesFactory.NewProductOriginalNumberRepository(connection);

            foreach (ProductForUpload productForUpload in productForUploads.Where(p => !p.Skipped)) {
                Product product = getSingleProductRepository.GetProductByVendorCode(productForUpload.VendorCode);

                switch (message.Configuration.Mode) {
                    case ProductUploadMode.Add:
                        product = new Product {
                            Name = productForUpload.Name ?? string.Empty,
                            NamePL = productForUpload.NamePL ?? string.Empty,
                            NameUA = productForUpload.NameUA ?? string.Empty,
                            Description = productForUpload.Description ?? string.Empty,
                            DescriptionPL = productForUpload.DescriptionPL ?? string.Empty,
                            DescriptionUA = productForUpload.DescriptionUA ?? string.Empty,
                            IsForSale = productForUpload.IsForSale,
                            IsForWeb = productForUpload.IsForWeb,
                            MainOriginalNumber = productForUpload.MainOriginalNumber ?? string.Empty,
                            Size = productForUpload.Size ?? string.Empty,
                            OrderStandard = productForUpload.OrderStandard ?? string.Empty,
                            PackingStandard = productForUpload.PackingStandard ?? string.Empty,
                            Top = productForUpload.Top ?? string.Empty,
                            UCGFEA = productForUpload.UCGFEA ?? string.Empty,
                            Volume = productForUpload.Volume ?? string.Empty,
                            Weight = productForUpload.Weight,
                            VendorCode = productForUpload.VendorCode ?? string.Empty
                        };

                        product.SearchName = SpecialCharactersReplace.Replace(product.Name, string.Empty);
                        product.SearchNameUA = SpecialCharactersReplace.Replace(product.NameUA, string.Empty);
                        product.SearchDescription = SpecialCharactersReplace.Replace(product.Description, string.Empty);
                        product.SearchDescriptionUA = SpecialCharactersReplace.Replace(product.DescriptionUA, string.Empty);
                        product.SearchSize = SpecialCharactersReplace.Replace(product.Size, string.Empty);
                        product.SearchVendorCode = SpecialCharactersReplace.Replace(product.VendorCode, string.Empty);

                        product.SearchNamePL =
                            SpecialCharactersReplace.Replace(product.NamePL, string.Empty)
                                .Replace("a", "a")
                                .Replace("c", "c")
                                .Replace("e", "e")
                                .Replace("l", "l")
                                .Replace("n", "n")
                                .Replace("o", "o")
                                .Replace("s", "s")
                                .Replace("z", "z")
                                .Replace("z", "z");

                        product.SearchDescriptionPL =
                            SpecialCharactersReplace.Replace(product.DescriptionPL, string.Empty)
                                .Replace("a", "a")
                                .Replace("c", "c")
                                .Replace("e", "e")
                                .Replace("l", "l")
                                .Replace("n", "n")
                                .Replace("o", "o")
                                .Replace("s", "s")
                                .Replace("z", "z")
                                .Replace("z", "z");

                        product.MeasureUnitId = !message.Configuration.WithMeasureUnit ? measureUnitRepository.GetByName("��").Id : productForUpload.MeasureUnitId;

                        product.Id = productRepository.Add(product);

                        if (message.Configuration.WithMainOriginalNumber && !string.IsNullOrEmpty(productForUpload.MainOriginalNumber)) {
                            OriginalNumber originalNumber = originalNumberRepository.GetByNumber(productForUpload.MainOriginalNumber);

                            if (originalNumber != null)
                                productOriginalNumberRepository.Add(new ProductOriginalNumber {
                                    IsMainOriginalNumber = true,
                                    ProductId = product.Id,
                                    OriginalNumberId = originalNumber.Id
                                });
                            else
                                productOriginalNumberRepository.Add(new ProductOriginalNumber {
                                    IsMainOriginalNumber = true,
                                    ProductId = product.Id,
                                    OriginalNumberId = originalNumberRepository.Add(new OriginalNumber {
                                        MainNumber = productForUpload.MainOriginalNumber,
                                        Number = SpecialCharactersReplace.Replace(productForUpload.MainOriginalNumber, string.Empty)
                                    })
                                });
                        }

                        if (message.Configuration.WithPrices)
                            foreach (ProductForUploadPricing pricing in productForUpload.Pricings)
                                productPricingRepository.Add(new ProductPricing {
                                    ProductId = product.Id,
                                    PricingId = pricing.PricingId,
                                    Price = pricing.Price
                                });

                        if (message.Configuration.WithProductGroup)
                            productProductGroupRepository.Add(new ProductProductGroup {
                                ProductId = product.Id,
                                ProductGroupId = productForUpload.ProductGroupId
                            });

                        break;
                    case ProductUploadMode.Update:
                        if (message.Configuration.WithNewVendorCode) product.VendorCode = productForUpload.NewVendorCode ?? string.Empty;
                        if (message.Configuration.WithNameRU) product.Name = productForUpload.Name ?? string.Empty;
                        if (message.Configuration.WithNameUA) product.NameUA = productForUpload.NameUA ?? string.Empty;
                        if (message.Configuration.WithNamePL) product.NamePL = productForUpload.NamePL ?? string.Empty;
                        if (message.Configuration.WithDescriptionRU) product.Description = productForUpload.Description ?? string.Empty;
                        if (message.Configuration.WithDescriptionUA) product.DescriptionUA = productForUpload.DescriptionUA ?? string.Empty;
                        if (message.Configuration.WithDescriptionPL) product.DescriptionPL = productForUpload.DescriptionPL ?? string.Empty;
                        if (message.Configuration.WithMeasureUnit) product.MeasureUnitId = productForUpload.MeasureUnitId;
                        if (message.Configuration.WithWeight) product.Weight = productForUpload.Weight;
                        if (message.Configuration.WithTop) product.Top = productForUpload.Top ?? string.Empty;
                        if (message.Configuration.WithOrderStandard) product.OrderStandard = productForUpload.OrderStandard ?? string.Empty;
                        if (message.Configuration.WithPackingStandard) product.PackingStandard = productForUpload.PackingStandard ?? string.Empty;
                        if (message.Configuration.WithSize) product.Size = productForUpload.Size ?? string.Empty;
                        if (message.Configuration.WithVolume) product.Volume = productForUpload.Volume ?? string.Empty;
                        if (message.Configuration.WithUCGFEA) product.UCGFEA = productForUpload.UCGFEA ?? string.Empty;
                        if (message.Configuration.WithIsForWeb) product.IsForWeb = productForUpload.IsForWeb;
                        if (message.Configuration.WithIsForSale) product.IsForSale = productForUpload.IsForSale;

                        product.SearchName = SpecialCharactersReplace.Replace(product.Name, string.Empty);
                        product.SearchNameUA = SpecialCharactersReplace.Replace(product.NameUA, string.Empty);
                        product.SearchDescription = SpecialCharactersReplace.Replace(product.Description, string.Empty);
                        product.SearchDescriptionUA = SpecialCharactersReplace.Replace(product.DescriptionUA, string.Empty);
                        product.SearchSize = SpecialCharactersReplace.Replace(product.Size, string.Empty);
                        product.SearchVendorCode = SpecialCharactersReplace.Replace(product.VendorCode, string.Empty);

                        product.SearchNamePL =
                            SpecialCharactersReplace.Replace(product.NamePL, string.Empty)
                                .Replace("a", "a")
                                .Replace("c", "c")
                                .Replace("e", "e")
                                .Replace("l", "l")
                                .Replace("n", "n")
                                .Replace("o", "o")
                                .Replace("s", "s")
                                .Replace("z", "z")
                                .Replace("z", "z");

                        product.SearchDescriptionPL =
                            SpecialCharactersReplace.Replace(product.DescriptionPL, string.Empty)
                                .Replace("a", "a")
                                .Replace("c", "c")
                                .Replace("e", "e")
                                .Replace("l", "l")
                                .Replace("n", "n")
                                .Replace("o", "o")
                                .Replace("s", "s")
                                .Replace("z", "z")
                                .Replace("z", "z");

                        if (message.Configuration.WithMainOriginalNumber && !string.IsNullOrEmpty(productForUpload.MainOriginalNumber)) {
                            OriginalNumber originalNumber = originalNumberRepository.GetByNumber(productForUpload.MainOriginalNumber);

                            if (originalNumber != null)
                                productOriginalNumberRepository.Add(new ProductOriginalNumber {
                                    IsMainOriginalNumber = true,
                                    ProductId = product.Id,
                                    OriginalNumberId = originalNumber.Id
                                });
                            else
                                productOriginalNumberRepository.Add(new ProductOriginalNumber {
                                    IsMainOriginalNumber = true,
                                    ProductId = product.Id,
                                    OriginalNumberId = originalNumberRepository.Add(new OriginalNumber {
                                        MainNumber = productForUpload.MainOriginalNumber,
                                        Number = SpecialCharactersReplace.Replace(productForUpload.MainOriginalNumber, string.Empty)
                                    })
                                });
                        }

                        if (message.Configuration.WithProductGroup) {
                            productProductGroupRepository.RemoveAllByProductId(product.Id);

                            productProductGroupRepository.Add(new ProductProductGroup {
                                ProductId = product.Id,
                                ProductGroupId = productForUpload.ProductGroupId
                            });
                        }

                        productRepository.Update(product);

                        if (message.Configuration.WithPrices)
                            foreach (ProductForUploadPricing pricing in productForUpload.Pricings) {
                                ProductPricing productPricing = productPricingRepository.GetByIdsIfExists(product.Id, pricing.PricingId);

                                if (productPricing != null) {
                                    productPricing.Price = pricing.Price;

                                    productPricingRepository.Update(productPricing);
                                } else {
                                    productPricingRepository.Add(new ProductPricing {
                                        ProductId = product.Id,
                                        PricingId = pricing.PricingId,
                                        Price = pricing.Price
                                    });
                                }
                            }

                        break;
                    case ProductUploadMode.Remove:
                        productRepository.Remove(product.Id);

                        break;
                }
            }

            Sender.Tell(null);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUploadAnaloguesFromFileMessage(UploadAnaloguesFromFileMessage message) {
        List<long> idsToDeleteInCaseOfFailure = new();
        HashSet<long> baseProductIdsToUpdateInCaseOfFailure = new();

        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            List<AnalogueForUpload> productsForUpload =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetAnaloguesForUploadByConfiguration(message.FilePath, message.AnaloguesUploadParseConfiguration);

            IProductRepository productRepository = _productRepositoriesFactory.NewProductRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductAnalogueRepository productAnalogueRepository = _productRepositoriesFactory.NewProductAnalogueRepository(connection);

            HashSet<AnalogueForUpload> pairsToInsert = AnaloguesFinder.FindAllAnaloguePairs(productsForUpload.ToHashSet(), productAnalogueRepository);

            foreach (AnalogueForUpload analogueForUpload in pairsToInsert) {
                Product baseProduct = getSingleProductRepository.GetProductByVendorCode(analogueForUpload.VendorCode);

                if (baseProduct == null)
                    throw new ProductUploadParseException(
                        ProductUploadParseExceptionType.NoProductByVendorCode,
                        0,
                        0,
                        analogueForUpload.VendorCode
                    );

                long analogueId = getSingleProductRepository.GetProductIdByVendorCode(analogueForUpload.AnalogueVendorCode);

                if (analogueId == 0L)
                    throw new ProductUploadParseException(
                        ProductUploadParseExceptionType.NoProductByVendorCode,
                        analogueForUpload.AnalogueColumn,
                        analogueForUpload.Row,
                        analogueForUpload.AnalogueVendorCode
                    );

                if (productAnalogueRepository.CheckIsProductAnalogueExistsByBaseProductAndAnalogueIds(baseProduct.Id, analogueId)) continue;

                long addedProductAnalogueId = productAnalogueRepository.Add(new ProductAnalogue {
                    BaseProductId = baseProduct.Id,
                    AnalogueProductId = analogueId
                });

                if (!baseProduct.HasAnalogue) {
                    productRepository.UpdateProductHasAnalogue(baseProduct.Id);
                    baseProductIdsToUpdateInCaseOfFailure.Add(baseProduct.Id);
                }

                idsToDeleteInCaseOfFailure.Add(addedProductAnalogueId);
            }

            Sender.Tell(null);
        } catch (Exception exc) {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            _productRepositoriesFactory.NewProductAnalogueRepository(connection).DeleteAllByIds(idsToDeleteInCaseOfFailure);
            _productRepositoriesFactory.NewProductRepository(connection).UncheckProductHasAnalogue(baseProductIdsToUpdateInCaseOfFailure);

            Sender.Tell(exc);
        }
    }

    private void ProcessUploadComponentsFromFileMessage(UploadComponentsFromFileMessage message) {
        List<long> idsToDeleteInCaseOfFailure = new();
        List<long> baseProductIdsToUpdateInCaseOfFailure = new();

        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            List<ComponentForUpload> productsForUpload =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetComponentsForUploadByConfiguration(message.FilePath, message.Configuration);

            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductSetRepository productSetRepository = _productRepositoriesFactory.NewProductSetRepository(connection);
            IProductRepository productRepository = _productRepositoriesFactory.NewProductRepository(connection);

            foreach (IGrouping<string, ComponentForUpload> componentsGroup in productsForUpload.GroupBy(g => g.VendorCode)) {
                Product baseProduct = getSingleProductRepository.GetProductByVendorCode(componentsGroup.Key);

                if (baseProduct == null)
                    throw new ProductUploadParseException(ProductUploadParseExceptionType.NoProductByVendorCode, 0, 0, componentsGroup.Key);

                foreach (ComponentForUpload componentForUpload in componentsGroup.Select(group => group)) {
                    long componentId = getSingleProductRepository.GetProductIdByVendorCode(componentForUpload.ComponentVendorCode);

                    if (componentId == 0L)
                        throw new ProductUploadParseException(ProductUploadParseExceptionType.NoProductByVendorCode, 0, 0, componentForUpload.ComponentVendorCode);

                    ProductSet productSet = productSetRepository.GetByProductAndComponentIds(baseProduct.Id, componentId);
                    if (productSet != null) productSetRepository.Remove(productSet);

                    long addedProductSet = productSetRepository.Add(new ProductSet {
                        BaseProductId = baseProduct.Id,
                        ComponentProductId = componentId,
                        SetComponentsQty = componentForUpload.SetComponentsQty
                    });

                    if (!baseProduct.HasComponent) {
                        productRepository.UpdateProductHasComponent(baseProduct.Id);
                        baseProductIdsToUpdateInCaseOfFailure.Add(baseProduct.Id);
                    }

                    idsToDeleteInCaseOfFailure.Add(addedProductSet);
                }
            }

            Sender.Tell(null);
        } catch (Exception exc) {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            _productRepositoriesFactory.NewProductSetRepository(connection).DeleteAllByIds(idsToDeleteInCaseOfFailure);
            _productRepositoriesFactory.NewProductRepository(connection).UncheckProductHasComponent(baseProductIdsToUpdateInCaseOfFailure);

            Sender.Tell(exc);
        }
    }

    private void ProcessUploadOriginalNumbersFromFileMessage(UploadOriginalNumbersFromFileMessage message) {
        List<long> productOriginalNumberIdsToDeleteInCaseOfFailure = new();
        List<long> originalNumberIdsToDeleteInCaseOfFailure = new();

        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            List<OriginalNumberForUpload> productsForUpload =
                _xlsFactoryManager
                    .NewParseConfigurationXlsManager()
                    .GetOriginalNumbersForUploadByConfiguration(message.FilePath, message.Configuration);

            IProductRepository productRepository = _productRepositoriesFactory.NewProductRepository(connection);
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductOriginalNumberRepository productOriginalNumberRepository = _productRepositoriesFactory.NewProductOriginalNumberRepository(connection);
            IOriginalNumberRepository originalNumberRepository = _originalNumberRepositoryFactory.NewOriginalNumberRepository(connection);

            foreach (IGrouping<string, OriginalNumberForUpload> originalNumbersGroup in productsForUpload.GroupBy(g => g.VendorCode)) {
                Product baseProduct = getSingleProductRepository.GetProductByVendorCode(originalNumbersGroup.Key);

                if (baseProduct == null)
                    throw new ProductUploadParseException(ProductUploadParseExceptionType.NoProductByVendorCode, 0, 0, originalNumbersGroup.Key);

                if (message.Configuration.IsCleanBeforeLoading) {
                    IEnumerable<ProductOriginalNumber> productOriginalNumbers = productOriginalNumberRepository.GetByProductId(baseProduct.Id);

                    originalNumberRepository.RemoveAllByIds(productOriginalNumbers.Select(e => e.OriginalNumberId));

                    productOriginalNumberRepository.RemoveAllByIds(productOriginalNumbers.Select(e => e.Id));

                    productRepository.UpdateMainOriginalNumber(string.Empty, baseProduct.NetUid);

                    baseProduct.MainOriginalNumber = string.Empty;
                }

                foreach (OriginalNumberForUpload originalNumberForUpload in originalNumbersGroup.Select(group => group)) {
                    bool isMainOriginalNumber = false;

                    if (productOriginalNumberRepository
                        .CheckIsProductOriginalNumberExistsByBaseProductIdAndOriginalNumber(
                            baseProduct.Id,
                            originalNumberForUpload.OriginalNumber)) continue;

                    if (string.IsNullOrEmpty(baseProduct.MainOriginalNumber)) {
                        productRepository.UpdateMainOriginalNumber(originalNumberForUpload.OriginalNumber, baseProduct.NetUid);
                        baseProduct.MainOriginalNumber = originalNumberForUpload.OriginalNumber;
                        isMainOriginalNumber = true;
                    }

                    long originalNumberId = originalNumberRepository.Add(new OriginalNumber {
                        MainNumber = originalNumberForUpload.OriginalNumber,
                        Number = SpecialCharactersReplace.Replace(originalNumberForUpload.OriginalNumber, string.Empty)
                    });

                    originalNumberIdsToDeleteInCaseOfFailure.Add(originalNumberId);

                    long productOriginalNumberId = productOriginalNumberRepository.Add(new ProductOriginalNumber {
                        IsMainOriginalNumber = isMainOriginalNumber || baseProduct.MainOriginalNumber.Equals(originalNumberForUpload.OriginalNumber),
                        ProductId = baseProduct.Id,
                        OriginalNumberId = originalNumberId
                    });

                    productOriginalNumberIdsToDeleteInCaseOfFailure.Add(productOriginalNumberId);
                }
            }

            Sender.Tell(null);
        } catch (Exception exc) {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            _productRepositoriesFactory.NewProductOriginalNumberRepository(connection).DeleteAllByIds(productOriginalNumberIdsToDeleteInCaseOfFailure);
            _originalNumberRepositoryFactory.NewOriginalNumberRepository(connection).DeleteAllByIds(originalNumberIdsToDeleteInCaseOfFailure);

            Sender.Tell(exc);
        }
    }

    private void ProcessRemoveProductAnaloguesMessage(RemoveProductAnaloguesMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            IProductAnalogueRepository productAnalogueRepository = _productRepositoriesFactory.NewProductAnalogueRepository(connection);

            if (!productAnalogueRepository.CheckIsProductAnalogueExistsByBaseProductAndAnalogueNetIds(message.BaseProductNetId, message.AnalogueProductNetId)) {
                Sender.Tell("message");
                return;
            }

            if (message.RemoveIndirectAnalogues) {
                List<ProductAnalogue> existingAnaloguesFromDb = new();

                Product analogue = getSingleProductRepository.GetByNetIdWithoutIncludes(message.AnalogueProductNetId);

                existingAnaloguesFromDb.AddRange(
                    productAnalogueRepository
                        .GetAllProductAnaloguesByBaseProductVendorCode(analogue.VendorCode));

                existingAnaloguesFromDb.AddRange(
                    productAnalogueRepository
                        .GetAllProductAnaloguesByAnalogueVendorCode(analogue.VendorCode));

                productAnalogueRepository.DeleteAllByIds(existingAnaloguesFromDb.Select(e => e.Id));
            } else {
                productAnalogueRepository.DeleteByBaseProductAndAnalogueNetIds(message.BaseProductNetId, message.AnalogueProductNetId);
                productAnalogueRepository.DeleteByBaseProductAndAnalogueNetIds(message.AnalogueProductNetId, message.BaseProductNetId);
            }

            Sender.Tell(true);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessRemoveProductComponentsMessage(RemoveProductComponentsMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            IProductSetRepository productSetRepository = _productRepositoriesFactory.NewProductSetRepository(connection);

            if (message.IsProductSet) {
                if (!productSetRepository.CheckIfProductSetExistsByBaseProductAndComponentNetIds(message.ComponentNetId, message.BaseProductNetId)) {
                    Sender.Tell("new exception");
                    return;
                }

                productSetRepository.DeleteByBaseProductNetIdAndComponentNetId(message.ComponentNetId, message.BaseProductNetId);
            } else {
                if (!productSetRepository.CheckIfProductSetExistsByBaseProductAndComponentNetIds(message.BaseProductNetId, message.ComponentNetId)) {
                    Sender.Tell("new exception");
                    return;
                }

                productSetRepository.DeleteByBaseProductNetIdAndComponentNetId(message.BaseProductNetId, message.ComponentNetId);
            }

            Sender.Tell(true);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessDeleteProductMessage(DeleteProductMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _productRepositoriesFactory.NewProductRepository(connection).Remove(message.NetId);
    }

    private void ProcessAddProductSpecificationCodeToProductMessage(AddProductSpecificationCodeToProductMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.Product == null) {
            Sender.Tell(new Tuple<Product, string>(null, "Product entity can not be null."));
        } else if (message.Product.IsNew()) {
            Sender.Tell(new Tuple<Product, string>(null, "New product is not valid for current request."));
        } else {
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);

            if (message.Product.ProductSpecifications.Any(s => s.IsNew())) {
                IProductSpecificationRepository productSpecificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);

                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                productSpecificationRepository.SetInactiveByProductId(message.Product.Id, "pl");

                if (message.Product.ProductSpecifications.Any(s => s.IsNew())) {
                    message.Product.ProductSpecifications.Last(s => s.IsNew()).IsActive = true;

                    productSpecificationRepository.Add(
                        message.Product.ProductSpecifications
                            .Where(p => p.IsNew())
                            .Select(p => {
                                p.AddedById = user.Id;
                                p.ProductId = message.Product.Id;

                                return p;
                            })
                    );
                }
            }

            Sender.Tell(new Tuple<Product, string>(getSingleProductRepository.GetByNetId(message.Product.NetUid), string.Empty));
        }
    }

    private void ProcessAddNewProductSpecificationForCurrentProductMessage(AddNewProductSpecificationForCurrentProductMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.ProductSpecification == null) {
            Sender.Tell(new Tuple<Product, string>(null, "ProductSpecification entity can not be null."));
        } else if (message.ProductNetId.Equals(Guid.Empty)) {
            Sender.Tell(new Tuple<Product, string>(null, "You need to specify ProductNetId."));
        } else {
            IGetSingleProductRepository getSingleProductRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);

            Product product = getSingleProductRepository.GetByNetIdWithoutIncludes(message.ProductNetId);

            if (product == null) {
                Sender.Tell(new Tuple<Product, string>(null, "There is no such Product with provided ProductNetId."));
            } else {
                IProductSpecificationRepository productSpecificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);

                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                productSpecificationRepository.SetInactiveByProductId(product.Id, "pl");

                message.ProductSpecification.ProductId = product.Id;
                message.ProductSpecification.AddedById = user.Id;
                message.ProductSpecification.IsActive = true;

                productSpecificationRepository.Add(message.ProductSpecification);

                Sender.Tell(new Tuple<Product, string>(getSingleProductRepository.GetByNetId(message.ProductNetId), string.Empty));
            }
        }
    }

    private void ProcessAddNewProductSpecificationForAllProductsMessage(AddNewProductSpecificationForAllProductsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.ProductSpecification == null) {
            Sender.Tell(new Tuple<List<Product>, string>(null, "ProductSpecification entity can not be null."));
        } else if (string.IsNullOrEmpty(message.OldCode)) {
            Sender.Tell(new Tuple<List<Product>, string>(null, ProductResourceNames.NEED_SPECIFY_OLD_SPECIFICATION_CODE));
        } else {
            IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);

            List<Product> products = getMultipleProductsRepository.GetAllByActiveProductSpecificationCode(message.OldCode);

            if (!products.Any()) {
                Sender.Tell(new Tuple<List<Product>, string>(null, ProductResourceNames.NOT_PRODUCT_WITH_SPECIFY_CODE));
            } else {
                IProductSpecificationRepository productSpecificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);

                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                products.ForEach(product => productSpecificationRepository.SetInactiveByProductId(product.Id, "pl"));

                productSpecificationRepository.Add(
                    products
                        .Select(product => {
                            ProductSpecification specification = new() {
                                Name = !string.IsNullOrEmpty(message.ProductSpecification.Name)
                                    ? message.ProductSpecification.Name
                                    : product.ProductSpecifications.Any()
                                        ? product.ProductSpecifications.First().Name
                                        : string.Empty,
                                VATValue = message.ProductSpecification.VATValue,
                                CustomsValue = message.ProductSpecification.CustomsValue,
                                Duty = message.ProductSpecification.Duty,
                                DutyPercent = message.ProductSpecification.Duty + message.ProductSpecification.CustomsValue > 0
                                    ? message.ProductSpecification.VATValue * 100 /
                                      (message.ProductSpecification.Duty + message.ProductSpecification.CustomsValue)
                                    : 0,
                                SpecificationCode = message.ProductSpecification.SpecificationCode,
                                AddedById = user.Id,
                                ProductId = product.Id,
                                IsActive = true
                            };

                            return specification;
                        })
                );

                Sender.Tell(new Tuple<List<Product>, string>(products, string.Empty));
            }
        }
    }

    private void ProcessAddNewSpecificationCodesFromFileMessage(AddNewSpecificationCodesFromFileMessage message) {
        ProductSpecificationFileUploadResult uploadResult = new();

        List<ProductSpecificationWithVendorCode> parsedSpecifications =
            _xlsFactoryManager
                .NewParseConfigurationXlsManager()
                .GetProductSpecificationWithVendorCodesFromXlsx(message.PathToFile);

        if (parsedSpecifications.Any()) {
            uploadResult.ParsedCount = parsedSpecifications.Count;

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);
            IProductSpecificationRepository productSpecificationRepository = _productRepositoriesFactory.NewProductSpecificationRepository(connection);

            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UpdatedByNetId);

            foreach (ProductSpecificationWithVendorCode parsedSpecification in parsedSpecifications) {
                IEnumerable<Product> products = getMultipleProductsRepository.GetAllByVendorCodeWithActiveProductSpecification(parsedSpecification.VendorCode);

                if (products.Any())
                    foreach (Product product in products)
                        if (!product.ProductSpecifications.Any() ||
                            !product.ProductSpecifications.First().SpecificationCode.ToLower().Equals(parsedSpecification.Code)) {
                            productSpecificationRepository.SetInactiveByProductId(product.Id, "pl");

                            productSpecificationRepository.Add(new ProductSpecification {
                                ProductId = product.Id,
                                Name = parsedSpecification.Name,
                                SpecificationCode = parsedSpecification.Code,
                                AddedById = user.Id,
                                IsActive = true
                            });

                            uploadResult.SuccessfullyUpdatedCount++;
                        } else {
                            uploadResult.UpdateWasNotRequiredCount++;
                        }
                else
                    uploadResult.InvalidVendorCodes.Add(parsedSpecification.VendorCode);
            }
        } else {
            uploadResult.ParsedCount = 0;
            uploadResult.SuccessfullyUpdatedCount = 0;
        }

        Sender.Tell(uploadResult);

        NoltFolderManager.DeleteFile(message.PathToFile);
    }
}