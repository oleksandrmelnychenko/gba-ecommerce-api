using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using GBA.Common.Extensions;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.ProductModels;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Products.Contracts;

namespace GBA.Services.Services.Products;

public sealed class ProductService : IProductService {
    private static readonly Regex SpecialCharactersReplace = new("[$&+,:;=?@#|/\\\\'\"�<>. ^*()%!\\-]", RegexOptions.Compiled);
    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;

    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;

    public ProductService(
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        IDbConnectionFactory connectionFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory) {
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _organizationRepositoriesFactory = organizationRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _connectionFactory = connectionFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;
    }

    public Task<Product> GetByNetIdForRetail(Guid productNetId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (productNetId.Equals(Guid.Empty)) throw new Exception("There's no such product in database");

        Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetWithHighestPriority();

        return Task.FromResult(_productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdForRetail(productNetId, storage.OrganizationId.Value, storage.ForVatProducts));
    }

    public Task<Product> GetByNetId(Guid productNetId, Guid clientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

        ClientAgreement clientAgreement =
            clientAgreementRepository.GetSelectedByClientNetId(clientNetId)
            ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(clientNetId);

        return Task.FromResult(
            _productRepositoriesFactory
                .NewGetSingleProductRepository(connection)
                .GetProductByNetId(
                    productNetId,
                    clientAgreement.NetUid,
                    clientAgreement.Agreement.WithVATAccounting,
                    clientAgreement.Agreement.CurrencyId,
                    clientAgreement.Agreement.OrganizationId
                )
        );
    }

