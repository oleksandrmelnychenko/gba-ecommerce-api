using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Currencies;

public sealed class CurrencyRepository : ICurrencyRepository {
    private readonly IDbConnection _connection;

    public CurrencyRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Currency currency) {
        return _connection.Query<long>(
                "INSERT INTO Currency (Name, Code, Updated) " +
                "VALUES (@Name, @Code, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                currency
            )
            .Single();
    }

    public void Update(Currency currency) {
        _connection.Execute(
            "UPDATE Currency SET " +
            "Name = @Name, Code = @Code, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            currency
        );
    }

    public Currency GetById(long id) {
        Currency currencyToReturn = null;

        _connection.Query<Currency, CurrencyTranslation, CurrencyTranslation, Currency>(
            "SELECT * FROM Currency " +
            "LEFT JOIN CurrencyTranslation AS CurrentTranslation " +
            "ON Currency.ID = CurrentTranslation.CurrencyID " +
            "AND CurrentTranslation.CultureCode = @Culture " +
            "AND CurrentTranslation.Deleted = 0 " +
            "LEFT JOIN CurrencyTranslation " +
            "ON Currency.ID = CurrencyTranslation.CurrencyID " +
            "AND CurrencyTranslation.Deleted = 0 " +
            "WHERE Currency.ID = @Id",
            (currency, currentTranslation, translation) => {
                if (currentTranslation != null) currency.Name = currentTranslation.Name;

                if (currencyToReturn != null) {
                    if (translation != null && !currencyToReturn.CurrencyTranslations.Any(t => t.Id.Equals(translation.Id))) currencyToReturn.CurrencyTranslations.Add(translation);
                } else {
                    if (translation != null && !currency.CurrencyTranslations.Any(t => t.Id.Equals(translation.Id))) currency.CurrencyTranslations.Add(translation);

                    currencyToReturn = currency;
                }

                return currency;
            },
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (currencyToReturn.CurrencyTranslations.Any()) {
            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl"))
                currencyToReturn.CurrencyTranslations = currencyToReturn.CurrencyTranslations.OrderBy(c => c.CultureCode).ToArray();
            else
                currencyToReturn.CurrencyTranslations = currencyToReturn.CurrencyTranslations.OrderByDescending(c => c.CultureCode).ToArray();
        }

        return currencyToReturn;
    }

    public Currency GetByNetId(Guid netId) {
        Currency currencyToReturn = null;

        _connection.Query<Currency, CurrencyTranslation, CurrencyTranslation, Currency>(
            "SELECT * FROM Currency " +
            "LEFT JOIN CurrencyTranslation AS CurrentTranslation " +
            "ON Currency.ID = CurrentTranslation.CurrencyID " +
            "AND CurrentTranslation.CultureCode = @Culture " +
            "AND CurrentTranslation.Deleted = 0 " +
            "LEFT JOIN CurrencyTranslation " +
            "ON Currency.ID = CurrencyTranslation.CurrencyID " +
            "AND CurrencyTranslation.Deleted = 0 " +
            "WHERE Currency.NetUID = @NetId",
            (currency, currentTranslation, translation) => {
                if (currentTranslation != null) currency.Name = currentTranslation.Name;

                if (currencyToReturn != null) {
                    if (translation != null && !currencyToReturn.CurrencyTranslations.Any(t => t.Id.Equals(translation.Id))) currencyToReturn.CurrencyTranslations.Add(translation);
                } else {
                    if (translation != null && !currency.CurrencyTranslations.Any(t => t.Id.Equals(translation.Id))) currency.CurrencyTranslations.Add(translation);

                    currencyToReturn = currency;
                }

                return currency;
            },
            new { NetId = netId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (currencyToReturn == null)
            return null;

        if (currencyToReturn.CurrencyTranslations.Any()) {
            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl"))
                currencyToReturn.CurrencyTranslations = currencyToReturn.CurrencyTranslations.OrderBy(c => c.CultureCode).ToArray();
            else
                currencyToReturn.CurrencyTranslations = currencyToReturn.CurrencyTranslations.OrderByDescending(c => c.CultureCode).ToArray();
        }

        return currencyToReturn;
    }

    public List<Currency> GetAll() {
        List<Currency> currencies = new();

        _connection.Query<Currency, CurrencyTranslation, CurrencyTranslation, Currency>(
            "SELECT * FROM Currency " +
            "LEFT JOIN CurrencyTranslation AS CurrentTranslation " +
            "ON Currency.ID = CurrentTranslation.CurrencyID " +
            "AND CurrentTranslation.CultureCode = @Culture " +
            "AND CurrentTranslation.Deleted = 0 " +
            "LEFT JOIN CurrencyTranslation " +
            "ON Currency.ID = CurrencyTranslation.CurrencyID " +
            "AND CurrencyTranslation.Deleted = 0 " +
            "WHERE Currency.Deleted = 0",
            (currency, currentTranslation, translation) => {
                if (currentTranslation != null) currency.Name = currentTranslation.Name;

                if (currencies.Any(c => c.Id.Equals(currency.Id))) {
                    if (translation != null && !currencies.First(c => c.Id.Equals(currency.Id)).CurrencyTranslations.Any(t => t.Id.Equals(translation.Id)))
                        currencies.First(c => c.Id.Equals(currency.Id)).CurrencyTranslations.Add(translation);
                } else {
                    if (translation != null && !currency.CurrencyTranslations.Any(t => t.Id.Equals(translation.Id))) currency.CurrencyTranslations.Add(translation);

                    currencies.Add(currency);
                }

                return currency;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (currencies.Any(c => c.CurrencyTranslations.Any()))
            foreach (Currency currency in currencies)
                if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl"))
                    currency.CurrencyTranslations = currency.CurrencyTranslations.OrderBy(c => c.CultureCode).ToArray();
                else
                    currency.CurrencyTranslations = currency.CurrencyTranslations.OrderByDescending(c => c.CultureCode).ToArray();

        return currencies;
    }

    public Currency GetEURCurrencyIfExists() {
        return _connection.Query<Currency>(
                "SELECT TOP(1) * " +
                "FROM [Currency] " +
                "WHERE [Currency].Deleted = 0 " +
                "AND [Currency].Code = 'eur'"
            )
            .SingleOrDefault();
    }

    public Currency GetPLNCurrencyIfExists() {
        return _connection.Query<Currency>(
                "SELECT TOP(1) * " +
                "FROM [Currency] " +
                "WHERE [Currency].Deleted = 0 " +
                "AND [Currency].Code = 'pln'"
            )
            .SingleOrDefault();
    }

    public Currency GetUAHCurrencyIfExists() {
        return _connection.Query<Currency>(
                "SELECT TOP(1) * " +
                "FROM [Currency] " +
                "WHERE [Currency].Deleted = 0 " +
                "AND [Currency].Code = 'uah'"
            )
            .SingleOrDefault();
    }

    public bool IsCurrencyAttachedToAnyPricing(long currencyId) {
        return _connection.Query<long>(
            "SELECT DISTINCT Currency.ID FROM Currency " +
            "LEFT JOIN Pricing " +
            "ON Pricing.CurrencyID = Currency.ID " +
            "WHERE Currency.ID = @Id " +
            "AND Currency.Deleted = 0 " +
            "AND Pricing.Deleted = 0",
            new { Id = currencyId }
        ).ToArray().Any();
    }

    public bool IsCurrencyAttachedToAnyAgreement(long currencyId) {
        return _connection.Query<long>(
            "SELECT DISTINCT Currency.ID FROM Currency " +
            "LEFT JOIN Agreement " +
            "ON Agreement.CurrencyID = Currency.ID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "WHERE Currency.ID = @Id " +
            "AND Currency.Deleted = 0 " +
            "AND ClientAgreement.Deleted = 0 " +
            "AND Agreement.Deleted = 0 ",
            new { Id = currencyId }
        ).ToArray().Any();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Currency SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }

    public Currency GetBase() {
        return _connection.Query<Currency>(
                "SELECT * FROM Currency WHERE ID = @EurCurrencyId",
                new { EurCurrencyId = 2 }
            )
            .SingleOrDefault();
    }

    public Currency GetByContainerServiceId(long id) {
        return _connection.Query<Currency>(
            "SELECT [Currency].* FROM [ContainerService] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [ContainerService].[SupplyOrganizationAgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "WHERE [ContainerService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }

    public Currency GetByVehicleServiceId(long id) {
        return _connection.Query<Currency>(
            "SELECT [Currency].* FROM [VehicleService] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [VehicleService].[SupplyOrganizationAgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "WHERE [VehicleService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }

    public Currency GetByMergedServiceId(long id) {
        return _connection.Query<Currency>(
            "SELECT [Currency].* " +
            "FROM [MergedService] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [MergedService].[SupplyOrganizationAgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "WHERE [MergedService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }

    public Currency GetByBillOfLadingServiceId(long id) {
        return _connection.Query<Currency>(
            "SELECT [Currency].* " +
            "FROM [BillOfLadingService] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [BillOfLadingService].[SupplyOrganizationAgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [SupplyOrganizationAgreement].[CurrencyID] " +
            "WHERE [BillOfLadingService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }

    public Currency GetUSDCurrencyIfExists() {
        return _connection.Query<Currency>(
                "SELECT TOP(1) * " +
                "FROM [Currency] " +
                "WHERE [Currency].Deleted = 0 " +
                "AND [Currency].Code = 'usd'"
            )
            .SingleOrDefault();
    }

    public Currency GetByOneCCode(string code) {
        return _connection.Query<Currency>(
            "SELECT * FROM [Currency] " +
            "WHERE [Currency].CodeOneC = @Code " +
            "AND [Currency].Deleted = 0 ",
            new { Code = code }).FirstOrDefault();
    }
}