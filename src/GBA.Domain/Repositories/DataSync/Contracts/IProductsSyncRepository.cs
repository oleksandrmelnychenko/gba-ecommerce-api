using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IProductsSyncRepository {
    IEnumerable<SyncProduct> GetAllSyncProducts();

    IEnumerable<SyncProduct> GetAmgAllSyncProducts();

    List<MeasureUnit> GetAllMeasureUnits();

    long Add(MeasureUnit measureUnit);

    void Add(MeasureUnitTranslation translation);

    void ExecuteQuery(string sqlExpression);

    void AssignProductsToProductGroups();

    List<SyncAnalogue> GetAllSyncAnalogues();

    List<SyncAnalogue> GetAmgAllSyncAnalogues();

    List<SyncComponent> GetAllSyncComponents();

    List<SyncComponent> GetAmgAllSyncComponents();

    List<SyncOriginalNumber> GetAllSyncOriginalNumbers();

    List<SyncOriginalNumber> GetAmgAllSyncOriginalNumbers();

    IEnumerable<Currency> GetAllCurrencies();

    IEnumerable<SyncPricing> GetAllSyncPricings();

    IEnumerable<SyncPricing> GetAmgAllSyncPricings();

    List<Pricing> GetAllPricings();

    long Add(Pricing pricing);

    void Add(PricingTranslation translation);

    void SetSharesPricings();

    IEnumerable<SyncProductPrice> GetAllSyncProductPrices();

    IEnumerable<SyncProductPrice> GetAmgAllSyncProductPrices();

    Product GetProductByVendorCode(string vendorCode);

    Product SearchProductByVendorCode(string vendorCode);

    List<Product> GetAllProducts();

    void CleanProductImages();

    void CleanCarBrandsAndAssignments();

    long Add(CarBrand carBrand);

    IEnumerable<PriceType> GetAllPriceTypes();

    void Update(Pricing pricing);
}