    public Task<Product> GetProductBySlug(string slug, Guid clientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (clientNetId.Equals(Guid.Empty))
            return Task.FromResult(_productRepositoriesFactory.NewGetSingleProductRepository(connection).GetBySlug(slug, Guid.Empty, Guid.Empty));

        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

        ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, false);
        ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, true);

        return Task.FromResult(
            _productRepositoriesFactory
                .NewGetSingleProductRepository(connection)
                .GetBySlug(
                    slug,
                    nonVatAgreement?.NetUid ?? Guid.Empty,
                    vatAgreement?.NetUid
                )
        );
    }

    public Task<List<FromSearchProduct>> GetAllFromSearch(string value, Guid currentClientNetId, long limit, long offset, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            // if (limit.Equals(0)) limit = 20;
            // if (offset < 0) offset = 0;

            IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);
            IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);

            if (string.IsNullOrEmpty(value)) value = string.Empty;

            // Single-pass normalization: trim, lowercase, and remove Polish diacritics
            value = StringOptimizations.NormalizeForSearch(value);

            dynamic props = new ExpandoObject();

            props.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            props.Limit = limit;
            props.Offset = offset;
            props.Value = SpecialCharactersReplace.Replace(value, string.Empty);

            string[] concreteValues = value.Split(' ');

            if (concreteValues.Length > 1)
                for (int i = 0; i < concreteValues.Length; i++)
                    (props as ExpandoObject).AddProperty($"Var{i}", SpecialCharactersReplace.Replace(concreteValues[i], string.Empty));

            StringBuilder builder = new();

            // Stage Zero for combining Product data
            builder.Append(";WITH [SearchStage_Zero] ");
            builder.Append("AS ( ");
            builder.Append("SELECT [Product].ID ");
            builder.Append(", [Product].SearchName ");
            builder.Append(", [Product].SearchDescription ");

            if (props.Culture.Equals("pl")) {
                builder.Append(", [Product].SearchNamePL ");
                builder.Append(", [Product].SearchDescriptionPL ");
            } else {
                builder.Append(", [Product].SearchNameUA ");
                builder.Append(", [Product].SearchDescriptionUA ");
            }

            builder.Append(", [Product].SearchVendorCode ");
            builder.Append(", [Product].SearchSize ");
            builder.Append(", [Product].MainOriginalNumber ");
            builder.Append(", ( ");
            builder.Append("CASE ");
            builder.Append("WHEN ([ProductAvailability].Amount > 0) ");
            builder.Append("THEN 1 ");
            builder.Append("ELSE 0 ");
            builder.Append("END ");
            builder.Append(") AS [Available] ");
            builder.Append("FROM [Product] ");
            builder.Append("LEFT JOIN [ProductAvailability] ");
            builder.Append("ON [ProductAvailability].ProductID = [Product].ID ");
            // builder.Append("LEFT JOIN [Storage] "); // Removed in updated SQL
            // builder.Append("ON [Storage].ID = [ProductAvailability].StorageID ");
            // builder.Append("AND [Storage].Locale = @Culture ");
            builder.Append("WHERE [Product].Deleted = 0 ");
            builder.Append("), ");

            if (concreteValues.Length > 1) {
                string lastProductsStageName = "[SearchStage_Zero] ";
                string lastOriginalNumbersStageName = string.Empty;

                for (int i = 0; i < concreteValues.Length; i++) {
                    string currentProductsStageName = $"[Search_Stage{i}] ";
                    string currentOriginalNumbersStageName = $"[OriginalNumbers_Stage{i}] ";
                    bool isFirstStage = i == 0;

                    // --- Product Stage ---
                    builder.Append(currentProductsStageName);
                    builder.Append("AS ( ");
                    builder.Append("SELECT [Product].ID ");
                    builder.Append(", [Product].SearchName ");
                    builder.Append(", [Product].SearchDescription ");

                    if (props.Culture.Equals("pl")) {
                        builder.Append(", [Product].SearchNamePL ");
                        builder.Append(", [Product].SearchDescriptionPL ");
                    } else {
                        builder.Append(", [Product].SearchNameUA ");
                        builder.Append(", [Product].SearchDescriptionUA ");
                    }

                    builder.Append(", [Product].SearchVendorCode ");
                    builder.Append(", [Product].SearchSize ");
                    builder.Append(", [Product].MainOriginalNumber ");
                    builder.Append(", [Product].[Available] ");

                    if (isFirstStage) {
                        builder.Append($" , PATINDEX('%' + @Var{i} + '%', [Product].SearchName) AS [SearchName_Match] ");
                        builder.Append($" , PATINDEX('%' + @Var{i} + '%', [Product].SearchDescription) AS [SearchDescription_Match] ");
                        if (props.Culture.Equals("pl")) {
                            builder.Append($" , PATINDEX('%' + @Var{i} + '%', [Product].SearchNamePL) AS [SearchNamePL_Match] ");
                            builder.Append($" , PATINDEX('%' + @Var{i} + '%', [Product].SearchDescriptionPL) AS [SearchDescriptionPL_Match] ");
                            builder.Append(" , 0 AS [SearchNameUA_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionUA_Match] ");
                        } else {
                            builder.Append(" , 0 AS [SearchNamePL_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionPL_Match] ");
                            builder.Append($" , PATINDEX('%' + @Var{i} + '%', [Product].SearchNameUA) AS [SearchNameUA_Match] ");
                            builder.Append($" , PATINDEX('%' + @Var{i} + '%', [Product].SearchDescriptionUA) AS [SearchDescriptionUA_Match] ");
                        }

                        builder.Append($" , PATINDEX('%' + @Var{i} + '%', [Product].SearchVendorCode) AS [SearchVendorCode_Match] ");
                        builder.Append($" , PATINDEX('%' + @Var{i} + '%', [Product].SearchSize) AS [SearchSize_Match] ");
                        builder.Append($" , PATINDEX('%' + @Var{i} + '%', [Product].MainOriginalNumber) AS [OriginalNumber_Match] ");
                    } else {
                        // Waterfall match logic: Keep 1 if already matched, otherwise check new word
                        builder.Append($" , CASE WHEN [Product].SearchName_Match > 0 THEN 1 ELSE PATINDEX('%' + @Var{i} + '%', [Product].SearchName) END AS [SearchName_Match] ");
                        builder.Append(
                            $" , CASE WHEN [Product].SearchDescription_Match > 0 THEN 1 ELSE PATINDEX('%' + @Var{i} + '%', [Product].SearchDescription) END AS [SearchDescription_Match] ");
                        if (props.Culture.Equals("pl")) {
                            builder.Append(
                                $" , CASE WHEN [Product].SearchNamePL_Match > 0 THEN 1 ELSE PATINDEX('%' + @Var{i} + '%', [Product].SearchNamePL) END AS [SearchNamePL_Match] ");
                            builder.Append(
                                $" , CASE WHEN [Product].SearchDescriptionPL_Match > 0 THEN 1 ELSE PATINDEX('%' + @Var{i} + '%', [Product].SearchDescriptionPL) END AS [SearchDescriptionPL_Match] ");
                            builder.Append(" , 0 AS [SearchNameUA_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionUA_Match] ");
                        } else {
                            builder.Append(" , 0 AS [SearchNamePL_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionPL_Match] ");
                            builder.Append(
                                $" , CASE WHEN [Product].SearchNameUA_Match > 0 THEN 1 ELSE PATINDEX('%' + @Var{i} + '%', [Product].SearchNameUA) END AS [SearchNameUA_Match] ");
                            builder.Append(
                                $" , CASE WHEN [Product].SearchDescriptionUA_Match > 0 THEN 1 ELSE PATINDEX('%' + @Var{i} + '%', [Product].SearchDescriptionUA) END AS [SearchDescriptionUA_Match] ");
                        }

                        builder.Append(
                            $" , CASE WHEN [Product].SearchVendorCode_Match > 0 THEN 1 ELSE PATINDEX('%' + @Var{i} + '%', [Product].SearchVendorCode) END AS [SearchVendorCode_Match] ");
                        builder.Append($" , CASE WHEN [Product].SearchSize_Match > 0 THEN 1 ELSE PATINDEX('%' + @Var{i} + '%', [Product].SearchSize) END AS [SearchSize_Match] ");
                        builder.Append(
                            $" , CASE WHEN [Product].OriginalNumber_Match > 0 THEN 1 ELSE PATINDEX('%' + @Var{i} + '%', [Product].MainOriginalNumber) END AS [OriginalNumber_Match] ");
                    }

                    // Last stage specific columns
                    if (i.Equals(concreteValues.Length - 1))
                        // builder.Append(props.Culture.Equals("pl") ? ", [Product].SearchNamePL AS [SearchName] ," : ", [Product].SearchNameUA AS [SearchName] ,");
                        builder.Append(", 0 AS [HundredPercentMatch] ");

                    builder.Append($"FROM {lastProductsStageName}AS [Product] ");
                    builder.Append("WHERE ");
                    builder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchName) > 0 ");
                    builder.Append("OR ");
                    builder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchDescription) > 0 ");
                    builder.Append("OR ");

                    if (props.Culture.Equals("pl")) {
                        builder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchNamePL) > 0 ");
                        builder.Append("OR ");
                        builder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchDescriptionPL) > 0 ");
                    } else {
                        builder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchNameUA) > 0 ");
                        builder.Append("OR ");
                        builder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchDescriptionUA) > 0 ");
                    }

                    builder.Append("OR ");
                    builder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchVendorCode) > 0 ");
                    builder.Append("OR ");
                    builder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchSize) > 0 ");
                    builder.Append("OR ");
                    builder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].MainOriginalNumber) > 0 ");
                    builder.Append("), ");

                    // --- Original Numbers Stage ---
                    if (isFirstStage) {
                        builder.Append(currentOriginalNumbersStageName);
                        builder.Append("AS ( ");
                        builder.Append("SELECT [Product].ID ");

                        builder.Append(props.Culture.Equals("pl") ? ", [Product].SearchNamePL AS [SearchName] " : ", [Product].SearchNameUA AS [SearchName] ");

                        builder.Append(", ( ");
                        builder.Append("CASE ");
                        builder.Append("WHEN ([ProductAvailability].Amount > 0) ");
                        builder.Append("THEN 1 ");
                        builder.Append("ELSE 0 ");
                        builder.Append("END ");
                        builder.Append(") AS [Available] ");
                        builder.Append(", ( ");
                        builder.Append("CASE ");
                        builder.Append($"WHEN [OriginalNumber].Number = @Var{i} ");
                        builder.Append("THEN 1 ");
                        builder.Append("ELSE 0 ");
                        builder.Append("END ");
                        builder.Append(") AS [HundredPercentMatch] ");
                        builder.Append(", [OriginalNumber].Number AS [Number] ");

                        // Zeros for first stage non-original matches
                        builder.Append(" , 0 AS [SearchName_Match] ");
                        builder.Append(" , 0 AS [SearchDescription_Match] ");
                        if (props.Culture.Equals("pl")) {
                            builder.Append(" , 0 AS [SearchNamePL_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionPL_Match] ");
                            builder.Append(" , 0 AS [SearchNameUA_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionUA_Match] ");
                        } else {
                            builder.Append(" , 0 AS [SearchNamePL_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionPL_Match] ");
                            builder.Append(" , 0 AS [SearchNameUA_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionUA_Match] ");
                        }

                        builder.Append(" , 0 AS [SearchVendorCode_Match] ");
                        builder.Append(" , 0 AS [SearchSize_Match] ");
                        builder.Append($" , PATINDEX('%' + @Var{i} + '%', [OriginalNumber].Number) AS [OriginalNumber_Match] ");

                        builder.Append("FROM [OriginalNumber] ");
                        builder.Append("LEFT JOIN [ProductOriginalNumber] ");
                        builder.Append("ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID ");
                        builder.Append("AND [ProductOriginalNumber].Deleted = 0 ");
                        builder.Append("LEFT JOIN [Product] ");
                        builder.Append("ON [Product].ID = [ProductOriginalNumber].ProductID ");
                        builder.Append("LEFT JOIN [ProductAvailability] ");
                        builder.Append("ON [ProductAvailability].ProductID = [Product].ID ");
                        builder.Append("WHERE [Product].Deleted = 0 ");
                        builder.Append($"AND PATINDEX('%' + @Var{i} + '%', [OriginalNumber].Number) > 0 ");
                        builder.Append("), ");
                    } else {
                        builder.Append(currentOriginalNumbersStageName);
                        builder.Append("AS ( ");
                        builder.Append("SELECT [OriginalNumber].ID ");
                        builder.Append(", [OriginalNumber].[Available] ");
                        builder.Append(", [OriginalNumber].[HundredPercentMatch] ");
                        builder.Append(", [OriginalNumber].[Number] ");
                        builder.Append(", [OriginalNumber].[SearchName] ");

                        // Zeros for subsequent stages
                        builder.Append(" , 0 AS [SearchName_Match] ");
                        builder.Append(" , 0 AS [SearchDescription_Match] ");
                        if (props.Culture.Equals("pl")) {
                            builder.Append(" , 0 AS [SearchNamePL_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionPL_Match] ");
                            builder.Append(" , 0 AS [SearchNameUA_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionUA_Match] ");
                        } else {
                            builder.Append(" , 0 AS [SearchNamePL_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionPL_Match] ");
                            builder.Append(" , 0 AS [SearchNameUA_Match] ");
                            builder.Append(" , 0 AS [SearchDescriptionUA_Match] ");
                        }

                        builder.Append(" , 0 AS [SearchVendorCode_Match] ");
                        builder.Append(" , 0 AS [SearchSize_Match] ");
                        builder.Append(
                            $" , CASE WHEN [OriginalNumber].OriginalNumber_Match > 0 THEN 1 ELSE PATINDEX('%' + @Var{i} + '%', [OriginalNumber].Number) END AS [OriginalNumber_Match] ");

                        builder.Append($"FROM {lastOriginalNumbersStageName} AS [OriginalNumber] ");
                        builder.Append($"WHERE PATINDEX('%' + @Var{i} + '%', [OriginalNumber].Number) > 0 ");
                        builder.Append("), ");
                    }

                    lastProductsStageName = currentProductsStageName;
                    lastOriginalNumbersStageName = currentOriginalNumbersStageName;
                }

                //Uniting results of search by Product data and by Original number
                builder.Append("[United_CTE] ");
                builder.Append("AS ( ");
                builder.Append("SELECT [LastStageByProduct].ID ");
                builder.Append(", [LastStageByProduct].Available ");
                builder.Append(", [LastStageByProduct].SearchName ");
                builder.Append(", [LastStageByProduct].HundredPercentMatch ");
                builder.Append(", [LastStageByProduct].SearchName_Match ");
                builder.Append(", [LastStageByProduct].SearchDescription_Match ");
                if (props.Culture.Equals("pl")) {
                    builder.Append(", [LastStageByProduct].SearchNamePL_Match ");
                    builder.Append(", [LastStageByProduct].SearchDescriptionPL_Match ");
                    builder.Append(", [LastStageByProduct].SearchNameUA_Match ");
                    builder.Append(", [LastStageByProduct].SearchDescriptionUA_Match ");
                } else {
                    builder.Append(", [LastStageByProduct].SearchNamePL_Match ");
                    builder.Append(", [LastStageByProduct].SearchDescriptionPL_Match ");
                    builder.Append(", [LastStageByProduct].SearchNameUA_Match ");
                    builder.Append(", [LastStageByProduct].SearchDescriptionUA_Match ");
                }

                builder.Append(", [LastStageByProduct].SearchVendorCode_Match ");
                builder.Append(", [LastStageByProduct].SearchSize_Match ");
                builder.Append(", [LastStageByProduct].OriginalNumber_Match ");
                builder.Append($"FROM {lastProductsStageName}AS [LastStageByProduct]");
                builder.Append("UNION ");
                builder.Append($"SELECT {lastOriginalNumbersStageName}.ID ");
                builder.Append($", {lastOriginalNumbersStageName}.Available ");
                builder.Append($", {lastOriginalNumbersStageName}.SearchName ");
                builder.Append($", {lastOriginalNumbersStageName}.HundredPercentMatch ");
                builder.Append($", {lastOriginalNumbersStageName}.SearchName_Match ");
                builder.Append($", {lastOriginalNumbersStageName}.SearchDescription_Match ");
                if (props.Culture.Equals("pl")) {
                    builder.Append($", {lastOriginalNumbersStageName}.SearchNamePL_Match ");
                    builder.Append($", {lastOriginalNumbersStageName}.SearchDescriptionPL_Match ");
                    builder.Append($", {lastOriginalNumbersStageName}.SearchNameUA_Match ");
                    builder.Append($", {lastOriginalNumbersStageName}.SearchDescriptionUA_Match ");
                } else {
                    builder.Append($", {lastOriginalNumbersStageName}.SearchNamePL_Match ");
                    builder.Append($", {lastOriginalNumbersStageName}.SearchDescriptionPL_Match ");
                    builder.Append($", {lastOriginalNumbersStageName}.SearchNameUA_Match ");
                    builder.Append($", {lastOriginalNumbersStageName}.SearchDescriptionUA_Match ");
                }

                builder.Append($", {lastOriginalNumbersStageName}.SearchVendorCode_Match ");
                builder.Append($", {lastOriginalNumbersStageName}.SearchSize_Match ");
                builder.Append($", {lastOriginalNumbersStageName}.OriginalNumber_Match ");
                builder.Append($"FROM {lastOriginalNumbersStageName}");

                builder.Append("), ");
            } else {
                //Stages of searching by full entered search value by Products data and OriginalNumbers
                builder.Append("[Search_FullValue] ");
                builder.Append("AS ( ");
                builder.Append("SELECT [Product].ID ");
                builder.Append(", [Product].Available ");

                builder.Append(props.Culture.Equals("pl") ? ", [Product].SearchNamePL AS [SearchName] " : ", [Product].SearchNameUA AS [SearchName] ");

                builder.Append(", ( ");
                builder.Append("CASE ");

                builder.Append(props.Culture.Equals("pl")
                    ? "WHEN [Product].SearchNamePL = @Value OR [Product].SearchVendorCode = @Value "
                    : "WHEN [Product].SearchNameUA = @Value OR [Product].SearchVendorCode = @Value ");

                builder.Append("THEN 1 ");
                builder.Append("ELSE 0 ");
                builder.Append("END ");
                builder.Append(") AS [HundredPercentMatch] ");

                // Match Columns for Single Value
                builder.Append(" , PATINDEX('%' + @Value + '%', [Product].SearchName) AS [SearchName_Match] ");
                builder.Append(" , PATINDEX('%' + @Value + '%', [Product].SearchDescription) AS [SearchDescription_Match] ");
                if (props.Culture.Equals("pl")) {
                    builder.Append(" , PATINDEX('%' + @Value + '%', [Product].SearchNamePL) AS [SearchNamePL_Match] ");
                    builder.Append(" , PATINDEX('%' + @Value + '%', [Product].SearchDescriptionPL) AS [SearchDescriptionPL_Match] ");
                    builder.Append(" , 0 AS [SearchNameUA_Match] ");
                    builder.Append(" , 0 AS [SearchDescriptionUA_Match] ");
                } else {
                    builder.Append(" , 0 AS [SearchNamePL_Match] ");
                    builder.Append(" , 0 AS [SearchDescriptionPL_Match] ");
                    builder.Append(" , PATINDEX('%' + @Value + '%', [Product].SearchNameUA) AS [SearchNameUA_Match] ");
                    builder.Append(" , PATINDEX('%' + @Value + '%', [Product].SearchDescriptionUA) AS [SearchDescriptionUA_Match] ");
                }

                builder.Append(" , PATINDEX('%' + @Value + '%', [Product].SearchVendorCode) AS [SearchVendorCode_Match] ");
                builder.Append(" , PATINDEX('%' + @Value + '%', [Product].SearchSize) AS [SearchSize_Match] ");
                builder.Append(" , PATINDEX('%' + @Value + '%', [Product].MainOriginalNumber) AS [OriginalNumber_Match] ");

                builder.Append("FROM [SearchStage_Zero] AS [Product] ");
                builder.Append("WHERE ");
                builder.Append("PATINDEX('%' + @Value + '%', [Product].SearchName) > 0 ");
                builder.Append("OR ");
                builder.Append("PATINDEX('%' + @Value + '%', [Product].SearchDescription) > 0 ");
                builder.Append("OR ");

                if (props.Culture.Equals("pl")) {
                    builder.Append("PATINDEX('%' + @Value + '%', [Product].SearchNamePL) > 0 ");
                    builder.Append("OR ");
                    builder.Append("PATINDEX('%' + @Value + '%', [Product].SearchDescriptionPL) > 0 ");
                } else {
                    builder.Append("PATINDEX('%' + @Value + '%', [Product].SearchNameUA) > 0 ");
                    builder.Append("OR ");
                    builder.Append("PATINDEX('%' + @Value + '%', [Product].SearchDescriptionUA) > 0 ");
                }

                builder.Append("OR ");
                builder.Append("PATINDEX('%' + @Value + '%', [Product].SearchVendorCode) > 0 ");
                builder.Append("OR ");
                builder.Append("PATINDEX('%' + @Value + '%', [Product].SearchSize) > 0 ");
                builder.Append("OR ");
                builder.Append("PATINDEX('%' + @Value + '%', [Product].MainOriginalNumber) > 0 ");
                builder.Append("), ");


                builder.Append("[OriginalNumbers_FullValue] ");
                builder.Append("AS ( ");
                builder.Append("SELECT [Product].ID ");

                builder.Append(props.Culture.Equals("pl") ? ", [Product].SearchNamePL AS [SearchName] " : ", [Product].SearchNameUA AS [SearchName] ");

                builder.Append(", ( ");
                builder.Append("CASE ");
                builder.Append("WHEN ([ProductAvailability].Amount > 0) ");
                builder.Append("THEN 1 ");
                builder.Append("ELSE 0 ");
                builder.Append("END ");
                builder.Append(") AS [Available] ");
                builder.Append(", ( ");
                builder.Append("CASE ");
                builder.Append("WHEN [OriginalNumber].Number = @Value ");
                builder.Append("THEN 1 ");
                builder.Append("ELSE 0 ");
                builder.Append("END ");
                builder.Append(") AS [HundredPercentMatch] ");

                // Zeros for Match Columns in Original Number CTE
                builder.Append(" , 0 AS [SearchName_Match] ");
                builder.Append(" , 0 AS [SearchDescription_Match] ");
                if (props.Culture.Equals("pl")) {
                    builder.Append(" , 0 AS [SearchNamePL_Match] ");
                    builder.Append(" , 0 AS [SearchDescriptionPL_Match] ");
                    builder.Append(" , 0 AS [SearchNameUA_Match] ");
                    builder.Append(" , 0 AS [SearchDescriptionUA_Match] ");
                } else {
                    builder.Append(" , 0 AS [SearchNamePL_Match] ");
                    builder.Append(" , 0 AS [SearchDescriptionPL_Match] ");
                    builder.Append(" , 0 AS [SearchNameUA_Match] ");
                    builder.Append(" , 0 AS [SearchDescriptionUA_Match] ");
                }

                builder.Append(" , 0 AS [SearchVendorCode_Match] ");
                builder.Append(" , 0 AS [SearchSize_Match] ");
                builder.Append(" , PATINDEX('%' + @Value + '%', [OriginalNumber].Number) AS [OriginalNumber_Match] ");

                builder.Append("FROM [OriginalNumber] ");
                builder.Append("LEFT JOIN [ProductOriginalNumber] ");
                builder.Append("ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID ");
                builder.Append("AND [ProductOriginalNumber].Deleted = 0 ");
                builder.Append("LEFT JOIN [Product] ");
                builder.Append("ON [Product].ID = [ProductOriginalNumber].ProductID ");
                builder.Append("LEFT JOIN [ProductAvailability] ");
                builder.Append("ON [ProductAvailability].ProductID = [Product].ID ");
                builder.Append("WHERE [Product].Deleted = 0 ");
                builder.Append("AND PATINDEX('%' + @Value + '%', [OriginalNumber].Number) > 0 ");
                builder.Append("), ");

                //Uniting results of search by Product data and by Original number
                builder.Append("[United_CTE] ");
                builder.Append("AS ( ");
                builder.Append("SELECT [Search_FullValue].ID ");
                builder.Append(", [Search_FullValue].Available ");
                builder.Append(", [Search_FullValue].SearchName ");
                builder.Append(", [Search_FullValue].HundredPercentMatch ");
                builder.Append(", [Search_FullValue].SearchName_Match ");
                builder.Append(", [Search_FullValue].SearchDescription_Match ");
                if (props.Culture.Equals("pl")) {
                    builder.Append(", [Search_FullValue].SearchNamePL_Match ");
                    builder.Append(", [Search_FullValue].SearchDescriptionPL_Match ");
                    builder.Append(", [Search_FullValue].SearchNameUA_Match ");
                    builder.Append(", [Search_FullValue].SearchDescriptionUA_Match ");
                } else {
                    builder.Append(", [Search_FullValue].SearchNamePL_Match ");
                    builder.Append(", [Search_FullValue].SearchDescriptionPL_Match ");
                    builder.Append(", [Search_FullValue].SearchNameUA_Match ");
                    builder.Append(", [Search_FullValue].SearchDescriptionUA_Match ");
                }

                builder.Append(", [Search_FullValue].SearchVendorCode_Match ");
                builder.Append(", [Search_FullValue].SearchSize_Match ");
                builder.Append(", [Search_FullValue].OriginalNumber_Match ");
                builder.Append("FROM [Search_FullValue] ");
                builder.Append("UNION ");
                builder.Append("SELECT [OriginalNumbers_FullValue].ID ");
                builder.Append(", [OriginalNumbers_FullValue].Available ");
                builder.Append(", [OriginalNumbers_FullValue].SearchName ");
                builder.Append(", [OriginalNumbers_FullValue].HundredPercentMatch ");
                builder.Append(", [OriginalNumbers_FullValue].SearchName_Match ");
                builder.Append(", [OriginalNumbers_FullValue].SearchDescription_Match ");
                if (props.Culture.Equals("pl")) {
                    builder.Append(", [OriginalNumbers_FullValue].SearchNamePL_Match ");
                    builder.Append(", [OriginalNumbers_FullValue].SearchDescriptionPL_Match ");
                    builder.Append(", [OriginalNumbers_FullValue].SearchNameUA_Match ");
                    builder.Append(", [OriginalNumbers_FullValue].SearchDescriptionUA_Match ");
                } else {
                    builder.Append(", [OriginalNumbers_FullValue].SearchNamePL_Match ");
                    builder.Append(", [OriginalNumbers_FullValue].SearchDescriptionPL_Match ");
                    builder.Append(", [OriginalNumbers_FullValue].SearchNameUA_Match ");
                    builder.Append(", [OriginalNumbers_FullValue].SearchDescriptionUA_Match ");
                }

                builder.Append(", [OriginalNumbers_FullValue].SearchVendorCode_Match ");
                builder.Append(", [OriginalNumbers_FullValue].SearchSize_Match ");
                builder.Append(", [OriginalNumbers_FullValue].OriginalNumber_Match ");
                builder.Append("FROM [OriginalNumbers_FullValue] ");
                builder.Append("), ");
            }

            //Adding row numbers for virtualization
            builder.Append("[Rowed_CTE] ");
            builder.Append("AS ( ");
            builder.Append("SELECT [Product].ID ");
            builder.Append(", MAX([Product].HundredPercentMatch) AS [HundredPercentMatch] ");
            builder.Append(", MAX([Product].Available) AS [Available] ");

            builder.Append(", ROW_NUMBER() OVER(ORDER BY ");
            builder.Append("MAX([Product].Available) DESC, ");
            builder.Append("MAX([Product].HundredPercentMatch) DESC, ");
            builder.Append("(CASE WHEN MAX([Product].OriginalNumber_Match) > 0 THEN 1 ELSE 0 END) DESC, ");
            builder.Append("(CASE WHEN MAX([Product].SearchVendorCode_Match) > 0 THEN 1 ELSE 0 END) DESC, ");
            builder.Append("(CASE WHEN MAX([Product].OriginalNumber_Match) > 0 OR MAX([Product].SearchName_Match) > 0 THEN 1 ELSE 0 END) DESC, ");

            if (props.Culture.Equals("pl"))
                builder.Append("(CASE WHEN MAX([Product].SearchDescriptionPL_Match) > 0 OR MAX([Product].SearchDescription_Match) > 0 THEN 1 ELSE 0 END) DESC, ");
            else
                builder.Append("(CASE WHEN MAX([Product].SearchDescriptionUA_Match) > 0 OR MAX([Product].SearchDescription_Match) > 0 THEN 1 ELSE 0 END) DESC, ");

            builder.Append("(CASE WHEN MAX([Product].SearchSize_Match) > 0 THEN 1 ELSE 0 END) DESC, ");
            builder.Append("[Product].SearchName) AS [RowNumber] ");

            builder.Append("FROM [United_CTE] AS [Product] ");
            builder.Append("GROUP BY [Product].ID, [Product].SearchName ");
            builder.Append(") ");

            //Selecting Product ids with virtualization
            builder.Append("SELECT [Rowed_CTE].ID, [Rowed_CTE].RowNumber, [Rowed_CTE].HundredPercentMatch AS [HunderdPrecentMatch], [Rowed_CTE].Available ");
            builder.Append(", [Product].VendorCode, [Product].SearchName ");
            builder.Append("FROM [Rowed_CTE] ");
            builder.Append("INNER JOIN [Product] ON [Product].ID = [Rowed_CTE].ID ");
            builder.Append("WHERE [Rowed_CTE].RowNumber > @Offset ");
            builder.Append("AND [Rowed_CTE].RowNumber <= @Limit + @Offset ");
            builder.Append("ORDER BY [Rowed_CTE].RowNumber ");

            List<SearchResult> ids = getMultipleProductsRepository.GetAllProductIdsFromSql(builder.ToString(), props);
            List<SearchResult> idsSearchAnalogues = new();
            if (ids.Count.Equals(0)) {
                StringBuilder queryBuilder = new();

                queryBuilder.AppendLine(";WITH [SearchStage_Zero] AS ");
                queryBuilder.AppendLine("( ");
                queryBuilder.AppendLine("    SELECT ");
                queryBuilder.AppendLine("        [Product].ID ,");
                queryBuilder.AppendLine("        [Product].SearchName ,");
                queryBuilder.AppendLine("        [Product].SearchDescription ,");
                if (props.Culture.Equals("pl")) {
                    builder.Append(" [Product].SearchNamePL ,");
                    builder.Append(" [Product].SearchDescriptionPL ,");
                } else {
                    queryBuilder.Append(" [Product].SearchNameUA ,");
                    queryBuilder.Append(" [Product].SearchDescriptionUA ,");
                }

                queryBuilder.AppendLine("        [Product].SearchVendorCode ,");
                queryBuilder.AppendLine("        [Product].SearchSize ");
                queryBuilder.AppendLine("    FROM ");
                queryBuilder.AppendLine("        [Product] ");
                queryBuilder.AppendLine("    WHERE ");
                queryBuilder.AppendLine("        [Product].Deleted = 0 ");
                queryBuilder.AppendLine("), ");

                //��� �� ���� �� ���� ���� ��
                if (concreteValues.Length > 1) {
                    string lastProductsStageName = "[SearchStage_Zero] ";
                    string lastOriginalNumbersStageName = string.Empty;

                    for (int i = 0; i < concreteValues.Length; i++) {
                        string currentProductsStageName = $"[Search_Stage{i}] ";
                        string currentOriginalNumbersStageName = $"[OriginalNumbers_Stage{i}] ";

                        if (!i.Equals(concreteValues.Length - 1)) {
                            queryBuilder.Append(currentProductsStageName);
                            queryBuilder.Append("AS ( ");
                            queryBuilder.Append("SELECT [Product].ID ");
                            queryBuilder.Append(", [Product].SearchName ");
                            queryBuilder.Append(", [Product].SearchDescription ");

                            if (props.Culture.Equals("pl")) {
                                queryBuilder.Append(", [Product].SearchNamePL ");
                                queryBuilder.Append(", [Product].SearchDescriptionPL ");
                            } else {
                                queryBuilder.Append(", [Product].SearchNameUA ");
                                queryBuilder.Append(", [Product].SearchDescriptionUA ");
                            }

                            queryBuilder.Append(", [Product].SearchVendorCode ");
                            queryBuilder.Append(", [Product].SearchSize ");
                        } else {
                            queryBuilder.Append(currentProductsStageName);
                            queryBuilder.Append("AS ( ");
                            queryBuilder.Append("SELECT [Product].ID ");

                            queryBuilder.Append(props.Culture.Equals("pl") ? ", [Product].SearchNamePL AS [SearchName] " : ", [Product].SearchNameUA AS [SearchName] ");

                            queryBuilder.Append(", 0 AS [HundredPercentMatch] ");
                        }

                        queryBuilder.Append($"FROM {lastProductsStageName}AS [Product] ");
                        queryBuilder.Append("WHERE ");
                        queryBuilder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchName) > 0 ");
                        queryBuilder.Append("OR ");
                        queryBuilder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchDescription) > 0 ");
                        queryBuilder.Append("OR ");

                        if (props.Culture.Equals("pl")) {
                            queryBuilder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchNamePL) > 0 ");
                            queryBuilder.Append("OR ");
                            queryBuilder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchDescriptionPL) > 0 ");
                        } else {
                            queryBuilder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchNameUA) > 0 ");
                            queryBuilder.Append("OR ");
                            queryBuilder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchDescriptionUA) > 0 ");
                        }

                        queryBuilder.Append("OR ");
                        queryBuilder.Append($"PATINDEX('%' + @Var{i} + '%', [Product].SearchVendorCode) > 0 ");
                        queryBuilder.Append("), ");

                        if (string.IsNullOrEmpty(lastOriginalNumbersStageName)) {
                            queryBuilder.Append(currentOriginalNumbersStageName);
                            queryBuilder.Append("AS ( ");
                            queryBuilder.Append("SELECT [Product].ID ");

                            queryBuilder.Append(props.Culture.Equals("pl") ? ", [Product].SearchNamePL AS [SearchName] " : ", [Product].SearchNameUA AS [SearchName] ");

                            queryBuilder.Append(", ( ");
                            queryBuilder.Append("CASE ");
                            queryBuilder.Append($"WHEN [OriginalNumber].Number = @Var{i} ");
                            queryBuilder.Append("THEN 1 ");
                            queryBuilder.Append("ELSE 0 ");
                            queryBuilder.Append("END ");
                            queryBuilder.Append(") AS [HundredPercentMatch] ");
                            queryBuilder.Append(", [OriginalNumber].Number AS [Number] ");
                            queryBuilder.Append("FROM [OriginalNumber] ");
                            queryBuilder.Append("LEFT JOIN [ProductOriginalNumber] ");
                            queryBuilder.Append("ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID ");
                            queryBuilder.Append("AND [ProductOriginalNumber].Deleted = 0 ");
                            queryBuilder.Append("LEFT JOIN [Product] ");
                            queryBuilder.Append("ON [Product].ID = [ProductOriginalNumber].ProductID ");
                            queryBuilder.Append($"AND PATINDEX('%' + @Var{i} + '%', [OriginalNumber].Number) > 0 ");
                            queryBuilder.Append("), ");
                        } else if (!i.Equals(concreteValues.Length - 1)) {
                            queryBuilder.Append(currentOriginalNumbersStageName);
                            queryBuilder.Append("AS ( ");
                            queryBuilder.Append("SELECT [OriginalNumber].ID ");
                            queryBuilder.Append(", [OriginalNumber].[HundredPercentMatch] ");
                            queryBuilder.Append(", [OriginalNumber].[Number] ");
                            queryBuilder.Append(", [OriginalNumber].[SearchName] ");
                            queryBuilder.Append($"FROM {lastOriginalNumbersStageName} AS [OriginalNumber] ");
                            queryBuilder.Append($"WHERE PATINDEX('%' + @Var{i} + '%', [OriginalNumber].Number) > 0 ");
                            queryBuilder.Append("), ");
                        } else {
                            queryBuilder.Append(currentOriginalNumbersStageName);
                            queryBuilder.Append("AS ( ");
                            queryBuilder.Append("SELECT [OriginalNumber].ID ");
                            queryBuilder.Append(", [OriginalNumber].[SearchName] ");
                            queryBuilder.Append(", [OriginalNumber].[HundredPercentMatch] ");
                            queryBuilder.Append($"FROM {lastOriginalNumbersStageName} AS [OriginalNumber] ");
                            queryBuilder.Append($"WHERE PATINDEX('%' + @Var{i} + '%', [OriginalNumber].Number) > 0 ");
                            queryBuilder.Append("), ");
                        }

                        lastProductsStageName = currentProductsStageName;

                        lastOriginalNumbersStageName = currentOriginalNumbersStageName;
                    }

                    //Uniting results of search by Product data and by Original number
                    queryBuilder.Append("[United_CTE] ");
                    queryBuilder.Append("AS ( ");
                    queryBuilder.Append("SELECT [LastStageByProduct].ID ");
                    queryBuilder.Append(", [LastStageByProduct].SearchName ");
                    queryBuilder.Append(", [LastStageByProduct].HundredPercentMatch ");
                    queryBuilder.Append($"FROM {lastProductsStageName}AS [LastStageByProduct]");
                    queryBuilder.Append("UNION ");
                    queryBuilder.Append($"SELECT {lastOriginalNumbersStageName}.ID ");
                    queryBuilder.Append($", {lastOriginalNumbersStageName}.SearchName ");
                    queryBuilder.Append($", {lastOriginalNumbersStageName}.HundredPercentMatch ");
                    queryBuilder.Append($"FROM {lastOriginalNumbersStageName}");

                    queryBuilder.Append("), ");
                } else {
                    queryBuilder.AppendLine("[Search_FullValue] ");
                    queryBuilder.AppendLine(" AS ( ");
                    queryBuilder.AppendLine("    SELECT ");
                    queryBuilder.AppendLine("        [Product].ID,");

                    queryBuilder.Append(props.Culture.Equals("pl") ? " [Product].SearchNamePL AS [SearchName] , " : " [Product].SearchNameUA AS [SearchName], ");

                    queryBuilder.AppendLine("        CASE ");

                    queryBuilder.Append(props.Culture.Equals("pl")
                        ? "WHEN [Product].SearchNamePL = @Value OR [Product].SearchVendorCode = @Value "
                        : "WHEN [Product].SearchNameUA = @Value OR [Product].SearchVendorCode = @Value ");

                    queryBuilder.Append("THEN 1 ");
                    queryBuilder.AppendLine("            ELSE 0 ");
                    queryBuilder.AppendLine("        END AS [HundredPercentMatch] ");
                    queryBuilder.AppendLine("    FROM ");
                    queryBuilder.AppendLine("        [SearchStage_Zero] AS [Product] ");
                    queryBuilder.AppendLine("    WHERE ");
                    queryBuilder.AppendLine("        PATINDEX('%' + @Value + '%', [Product].SearchName) > 0 ");
                    queryBuilder.AppendLine("        OR PATINDEX('%' + @Value + '%', [Product].SearchDescription) > 0 ");

                    if (props.Culture.Equals("pl")) {
                        queryBuilder.Append("OR ");
                        queryBuilder.Append("PATINDEX('%' + @Value + '%', [Product].SearchNamePL) > 0 ");
                        queryBuilder.Append("OR ");
                        queryBuilder.Append("PATINDEX('%' + @Value + '%', [Product].SearchDescriptionPL) > 0 ");
                    } else {
                        queryBuilder.Append("OR ");
                        queryBuilder.Append("PATINDEX('%' + @Value + '%', [Product].SearchNameUA) > 0 ");
                        queryBuilder.Append("OR ");
                        queryBuilder.Append("PATINDEX('%' + @Value + '%', [Product].SearchDescriptionUA) > 0 ");
                    }

                    queryBuilder.AppendLine(" OR PATINDEX('%' + @Value + '%', [Product].SearchVendorCode) > 0 ");
                    queryBuilder.AppendLine("), ");
                    queryBuilder.AppendLine("[OriginalNumbers_FullValue] AS ");
                    queryBuilder.AppendLine("( ");
                    queryBuilder.AppendLine("    SELECT ");
                    queryBuilder.AppendLine("        [Product].ID,");


                    queryBuilder.Append(props.Culture.Equals("pl") ? "[Product].SearchNamePL AS [SearchName], " : "[Product].SearchNameUA AS [SearchName], ");

                    queryBuilder.AppendLine(" CASE ");
                    queryBuilder.AppendLine("            WHEN [OriginalNumber].Number = @Value THEN 1 ");
                    queryBuilder.AppendLine("            ELSE 0 ");
                    queryBuilder.AppendLine("        END AS [HundredPercentMatch] ");
                    queryBuilder.AppendLine("    FROM ");
                    queryBuilder.AppendLine("        [OriginalNumber] ");
                    queryBuilder.AppendLine("    LEFT JOIN ");
                    queryBuilder.AppendLine("        [ProductOriginalNumber] ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID ");
                    queryBuilder.AppendLine("            AND [ProductOriginalNumber].Deleted = 0 ");
                    queryBuilder.AppendLine("    LEFT JOIN ");
                    queryBuilder.AppendLine("        [Product] ON [Product].ID = [ProductOriginalNumber].ProductID ");
                    queryBuilder.AppendLine("    WHERE ");
                    queryBuilder.AppendLine("        [Product].Deleted = 0 ");
                    queryBuilder.AppendLine("        AND PATINDEX('%' + @Value + '%', [OriginalNumber].Number) > 0 ");
                    queryBuilder.AppendLine("), ");
                    queryBuilder.AppendLine("[United_CTE] AS ");
                    queryBuilder.AppendLine("( ");
                    queryBuilder.AppendLine("    SELECT ");
                    queryBuilder.AppendLine("        [Search_FullValue].ID, ");
                    queryBuilder.AppendLine("        [Search_FullValue].SearchName, ");
                    queryBuilder.AppendLine("        [Search_FullValue].HundredPercentMatch ");
                    queryBuilder.AppendLine("    FROM ");
                    queryBuilder.AppendLine("        [Search_FullValue] ");
                    queryBuilder.AppendLine("    UNION ");
                    queryBuilder.AppendLine("    SELECT ");
                    queryBuilder.AppendLine("        [OriginalNumbers_FullValue].ID, ");
                    queryBuilder.AppendLine("        [OriginalNumbers_FullValue].SearchName, ");
                    queryBuilder.AppendLine("        [OriginalNumbers_FullValue].HundredPercentMatch ");
                    queryBuilder.AppendLine("    FROM ");
                    queryBuilder.AppendLine("        [OriginalNumbers_FullValue] ");
                    queryBuilder.AppendLine("), ");
                }

                queryBuilder.AppendLine("[Rowed_CTE] AS ");
                queryBuilder.AppendLine("( ");
                queryBuilder.AppendLine("    SELECT ");
                queryBuilder.AppendLine("        [Product].ID,");
                queryBuilder.AppendLine("        MAX([Product].HundredPercentMatch) AS [HundredPercentMatch],");
                queryBuilder.AppendLine("        ROW_NUMBER() OVER(ORDER BY MAX([Product].HundredPercentMatch) DESC, [Product].SearchName) AS [RowNumber] ");
                queryBuilder.AppendLine("    FROM ");
                queryBuilder.AppendLine("        [United_CTE] AS [Product] ");
                queryBuilder.AppendLine("    GROUP BY ");
                queryBuilder.AppendLine("        [Product].ID, [Product].SearchName ");
                queryBuilder.AppendLine(") ");


                queryBuilder.AppendLine("SELECT ");
                queryBuilder.AppendLine("    [Rowed_CTE].ID, ");
                queryBuilder.AppendLine("    [Rowed_CTE].RowNumber, ");
                queryBuilder.AppendLine("    [Rowed_CTE].HundredPercentMatch AS [HunderdPrecentMatch] ");
                queryBuilder.AppendLine("FROM ");
                queryBuilder.AppendLine("    [Rowed_CTE] ");
                queryBuilder.AppendLine("WHERE ");
                queryBuilder.AppendLine("    [Rowed_CTE].RowNumber > @Offset ");
                queryBuilder.AppendLine("    AND [Rowed_CTE].RowNumber <= @Limit + @Offset ");
                queryBuilder.AppendLine("ORDER BY ");
                queryBuilder.AppendLine("    [Rowed_CTE].RowNumber");
                idsSearchAnalogues = getMultipleProductsRepository.GetAllProductIdsFromSql(queryBuilder.ToString(), props);
            }

            builder = new StringBuilder();

            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            if (!ids.Any() && !idsSearchAnalogues.Any()) return Task.FromResult(new List<FromSearchProduct>());
            if (idsSearchAnalogues.Any()) {
                if (currentClientNetId.Equals(Guid.Empty)) {
                    Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetWithHighestPriority();

                    ClientAgreement clientAgreement = clientAgreementRepository.GetByClientNetIdWithOrWithoutVat(
                        _clientRepositoriesFactory.NewClientRepository(connection).GetRetailClient().NetUid,
                        storage.OrganizationId.Value,
                        storage.ForVatProducts);

                    List<FromSearchProduct> analogues = new();

                    foreach (SearchResult item in idsSearchAnalogues)
                        if (!(analogues.Count >= limit))
                            analogues.AddRange(getMultipleProductsRepository
                                .GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
                                    item.Id,
                                    clientAgreement.NetUid,
                                    clientAgreement.Agreement.OrganizationId,
                                    clientAgreement.Agreement.CurrencyId,
                                    clientAgreement.Agreement.WithVATAccounting
                                ));
                        else break;

                    return Task.FromResult(analogues);
                } else {
                    ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId)
                                                      ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);
                    if (clientAgreement == null) {
                        clientAgreement = clientAgreementRepository.GetSelectedByClientNotSelectedNetId(currentClientNetId);
                        clientAgreement.Agreement.IsSelected = true;
                        agreementRepository.Update(clientAgreement.Agreement);
                    }

                    clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId) ??
                                      clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);
                    List<FromSearchProduct> analogues = new();

                    foreach (SearchResult item in idsSearchAnalogues)
                        if (!(analogues.Count >= limit))
                            analogues.AddRange(getMultipleProductsRepository
                                .GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
                                    item.Id,
                                    clientAgreement.NetUid,
                                    clientAgreement.Agreement.OrganizationId,
                                    clientAgreement.Agreement.CurrencyId,
                                    clientAgreement.Agreement.WithVATAccounting
                                ));
                        else break;

                    return Task.FromResult(analogues);
                }
            }

            builder.Append("CREATE TABLE dbo.#SearchResult(");
            builder.Append("ID bigint,");
            builder.Append("RowNumber bigint,");
            builder.Append("HundredPercentMatch bit,");
            builder.Append("Available bit");
            builder.Append("); ");
            builder.Append("INSERT INTO dbo.#SearchResult(ID, RowNumber, HundredPercentMatch, Available) VALUES ");

            for (int i = 0; i < ids.Count; i++)
                if (i.Equals(ids.Count - 1)) {
                    if (ids[i].HunderdPrecentMatch)
                        builder.Append(ids[i].Available ? $"({ids[i].Id}, {ids[i].RowNumber}, 1, 1); " : $"({ids[i].Id}, {ids[i].RowNumber}, 1, 0); ");
                    else
                        builder.Append(ids[i].Available ? $"({ids[i].Id}, {ids[i].RowNumber}, 0, 1); " : $"({ids[i].Id}, {ids[i].RowNumber}, 0, 0); ");
                } else {
                    if (ids[i].HunderdPrecentMatch)
                        builder.Append(ids[i].Available ? $"({ids[i].Id}, {ids[i].RowNumber}, 1, 1), " : $"({ids[i].Id}, {ids[i].RowNumber}, 1, 0), ");
                    else
                        builder.Append(ids[i].Available ? $"({ids[i].Id}, {ids[i].RowNumber}, 0, 1), " : $"({ids[i].Id}, {ids[i].RowNumber}, 0, 0), ");
                }

            if (currentClientNetId.Equals(Guid.Empty)) {
                Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetWithHighestPriority();

                ClientAgreement clientAgreement = clientAgreementRepository.GetByClientNetIdWithOrWithoutVat(
                    _clientRepositoriesFactory.NewClientRepository(connection).GetRetailClient().NetUid,
                    storage.OrganizationId.Value,
                    storage.ForVatProducts);

                if (clientAgreement == null) return Task.FromResult(new List<FromSearchProduct>());

                List<FromSearchProduct> analogues = new();
                List<FromSearchProduct> productSearch = getMultipleProductsRepository.GetAllFromIdsInTempTableForRetail(builder.ToString(), clientAgreement);
                //List<FromSearchProduct> productSearch = getMultipleProductsRepository.GetAllFromIdsInTempTableForRetail(ids.Select(x=>x.Id).ToList(), clientAgreement, storage);

                foreach (FromSearchProduct item in productSearch)
                    if (!(productSearch.Count >= limit) && item.AvailableQtyUk.Equals(0))
                        analogues.AddRange(getMultipleProductsRepository
                            .GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
                                item.Id,
                                clientAgreement.NetUid,
                                clientAgreement.Agreement.OrganizationId,
                                clientAgreement.Agreement.CurrencyId,
                                clientAgreement.Agreement.WithVATAccounting
                            ));
                    else break;

                foreach (FromSearchProduct item in analogues)
                    if (!(productSearch.Count >= limit))
                        productSearch.Add(item);
                    else break;

                return Task.FromResult(productSearch);
            } else {
                ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId)
                                                  ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);
                if (clientAgreement == null) {
                    clientAgreement = clientAgreementRepository.GetSelectedByClientNotSelectedNetId(currentClientNetId);
                    clientAgreement.Agreement.IsSelected = true;
                    agreementRepository.Update(clientAgreement.Agreement);
                }

                clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId) ??
                                  clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);

                List<FromSearchProduct> analogues = new();
                List<FromSearchProduct> productSearch = getMultipleProductsRepository.GetAllFromIdsInTempTable(builder.ToString(),
                    clientAgreement.NetUid,
                    clientAgreement.Agreement.CurrencyId,
                    clientAgreement.Agreement.OrganizationId,
                    clientAgreement.Agreement.WithVATAccounting,
                    clientAgreement.Agreement.IsDefault);

                foreach (FromSearchProduct item in productSearch)
                    if (!(productSearch.Count >= limit) && item.AvailableQtyUk.Equals(0))
                        analogues.AddRange(getMultipleProductsRepository
                            .GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
                                item.Id,
                                clientAgreement.NetUid,
                                clientAgreement.Agreement.OrganizationId,
                                clientAgreement.Agreement.CurrencyId,
                                clientAgreement.Agreement.WithVATAccounting
                            ));
                    else break;

                foreach (FromSearchProduct item in analogues)
                    if (!(productSearch.Count >= limit))
                        productSearch.Add(item);
                    else break;

                return Task.FromResult(productSearch);
            }
    }

    /// <summary>
    /// Optimized search V2 - Ukrainian only, no Polish branches, simplified scoring.
    /// </summary>
    public Task<List<FromSearchProduct>> GetAllFromSearchV2(string value, Guid currentClientNetId, long limit, long offset, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

            IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);
            IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            if (string.IsNullOrEmpty(value)) return Task.FromResult(new List<FromSearchProduct>());

            value = StringOptimizations.NormalizeForSearch(value);

            var optimizedSearch = new ProductSearchServiceOptimized();
            List<SearchResult> ids = optimizedSearch.GetSearchResults(connection, value, limit, offset);

            if (ids.Count == 0) return Task.FromResult(new List<FromSearchProduct>());

            // Build temp table query string (same format as original)
            StringBuilder builder = new();
            builder.Append("CREATE TABLE dbo.#SearchResult(");
            builder.Append("ID bigint,");
            builder.Append("RowNumber bigint,");
            builder.Append("HundredPercentMatch bit,");
            builder.Append("Available bit");
            builder.Append("); ");
            builder.Append("INSERT INTO dbo.#SearchResult(ID, RowNumber, HundredPercentMatch, Available) VALUES ");

            for (int i = 0; i < ids.Count; i++) {
                if (i == ids.Count - 1)
                    builder.Append(ids[i].Available ? $"({ids[i].Id}, {ids[i].RowNumber}, {(ids[i].HunderdPrecentMatch ? 1 : 0)}, 1); " : $"({ids[i].Id}, {ids[i].RowNumber}, {(ids[i].HunderdPrecentMatch ? 1 : 0)}, 0); ");
                else
                    builder.Append(ids[i].Available ? $"({ids[i].Id}, {ids[i].RowNumber}, {(ids[i].HunderdPrecentMatch ? 1 : 0)}, 1), " : $"({ids[i].Id}, {ids[i].RowNumber}, {(ids[i].HunderdPrecentMatch ? 1 : 0)}, 0), ");
            }

            if (currentClientNetId.Equals(Guid.Empty)) {
                Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetWithHighestPriority();

                ClientAgreement clientAgreement = clientAgreementRepository.GetByClientNetIdWithOrWithoutVat(
                    _clientRepositoriesFactory.NewClientRepository(connection).GetRetailClient().NetUid,
                    storage.OrganizationId.Value,
                    storage.ForVatProducts);

                if (clientAgreement == null) return Task.FromResult(new List<FromSearchProduct>());

                return Task.FromResult(getMultipleProductsRepository.GetAllFromIdsInTempTableForRetail(builder.ToString(), clientAgreement));
            } else {
                ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId)
                                                  ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);
                if (clientAgreement == null) {
                    clientAgreement = clientAgreementRepository.GetSelectedByClientNotSelectedNetId(currentClientNetId);
                    if (clientAgreement != null) {
                        clientAgreement.Agreement.IsSelected = true;
                        agreementRepository.Update(clientAgreement.Agreement);
                    }
                }

                clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId) ??
                                  clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);

                if (clientAgreement == null) return Task.FromResult(new List<FromSearchProduct>());

                return Task.FromResult(getMultipleProductsRepository.GetAllFromIdsInTempTable(builder.ToString(),
                    clientAgreement.NetUid,
                    clientAgreement.Agreement.CurrencyId,
                    clientAgreement.Agreement.OrganizationId,
                    clientAgreement.Agreement.WithVATAccounting,
                    clientAgreement.Agreement.IsDefault));
            }
    }

    public Task<List<FromSearchProduct>> GetAllAnaloguesByProductNetIdForRetail(Guid productNetId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(productNetId);

        if (product == null) return Task.FromResult(new List<FromSearchProduct>());

        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

        Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetWithHighestPriority();

        ClientAgreement clientAgreement = clientAgreementRepository.GetByClientNetIdWithOrWithoutVat(
            _clientRepositoriesFactory.NewClientRepository(connection).GetRetailClient().NetUid,
            storage.OrganizationId.Value,
            storage.ForVatProducts);

        List<FromSearchProduct> analogues = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection)
            .GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
                product.Id,
                clientAgreement.NetUid,
                clientAgreement.Agreement.OrganizationId,
                clientAgreement.Agreement.CurrencyId,
                clientAgreement.Agreement.WithVATAccounting
            );

        return Task.FromResult(analogues);
    }

    public Task<List<FromSearchProduct>> GetAllAnaloguesByProductNetId(Guid productNetId, Guid currentClientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProductRepository productRepository = _productRepositoriesFactory.NewProductRepository(connection);

        Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(productNetId);

        if (product == null) return Task.FromResult(new List<FromSearchProduct>());

        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

        ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId) ??
                                          clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);

        List<FromSearchProduct> analogues = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection)
            .GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPrices(
                product.Id,
                clientAgreement.NetUid,
                clientAgreement.Agreement.OrganizationId,
                clientAgreement.Agreement.CurrencyId,
                clientAgreement.Agreement.WithVATAccounting
            );

        return Task.FromResult(analogues);
    }

    public Task<List<FromSearchProduct>> GetAllComponentsByProductNetId(Guid productNetId, Guid currentClientNetId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);

        Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(productNetId);

        if (product == null) return Task.FromResult(new List<FromSearchProduct>());

        if (currentClientNetId.Equals(Guid.Empty))
            return Task.FromResult(getMultipleProductsRepository
                .GetAllComponentsByProductIdWithCalculatedPrices(
                    product.Id,
                    currentClientNetId,
                    null
                ));

        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

        ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(currentClientNetId, false);
        ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(currentClientNetId, true);

        return Task.FromResult(getMultipleProductsRepository
            .GetAllComponentsByProductIdWithCalculatedPrices(
                product.Id,
                nonVatAgreement?.NetUid ?? Guid.Empty,
                vatAgreement?.NetUid,
                withVat ? vatAgreement?.Agreement?.OrganizationId : nonVatAgreement?.Agreement?.OrganizationId
            ));
    }

    public Task<List<Product>> GetAllByVendorCodes(List<string> vendorCodes, Guid currentClientNetId, long limit, long offset, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (vendorCodes.Count.Equals(0)) return Task.FromResult(new List<Product>());
        if (limit.Equals(0)) limit = 20;
        if (offset < 0) offset = 0;

            IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);

            StringBuilder idsSqlBuilder = new();

            idsSqlBuilder.Append("SELECT [SearchProduct].ID ");
            idsSqlBuilder.Append("FROM [Product] AS [SearchProduct] ");
            idsSqlBuilder.Append("WHERE [SearchProduct].VendorCode IN (");

            for (int i = 0; i < vendorCodes.Count; i++) idsSqlBuilder.Append(i.Equals(0) ? $"N'{vendorCodes[i]}'" : $", N'{vendorCodes[i]}'");

            idsSqlBuilder.Append(")");

            if (currentClientNetId.Equals(Guid.Empty))
                return Task.FromResult(getMultipleProductsRepository.GetAllFromIdsInPreDefinedQuery(idsSqlBuilder.ToString(), Guid.Empty, Guid.Empty));

            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(currentClientNetId, false);
            ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(currentClientNetId, true);

            return Task.FromResult(getMultipleProductsRepository
                .GetAllFromIdsInPreDefinedQuery(
                    idsSqlBuilder.ToString(),
                    nonVatAgreement?.NetUid ?? Guid.Empty,
                    vatAgreement?.NetUid
                ));
    }

    public Task<List<OrderItem>> GetAllOrderedProductsFiltered(DateTime from, DateTime to, long limit, long offset, Guid clientNetId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ClientAgreement clientAgreement = _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetSelectedByClientNetId(clientNetId);

        return Task.FromResult(_productRepositoriesFactory
            .NewGetMultipleProductsRepository(connection)
            .GetAllOrderedProductsFiltered(
                from.Year.Equals(1) ? DateTime.UtcNow.Date : from,
                to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.AddHours(23).AddMinutes(59).AddSeconds(59),
                limit <= 0 ? 20 : limit,
                offset < 0 ? 0 : offset,
                clientNetId,
                clientAgreement?.NetUid ?? Guid.Empty,
                clientAgreement?.Agreement.CurrencyId,
                clientAgreement?.Agreement.OrganizationId,
                clientAgreement?.Agreement.WithVATAccounting ?? false
            ));
    }

    public Task<List<ProductHistoryModel>> GetAllOrderedProductsHistoryByClientNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(_productRepositoriesFactory.NewGetMultipleProductsRepository(connection).GetAllOrderedProductsHistory(netId));
    }

    /// <summary>
    /// Gets products by IDs with calculated prices.
    /// Preserves the order of input IDs (important for search ranking).
    /// </summary>
    public Task<List<FromSearchProduct>> GetAllByIds(List<long> productIds, Guid currentClientNetId, bool withVat) {
        // Use optimized version by default for search
        return GetAllByIdsOptimized(productIds, currentClientNetId, withVat);
    }

    /// <summary>
    /// OPTIMIZED: Gets products by IDs using set-based price calculation.
    /// ~50x faster than the UDF-based approach.
    /// </summary>
    public Task<List<FromSearchProduct>> GetAllByIdsOptimized(List<long> productIds, Guid currentClientNetId, bool withVat) {
        if (productIds == null || productIds.Count == 0)
            return Task.FromResult(new List<FromSearchProduct>());

        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
        var optimizedRepo = new OptimizedProductRepository(connection);

        List<FromSearchProduct> products;

        if (currentClientNetId.Equals(Guid.Empty)) {
            // Retail user
            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetWithHighestPriority();

            ClientAgreement clientAgreement = clientAgreementRepository.GetByClientNetIdWithOrWithoutVat(
                _clientRepositoriesFactory.NewClientRepository(connection).GetRetailClient().NetUid,
                storage.OrganizationId.Value,
                storage.ForVatProducts);

            if (clientAgreement == null) return Task.FromResult(new List<FromSearchProduct>());

            products = optimizedRepo.GetProductsByIdsForRetail(
                productIds,
                clientAgreement.NetUid,
                culture,
                clientAgreement.Agreement.OrganizationId ?? 0,
                clientAgreement.Agreement.CurrencyId ?? 0,
                clientAgreement.Agreement.PricingId ?? 0);
        } else {
            // Logged-in user
            ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId)
                                              ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);
            if (clientAgreement == null) {
                clientAgreement = clientAgreementRepository.GetSelectedByClientNotSelectedNetId(currentClientNetId);
                if (clientAgreement != null) {
                    var agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);
                    clientAgreement.Agreement.IsSelected = true;
                    agreementRepository.Update(clientAgreement.Agreement);
                }
            }

            clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId) ??
                              clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);

            if (clientAgreement == null) return Task.FromResult(new List<FromSearchProduct>());

            products = optimizedRepo.GetProductsByIdsWithPrices(
                productIds,
                clientAgreement.NetUid,
                culture,
                withVat,
                clientAgreement.Agreement.OrganizationId);
        }

        // Fetch original numbers for all products
        if (products.Count > 0) {
            var fetchedIds = products.Select(p => p.Id).ToList();
            var originalNumbers = GetOriginalNumbersForProducts(connection, fetchedIds);

            foreach (var product in products) {
                if (originalNumbers.TryGetValue(product.Id, out var numbers)) {
                    product.OriginalNumbers = numbers;
                }
            }
        }

        return Task.FromResult(products);
    }

    /// <summary>
    /// Gets only calculated prices for products (lightweight query for V3 search).
    /// Product data comes from Typesense, this only calculates client-specific prices.
    /// </summary>
    public Dictionary<long, ProductPriceInfo> GetPricesOnly(List<long> productIds, Guid currentClientNetId, bool withVat, string culture = "uk") {
        if (productIds == null || productIds.Count == 0)
            return new Dictionary<long, ProductPriceInfo>();

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
        var optimizedRepo = new OptimizedProductRepository(connection);

        if (currentClientNetId.Equals(Guid.Empty)) {
            // Retail user - match V1 behavior: use storage.ForVatProducts for agreement selection
            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetWithHighestPriority();

            ClientAgreement clientAgreement = clientAgreementRepository.GetByClientNetIdWithOrWithoutVat(
                _clientRepositoriesFactory.NewClientRepository(connection).GetRetailClient().NetUid,
                storage.OrganizationId.Value,
                storage.ForVatProducts);

            if (clientAgreement == null) return new Dictionary<long, ProductPriceInfo>();

            return optimizedRepo.GetPricesOnlyForRetail(
                productIds,
                clientAgreement.NetUid,
                clientAgreement.Agreement.CurrencyId ?? 0,
                clientAgreement.Agreement.WithVATAccounting,
                culture);
        } else {
            // Logged-in user
            ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId)
                                              ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);
            if (clientAgreement == null) {
                clientAgreement = clientAgreementRepository.GetSelectedByClientNotSelectedNetId(currentClientNetId);
                if (clientAgreement != null) {
                    var agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);
                    clientAgreement.Agreement.IsSelected = true;
                    agreementRepository.Update(clientAgreement.Agreement);
                }
            }

            clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId) ??
                              clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);

            if (clientAgreement == null) return new Dictionary<long, ProductPriceInfo>();

            return optimizedRepo.GetPricesOnly(
                productIds,
                clientAgreement.NetUid,
                clientAgreement.Agreement.OrganizationId,
                clientAgreement.Agreement.WithVATAccounting,
                culture);
        }
    }

    /// <summary>
    /// LEGACY: Gets products by IDs with calculated prices using UDF-based approach.
    /// Slower but more feature-complete. Use GetAllByIdsOptimized for search.
    /// </summary>
    public Task<List<FromSearchProduct>> GetAllByIdsLegacy(List<long> productIds, Guid currentClientNetId, bool withVat) {
        if (productIds == null || productIds.Count == 0)
            return Task.FromResult(new List<FromSearchProduct>());

        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);
        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
        IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);

        // Build temp table with IDs preserving order via RowNumber
        StringBuilder builder = new();
        builder.Append("CREATE TABLE dbo.#SearchResult(");
        builder.Append("ID bigint,");
        builder.Append("RowNumber bigint,");
        builder.Append("HundredPercentMatch bit,");
        builder.Append("Available bit");
        builder.Append("); ");
        builder.Append("INSERT INTO dbo.#SearchResult(ID, RowNumber, HundredPercentMatch, Available) VALUES ");

        for (int i = 0; i < productIds.Count; i++) {
            if (i == productIds.Count - 1)
                builder.Append($"({productIds[i]}, {i + 1}, 0, 0); ");
            else
                builder.Append($"({productIds[i]}, {i + 1}, 0, 0), ");
        }

        List<FromSearchProduct> products;

        if (currentClientNetId.Equals(Guid.Empty)) {
            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetWithHighestPriority();

            ClientAgreement clientAgreement = clientAgreementRepository.GetByClientNetIdWithOrWithoutVat(
                _clientRepositoriesFactory.NewClientRepository(connection).GetRetailClient().NetUid,
                storage.OrganizationId.Value,
                storage.ForVatProducts);

            if (clientAgreement == null) return Task.FromResult(new List<FromSearchProduct>());

            products = getMultipleProductsRepository.GetAllFromIdsInTempTableForRetail(builder.ToString(), clientAgreement);
        } else {
            ClientAgreement clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId)
                                              ?? clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);
            if (clientAgreement == null) {
                clientAgreement = clientAgreementRepository.GetSelectedByClientNotSelectedNetId(currentClientNetId);
                if (clientAgreement != null) {
                    clientAgreement.Agreement.IsSelected = true;
                    agreementRepository.Update(clientAgreement.Agreement);
                }
            }

            clientAgreement = clientAgreementRepository.GetSelectedByClientNetId(currentClientNetId) ??
                              clientAgreementRepository.GetSelectedByWorkplaceNetId(currentClientNetId);

            if (clientAgreement == null) return Task.FromResult(new List<FromSearchProduct>());

            products = getMultipleProductsRepository.GetAllFromIdsInTempTable(builder.ToString(),
                clientAgreement.NetUid,
                clientAgreement.Agreement.CurrencyId,
                clientAgreement.Agreement.OrganizationId,
                clientAgreement.Agreement.WithVATAccounting,
                clientAgreement.Agreement.IsDefault);
        }

        // Fetch original numbers for all products
        if (products.Count > 0) {
            var fetchedIds = products.Select(p => p.Id).ToList();
            var originalNumbers = GetOriginalNumbersForProducts(connection, fetchedIds);

            foreach (var product in products) {
                if (originalNumbers.TryGetValue(product.Id, out var numbers)) {
                    product.OriginalNumbers = numbers;
                }
            }
        }

        return Task.FromResult(products);
    }

    /// <summary>
    /// Gets original numbers for a list of product IDs.
    /// </summary>
    private Dictionary<long, List<string>> GetOriginalNumbersForProducts(IDbConnection connection, List<long> productIds) {
        const string sql = @"
SELECT pon.ProductID, onum.Number
FROM ProductOriginalNumber pon
INNER JOIN OriginalNumber onum ON onum.ID = pon.OriginalNumberID
WHERE pon.Deleted = 0 AND pon.ProductID IN @ProductIds
ORDER BY pon.ProductID, pon.IsMainOriginalNumber DESC";

        var result = new Dictionary<long, List<string>>();

        // Batch to avoid 2100 parameter limit
        const int batchSize = 2000;
        for (int i = 0; i < productIds.Count; i += batchSize) {
            var batch = productIds.Skip(i).Take(batchSize).ToList();
            var rows = connection.Query<(long ProductId, string Number)>(sql, new { ProductIds = batch });

            foreach (var (productId, number) in rows) {
                if (!result.TryGetValue(productId, out var list)) {
                    list = new List<string>();
                    result[productId] = list;
                }
                if (!string.IsNullOrWhiteSpace(number)) {
                    list.Add(number);
                }
            }
        }

        return result;
    }
}
