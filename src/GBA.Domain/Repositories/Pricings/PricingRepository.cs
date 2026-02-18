using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Pricings;

public sealed class PricingRepository : IPricingRepository {
    private readonly IDbConnection _connection;

    public PricingRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Pricing pricing) {
        return _connection.Query<long>(
            "INSERT INTO [Pricing] (Name, Comment, ExtraCharge, BasePricingId, CurrencyId, PriceTypeId, Culture, CalculatedExtraCharge, ForVat, Updated) " +
            "VALUES (@Name, @Comment, @ExtraCharge, @BasePricingId, @CurrencyId, @PriceTypeId, @Culture, @CalculatedExtraCharge, @ForVat, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            pricing
        ).Single();
    }

    public void Update(Pricing pricing) {
        _connection.Execute(
            "UPDATE [Pricing] " +
            "SET Name = @Name, Comment = @Comment, ExtraCharge = @ExtraCharge, BasePricingId = @BasePricingId, CurrencyId = @CurrencyId, " +
            "PriceTypeId = @PriceTypeId, Culture = @Culture, CalculatedExtraCharge = @CalculatedExtraCharge, SortingPriority = @SortingPriority, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            pricing
        );
    }

    public void UpdatePricingPriorityById(long id, bool raise) {
        _connection.Execute(
            "UPDATE [Pricing] " +
            "SET SortingPriority = CASE WHEN @Raise = 1 THEN SortingPriority + 1 ELSE SortingPriority - 1 END WHERE ID = @Id ",
            new { Id = id, Raise = raise }
        );
    }

    public Pricing GetPricingByCurrentCultureWithHighestExtraCharge() {
        return _connection.Query<Pricing>(
            "SELECT TOP(1) * " +
            "FROM [Pricing] " +
            "WHERE [Pricing].Deleted = 0 " +
            "AND [Pricing].Culture = @Culture " +
            "ORDER BY [Pricing].CalculatedExtraCharge DESC",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).SingleOrDefault();
    }

    public long GetPricingIdByName(string name) {
        return _connection.Query<long>(
            "SELECT ID FROM Pricing " +
            "WHERE Name = @Name",
            new { Name = name }
        ).SingleOrDefault();
    }

    public Pricing GetById(long id) {
        Pricing pricingToReturn = null;

        string sql =
            "SELECT * " +
            "FROM [Pricing] " +
            "LEFT JOIN [PricingTranslation] AS [CurrentTranslation] " +
            "ON [Pricing].ID = [CurrentTranslation].PricingID " +
            "AND [CurrentTranslation].CultureCode = @Culture " +
            "AND [CurrentTranslation].Deleted = 0 " +
            "LEFT JOIN [PricingTranslation] " +
            "ON [Pricing].ID = [PricingTranslation].PricingID " +
            "AND [PricingTranslation].Deleted = 0 " +
            "LEFT JOIN [PriceType] " +
            "ON [Pricing].PriceTypeID = [PriceType].ID " +
            "LEFT JOIN [PriceTypeTranslation] " +
            "ON [PriceType].ID = [PriceTypeTranslation].PriceTypeID " +
            "AND [PriceTypeTranslation].CultureCode = @Culture " +
            "AND [PriceTypeTranslation].Deleted = 0 " +
            "LEFT JOIN [Currency] " +
            "ON [Pricing].CurrencyID = [Currency].ID " +
            "LEFT JOIN [CurrencyTranslation] " +
            "ON [Currency].ID = [CurrencyTranslation].CurrencyID " +
            "AND [CurrencyTranslation].CultureCode = @Culture " +
            "AND [CurrencyTranslation].Deleted = 0 " +
            "LEFT JOIN [Pricing] AS [BasePricing] " +
            "ON [Pricing].BasePricingID = [BasePricing].ID " +
            "WHERE [Pricing].ID = @Id";

        Type[] types = {
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(PricingTranslation),
            typeof(PriceType),
            typeof(PriceTypeTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Pricing)
        };

        Func<object[], Pricing> mapper = objects => {
            Pricing pricing = (Pricing)objects[0];
            PricingTranslation currentPricingTranslation = (PricingTranslation)objects[1];
            PricingTranslation pricingTranslation = (PricingTranslation)objects[2];
            PriceType type = (PriceType)objects[3];
            PriceTypeTranslation typeTranslation = (PriceTypeTranslation)objects[4];
            Currency currency = (Currency)objects[5];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[6];
            Pricing basePricing = (Pricing)objects[7];

            if (type != null) type.Name = typeTranslation?.Name;

            if (currentPricingTranslation != null) pricing.Name = currentPricingTranslation.Name;

            currency.Name = currencyTranslation?.Name;

            pricing.PriceType = type;
            pricing.BasePricing = basePricing;
            pricing.Currency = currency;

            if (pricingToReturn != null) {
                if (!pricingToReturn.PricingTranslations.Any(t => t.Id.Equals(pricingTranslation.Id))) pricingToReturn.PricingTranslations.Add(pricingTranslation);
            } else {
                if (!pricing.PricingTranslations.Any(t => t.Id.Equals(pricingTranslation.Id))) pricing.PricingTranslations.Add(pricingTranslation);

                pricingToReturn = pricing;
            }

            return pricing;
        };

        var props = new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sql, types, mapper, props);

        if (pricingToReturn.PricingTranslations.Any())
            pricingToReturn.PricingTranslations = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")
                ? pricingToReturn.PricingTranslations.OrderBy(t => t.CultureCode).ToArray()
                : pricingToReturn.PricingTranslations.OrderByDescending(t => t.CultureCode).ToArray();

        return pricingToReturn;
    }

    public Pricing GetByNetId(Guid netId) {
        Pricing pricingToReturn = null;

        string sql =
            "SELECT * " +
            "FROM [Pricing] " +
            "LEFT JOIN [PricingTranslation] AS [CurrentTranslation] " +
            "ON [Pricing].ID = [CurrentTranslation].PricingID " +
            "AND [CurrentTranslation].CultureCode = @Culture " +
            "AND [CurrentTranslation].Deleted = 0 " +
            "LEFT JOIN [PricingTranslation] " +
            "ON [Pricing].ID = [PricingTranslation].PricingID " +
            "AND [PricingTranslation].Deleted = 0 " +
            "LEFT JOIN [PriceType] " +
            "ON [Pricing].PriceTypeID = [PriceType].ID " +
            "LEFT JOIN [PriceTypeTranslation] " +
            "ON [PriceType].ID = [PriceTypeTranslation].PriceTypeID " +
            "AND [PriceTypeTranslation].CultureCode = @Culture " +
            "AND [PriceTypeTranslation].Deleted = 0 " +
            "LEFT JOIN [Currency] " +
            "ON [Pricing].CurrencyID = [Currency].ID " +
            "LEFT JOIN [CurrencyTranslation] " +
            "ON [Currency].ID = [CurrencyTranslation].CurrencyID " +
            "AND [CurrencyTranslation].CultureCode = @Culture " +
            "AND [CurrencyTranslation].Deleted = 0 " +
            "LEFT JOIN [Pricing] AS [BasePricing] " +
            "ON [Pricing].BasePricingID = [BasePricing].ID " +
            "WHERE [Pricing].NetUID = @NetId";

        Type[] types = {
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(PricingTranslation),
            typeof(PriceType),
            typeof(PriceTypeTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Pricing)
        };

        Func<object[], Pricing> mapper = objects => {
            Pricing pricing = (Pricing)objects[0];
            PricingTranslation currentPricingTranslation = (PricingTranslation)objects[1];
            PricingTranslation pricingTranslation = (PricingTranslation)objects[2];
            PriceType type = (PriceType)objects[3];
            PriceTypeTranslation typeTranslation = (PriceTypeTranslation)objects[4];
            Currency currency = (Currency)objects[5];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[6];
            Pricing basePricing = (Pricing)objects[7];

            if (type != null) type.Name = typeTranslation?.Name;

            if (currentPricingTranslation != null) pricing.Name = currentPricingTranslation.Name;

            currency.Name = currencyTranslation?.Name;

            pricing.PriceType = type;
            pricing.BasePricing = basePricing;
            pricing.Currency = currency;

            if (pricingToReturn != null) {
                if (!pricingToReturn.PricingTranslations.Any(t => t.Id.Equals(pricingTranslation.Id))) pricingToReturn.PricingTranslations.Add(pricingTranslation);
            } else {
                if (!pricing.PricingTranslations.Any(t => t.Id.Equals(pricingTranslation.Id))) pricing.PricingTranslations.Add(pricingTranslation);

                pricingToReturn = pricing;
            }

            return pricing;
        };

        var props = new { NetId = netId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sql, types, mapper, props);

        pricingToReturn.PricingTranslations = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")
            ? pricingToReturn.PricingTranslations.OrderBy(t => t.CultureCode).ToArray()
            : pricingToReturn.PricingTranslations.OrderByDescending(t => t.CultureCode).ToArray();

        return pricingToReturn;
    }

    public decimal GetCalculatedExtraChargeForCurrentPricing(Guid netId) {
        return _connection.Query<decimal>(
            "SELECT dbo.GetPricingExtraCharge(Pricing.[NetUID]) " +
            "FROM Pricing " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        ).Single();
    }

    public decimal GetCalculatedExtraChargeForCurrentPricing(long id) {
        decimal? result = _connection.Query<decimal?>(
            "SELECT dbo.GetPricingExtraCharge(Pricing.[NetUID]) " +
            "FROM Pricing " +
            "WHERE ID = @Id",
            new { Id = id }
        ).Single();

        return result ?? decimal.Zero;
    }

    public List<Pricing> GetAllWithCalculatedExtraCharge() {
        return _connection.Query<Pricing>(
            "SELECT [Pricing].ID " +
            ",[Pricing].BasePricingID " +
            ",[Pricing].Comment " +
            ",[Pricing].Created " +
            ",[Pricing].Culture " +
            ",[Pricing].CurrencyID " +
            ",[Pricing].Deleted " +
            ",dbo.GetPricingExtraCharge([Pricing].NetUID) AS [ExtraCharge] " +
            ",[Pricing].NetUID " +
            ",[Pricing].[Name] " +
            ",[Pricing].PriceTypeID " +
            ",[Pricing].Updated " +
            "FROM [views].[PricingView] [Pricing] " +
            "WHERE [Pricing].Deleted = 0 " +
            "AND [Pricing].[CultureCode] = @Culture",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).ToList();
    }

    public List<Pricing> GetAllWithCalculatedExtraChargeWithDynamicDiscounts(Guid productNetId) {
        return _connection.Query<Pricing, PricingProductGroupDiscount, PricingProductGroupDiscount, PricingProductGroupDiscount, Pricing, Pricing>(
            "DECLARE @ProductGroupId bigint;" +
            "SELECT @ProductGroupId = (" +
            "SELECT TOP(1) [ProductProductGroup].ProductGroupID " +
            "FROM [ProductProductGroup] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductProductGroup].ProductID " +
            "WHERE [ProductProductGroup].Deleted = 0 " +
            "AND [Product].NetUID = @ProductNetId" +
            "); " +
            "SELECT [Pricing].ID " +
            ",[Pricing].BasePricingID " +
            ",[Pricing].Comment " +
            ",[Pricing].Created " +
            ",[Pricing].Culture " +
            ",[Pricing].CurrencyID " +
            ",[Pricing].Deleted " +
            ",dbo.GetPricingExtraCharge([Pricing].NetUID) AS [ExtraCharge] " +
            ",[Pricing].NetUID " +
            ",[Pricing].[Name] " +
            ",[Pricing].PriceTypeID " +
            ",[Pricing].Updated " +
            ",[Pricing].SortingPriority " +
            ",[Discount3].* " +
            ",[Discount2].* " +
            ",[Discount1].* " +
            ",[BasePricing].* " +
            "FROM [views].[PricingView] [Pricing] " +
            "LEFT JOIN [PricingProductGroupDiscount] AS [Discount3] " +
            "ON [Discount3].PricingID = [Pricing].ID " +
            "AND [Discount3].ProductGroupID = @ProductGroupId " +
            "LEFT JOIN [PricingProductGroupDiscount] AS [Discount2] " +
            "ON [Discount2].PricingID  = [Discount3].BasePricingID " +
            "AND [Discount2].ProductGroupID = @ProductGroupId " +
            "LEFT JOIN [PricingProductGroupDiscount] AS [Discount1] " +
            "ON [Discount1].PricingID = [Discount2].BasePricingID " +
            "AND [Discount1].ProductGroupID = @ProductGroupId " +
            "LEFT JOIN [Pricing] AS [BasePricing] " +
            "ON [Pricing].BasePricingID = [BasePricing].ID " +
            "WHERE [Pricing].Deleted = 0 " +
            "AND [Pricing].CultureCode = @Culture " +
            "ORDER BY [Pricing].SortingPriority ASC, [Pricing].Name ASC ",
            (pricing, discount3, discount2, discount1, basePricing) => {
                if (discount1 != null) pricing.PricingProductGroupDiscounts.Add(discount1);

                if (discount2 != null) pricing.PricingProductGroupDiscounts.Add(discount2);

                if (discount3 != null) pricing.PricingProductGroupDiscounts.Add(discount3);

                pricing.BasePricing = basePricing;

                return pricing;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, ProductNetId = productNetId }
        ).ToList();
    }

    public Pricing GetByNetIdWithCalculatedExtraCharge(Guid netId) {
        return _connection.Query<Pricing>(
            "SELECT [ID] " +
            ",[BasePricingID] " +
            ",[Comment] " +
            ",[Created] " +
            ",[CurrencyID] " +
            ",[Deleted] " +
            ",[ExtraCharge] = dbo.GetPricingExtraCharge(Pricing.[NetUID]) " +
            ",[Name] " +
            ",[NetUID] " +
            ",[Updated] " +
            ",[PriceTypeID] " +
            ",[Culture] " +
            "FROM Pricing " +
            "WHERE [NetUID] = @NetId",
            new { NetId = netId.ToString() }
        ).SingleOrDefault();
    }

    public Pricing GetByIdWithCalculatedExtraCharge(long id) {
        return _connection.Query<Pricing>(
            "SELECT [ID] " +
            ",[BasePricingID] " +
            ",[Comment] " +
            ",[Created] " +
            ",[CurrencyID] " +
            ",[Deleted] " +
            ",[ExtraCharge] = dbo.GetPricingExtraCharge(Pricing.[NetUID]) " +
            ",[Name] " +
            ",[NetUID] " +
            ",[Updated] " +
            ",[PriceTypeID] " +
            ",[Culture] " +
            "FROM Pricing " +
            "WHERE [ID] = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public Pricing GetRetailPricingWithCalculatedExtraChargeByCulture() {
        return _connection.Query<Pricing>(
            "SELECT TOP(1) [ID] " +
            ",[BasePricingID] " +
            ",[Comment] " +
            ",[Created] " +
            ",[CurrencyID] " +
            ",[Deleted] " +
            ",[ExtraCharge] = dbo.GetPricingExtraCharge([Pricing].[NetUID]) " +
            ",[Name] " +
            ",[CalculatedExtraCharge] " +
            ",[NetUID] " +
            ",[Updated] " +
            ",[PriceTypeID] " +
            ",[Culture] " +
            "FROM [Pricing] " +
            "WHERE [Pricing].Culture = @Culture " +
            "ORDER BY dbo.GetPricingExtraCharge([Pricing].[NetUID]) DESC",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).SingleOrDefault();
    }

    public Pricing GetByClientAgreementNetIdWithCalculatedExtraCharge(Guid netId) {
        return _connection.Query<Pricing>(
            "SELECT Pricing.[ID] " +
            ",Pricing.[BasePricingID] " +
            ",Pricing.[Comment] " +
            ",Pricing.[Created] " +
            ",Pricing.[CurrencyID] " +
            ",Pricing.[Deleted] " +
            ",dbo.GetPricingExtraCharge(Pricing.[NetUID]) AS [ExtraCharge] " +
            ",Pricing.[Name] " +
            ",Pricing.[NetUID] " +
            ",[CalculatedExtraCharge] " +
            ",Pricing.[Updated] " +
            ",Pricing.[PriceTypeID] " +
            ",Pricing.[Culture] " +
            "FROM Pricing " +
            "LEFT JOIN Agreement " +
            "ON Pricing.ID = Agreement.PricingID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "WHERE ClientAgreement.NetUID = @NetId",
            new { NetId = netId.ToString() }
        ).SingleOrDefault();
    }

    public List<Pricing> GetAll() {
        List<Pricing> pricings = new();

        string sql =
            "SELECT * " +
            "FROM [Pricing] " +
            "LEFT JOIN [PricingTranslation] AS [CurrentTranslation] " +
            "ON [Pricing].ID = [CurrentTranslation].PricingID " +
            "AND [CurrentTranslation].CultureCode = @Culture " +
            "AND [CurrentTranslation].Deleted = 0 " +
            "LEFT JOIN [PricingTranslation] " +
            "ON [Pricing].ID = [PricingTranslation].PricingID " +
            "AND [PricingTranslation].Deleted = 0 " +
            "LEFT JOIN [PriceType] " +
            "ON [Pricing].PriceTypeID = [PriceType].ID " +
            "LEFT JOIN [PriceTypeTranslation] " +
            "ON [PriceType].ID = [PriceTypeTranslation].PriceTypeID " +
            "AND [PriceTypeTranslation].CultureCode = @Culture " +
            "AND [PriceTypeTranslation].Deleted = 0 " +
            "LEFT JOIN [Currency] " +
            "ON [Pricing].CurrencyID = [Currency].ID " +
            "LEFT JOIN [CurrencyTranslation] " +
            "ON [Currency].ID = [CurrencyTranslation].CurrencyID " +
            "AND [CurrencyTranslation].CultureCode = @Culture " +
            "AND [CurrencyTranslation].Deleted = 0 " +
            "LEFT JOIN [Pricing] AS [BasePricing] " +
            "ON [Pricing].BasePricingID = [BasePricing].ID " +
            "WHERE [Pricing].Deleted = 0 " +
            "ORDER BY [Pricing].[SortingPriority] ASC, [Pricing].[Name] ASC ";

        Type[] types = {
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(PricingTranslation),
            typeof(PriceType),
            typeof(PriceTypeTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Pricing)
        };

        Func<object[], Pricing> mapper = objects => {
            Pricing pricing = (Pricing)objects[0];
            PricingTranslation currentPricingTranslation = (PricingTranslation)objects[1];
            PricingTranslation pricingTranslation = (PricingTranslation)objects[2];
            PriceType type = (PriceType)objects[3];
            PriceTypeTranslation typeTranslation = (PriceTypeTranslation)objects[4];
            Currency currency = (Currency)objects[5];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[6];
            Pricing basePricing = (Pricing)objects[7];

            if (type != null) type.Name = typeTranslation?.Name;

            if (currentPricingTranslation != null) pricing.Name = currentPricingTranslation.Name;

            currency.Name = currencyTranslation?.Name;

            pricing.PriceType = type;
            pricing.BasePricing = basePricing;
            pricing.Currency = currency;

            if (pricings.Any(p => p.Id.Equals(pricing.Id))) {
                if (!pricings.First(p => p.Id.Equals(pricing.Id)).PricingTranslations.Any(t => t.Id.Equals(pricingTranslation.Id)))
                    pricings.First(p => p.Id.Equals(pricing.Id)).PricingTranslations.Add(pricingTranslation);
            } else {
                if (!pricing.PricingTranslations.Any(t => t.Id.Equals(pricingTranslation.Id))) pricing.PricingTranslations.Add(pricingTranslation);

                pricings.Add(pricing);
            }

            return pricing;
        };

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sql, types, mapper, props);

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl"))
            pricings.ForEach(p => {
                p.PricingTranslations = p.PricingTranslations.OrderBy(t => t.CultureCode).ToArray();
            });
        else
            pricings.ForEach(p => {
                p.PricingTranslations = p.PricingTranslations.OrderByDescending(t => t.CultureCode).ToArray();
            });

        return pricings;
    }

    public List<Pricing> GetAllBasePricings() {
        List<Pricing> pricings = new();

        string sql =
            "SELECT * FROM Pricing pricing " +
            "LEFT OUTER JOIN PricingTranslation AS CurrentTranslation " +
            "ON pricing.ID = CurrentTranslation.PricingID " +
            "AND CurrentTranslation.CultureCode = @Culture " +
            "AND CurrentTranslation.Deleted = 0 " +
            "LEFT OUTER JOIN PricingTranslation " +
            "ON pricing.ID = PricingTranslation.PricingID " +
            "AND PricingTranslation.Deleted = 0 " +
            "LEFT JOIN PriceType " +
            "ON pricing.PriceTypeID = PriceType.ID " +
            "LEFT JOIN PriceTypeTranslation " +
            "ON PriceType.ID = PriceTypeTranslation.PriceTypeID " +
            "AND PriceTypeTranslation.CultureCode = @Culture " +
            "AND PriceTypeTranslation.Deleted = 0 " +
            "LEFT JOIN Currency " +
            "ON pricing.CurrencyID = Currency.ID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON Currency.ID = CurrencyTranslation.CurrencyID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "AND CurrencyTranslation.Deleted = 0 " +
            "LEFT OUTER JOIN Pricing basePricing " +
            "ON pricing.BasePricingID = basePricing.ID " +
            "WHERE pricing.Deleted = 0 " +
            "AND pricing.PriceTypeID = 1";

        Type[] types = {
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(PricingTranslation),
            typeof(PriceType),
            typeof(PriceTypeTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Pricing)
        };

        Func<object[], Pricing> mapper = objects => {
            Pricing pricing = (Pricing)objects[0];
            PricingTranslation currentPricingTranslation = (PricingTranslation)objects[1];
            PricingTranslation pricingTranslation = (PricingTranslation)objects[2];
            PriceType type = (PriceType)objects[3];
            PriceTypeTranslation typeTranslation = (PriceTypeTranslation)objects[4];
            Currency currency = (Currency)objects[5];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[6];
            Pricing basePricing = (Pricing)objects[7];

            if (type != null) type.Name = typeTranslation?.Name;

            if (currentPricingTranslation != null) pricing.Name = currentPricingTranslation.Name;

            currency.Name = currencyTranslation?.Name;

            pricing.PriceType = type;
            pricing.BasePricing = basePricing;
            pricing.Currency = currency;

            if (pricings.Any(p => p.Id.Equals(pricing.Id))) {
                if (!pricings.First(p => p.Id.Equals(pricing.Id)).PricingTranslations.Any(t => t.Id.Equals(pricingTranslation.Id)))
                    pricings.First(p => p.Id.Equals(pricing.Id)).PricingTranslations.Add(pricingTranslation);
            } else {
                if (!pricing.PricingTranslations.Any(t => t.Id.Equals(pricingTranslation.Id))) pricing.PricingTranslations.Add(pricingTranslation);

                pricings.Add(pricing);
            }

            return pricing;
        };

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sql, types, mapper, props);

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl"))
            pricings.ForEach(p => {
                p.PricingTranslations = p.PricingTranslations.OrderBy(t => t.CultureCode).ToArray();
            });
        else
            pricings.ForEach(p => {
                p.PricingTranslations = p.PricingTranslations.OrderByDescending(t => t.CultureCode).ToArray();
            });

        return pricings;
    }

    public List<Pricing> GetAllWithBasePricings() {
        return _connection.Query<Pricing, Pricing, PricingTranslation, Currency, CurrencyTranslation, Pricing>(
            "SELECT * " +
            "FROM Pricing " +
            "LEFT JOIN Pricing AS BasePricing " +
            "ON Pricing.BasePricingID = BasePricing.ID " +
            "LEFT JOIN PricingTranslation " +
            "ON PricingTranslation.PricingID = Pricing.ID " +
            "AND PricingTranslation.CultureCode = @Culture " +
            "LEFT JOIN Currency " +
            "ON Pricing.CurrencyID = Currency.ID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "WHERE Pricing.Deleted = 0 " +
            "AND Pricing.Culture = @Culture",
            (pricing, basePricing, pricingTranslation, currency, currencyTranslation) => {
                if (currency != null) {
                    currency.Name = currencyTranslation?.Name ?? currency.Name;

                    pricing.Currency = currency;
                }

                pricing.Name = pricingTranslation?.Name ?? pricing.Name;
                pricing.BasePricing = basePricing;

                return pricing;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).ToList();
    }

    public List<Pricing> GetAllWithCalculatedExtraChargeByCurrentCulture() {
        return _connection.Query<Pricing>(
            "SELECT [Pricing].ID " +
            ",[Pricing].BasePricingID " +
            ",[Pricing].Comment " +
            ",[Pricing].Created " +
            ",[Pricing].Culture " +
            ",[Pricing].CurrencyID " +
            ",[Pricing].Deleted " +
            ",dbo.GetPricingExtraCharge([Pricing].NetUID) AS [ExtraCharge] " +
            ",[Pricing].NetUID " +
            ",[Pricing].[Name] " +
            ",[Pricing].PriceTypeID " +
            ",[Pricing].Updated " +
            "FROM [views].[PricingView] [Pricing] " +
            "WHERE [Pricing].Deleted = 0 " +
            "AND [Pricing].Culture = @Culture " +
            "AND [Pricing].CultureCode = @Culture",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).ToList();
    }

    public bool IsAnyAssignedToBasePricing(long basePricingId) {
        return _connection.Query<long>(
            "SELECT Pricing.ID FROM Pricing " +
            "WHERE Pricing.BasePricingID = @Id",
            new { Id = basePricingId }
        ).ToArray().Any();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Pricing SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}