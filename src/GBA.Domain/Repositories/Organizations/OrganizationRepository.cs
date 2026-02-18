using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Organizations;

public sealed class OrganizationRepository : IOrganizationRepository {
    private readonly IDbConnection _connection;

    public OrganizationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Organization organization) {
        return _connection.Query<long>(
            "INSERT INTO [Organization] " +
            "(Name, Code, Culture, FullName, TIN, USREOU, SROI, RegistrationNumber, PFURegistrationNumber, PhoneNumber, Address, RegistrationDate, PFURegistrationDate, " +
            "IsIndividual, CurrencyId, StorageId, TaxInspectionId, Updated, [Manager], [TypeTaxation], [VatRateID], [IsVatAgreements]) " +
            "VALUES " +
            "(@Name, @Code, @Culture, @FullName, @TIN, @USREOU, @SROI, @RegistrationNumber, @PFURegistrationNumber, @PhoneNumber, @Address, @RegistrationDate, " +
            "@PFURegistrationDate, @IsIndividual, @CurrencyId, @StorageId, @TaxInspectionId, getutcdate(), @Manager, @TypeTaxation, @VatRateId, @IsVatAgreements); " +
            "SELECT SCOPE_IDENTITY()",
            organization
        ).Single();
    }

    public void Update(Organization organization) {
        _connection.Execute(
            "UPDATE [Organization] SET " +
            "Name = @Name, Code = @Code, Culture = @Culture, FullName = @FullName, TIN = @TIN, USREOU = @USREOU, SROI = @SROI, RegistrationNumber = @RegistrationNumber, " +
            "PFURegistrationNumber = @PFURegistrationNumber, PhoneNumber = @PhoneNumber, Address = @Address, RegistrationDate = @RegistrationDate, " +
            "TaxInspectionId = @TaxInspectionId, PFURegistrationDate = @PFURegistrationDate, IsIndividual = @IsIndividual, CurrencyId = @CurrencyId, StorageId = @StorageId, " +
            "Updated = GETUTCDATE(), [Manager] = @Manager, [TypeTaxation] = @TypeTaxation, [VatRateID] = @VatRateId, [IsVatAgreements] = @IsVatAgreements " +
            "WHERE NetUID = @NetUid",
            organization
        );
    }

    public Organization GetOrganizationByCurrentCultureIfExists() {
        return _connection.Query<Organization>(
            "SELECT TOP(1) * " +
            "FROM [Organization] " +
            "WHERE [Organization].Deleted = 0 " +
            "AND [Organization].Culture = @Culture",
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).FirstOrDefault();
    }

    public Organization GetByOrganizationCultureIfExists(string culture) {
        return _connection.Query<Organization>(
            "SELECT TOP(1) * " +
            "FROM [Organization] " +
            "WHERE [Organization].Deleted = 0 " +
            "AND [Organization].Culture = @Culture",
            new { Culture = culture }
        ).SingleOrDefault();
    }

    public Organization GetCorrectOrganization() {
        return _connection.Query<Organization>(
            "SELECT * FROM Organization " +
            "WHERE [Name] LIKE N'[Фф][EЕeе][HНhн][IІiі][KКkк][CСсc]' " +
            "AND [Deleted] = 0; " +
            "--( ͡° ͜ʖ ͡°)"
        ).SingleOrDefault();
    }

    public Organization GetById(long id) {
        Organization organizationToReturn = null;

        _connection.Query<Organization, OrganizationTranslation, OrganizationTranslation, Currency, Storage, TaxInspection, VatRate, Organization>(
            "SELECT * " +
            "FROM [Organization] " +
            "LEFT JOIN [OrganizationTranslation] AS [CurrentTranslation] " +
            "ON [Organization].ID = [CurrentTranslation].OrganizationID " +
            "AND [CurrentTranslation].CultureCode = @Culture " +
            "AND [CurrentTranslation].Deleted = 0 " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [Organization].ID = [OrganizationTranslation].OrganizationID " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Organization].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Organization].StorageID " +
            "LEFT JOIN [TaxInspection] " +
            "ON [TaxInspection].ID = [Organization].[TaxInspectionID] " +
            "LEFT JOIN [VatRate] " +
            "ON [VatRate].[ID] = [Organization].[VatRateID] " +
            "WHERE [Organization].ID = @Id",
            (organization, currentTranslation, translation, currency, storage, taxInspection, vatRate) => {
                if (currentTranslation != null) organization.Name = currentTranslation.Name;

                if (organizationToReturn != null) {
                    if (!organizationToReturn.OrganizationTranslations.Any(t => t.Id.Equals(translation.Id))) organizationToReturn.OrganizationTranslations.Add(translation);
                } else {
                    if (!organization.OrganizationTranslations.Any(t => t.Id.Equals(translation.Id))) organization.OrganizationTranslations.Add(translation);

                    organization.Currency = currency;
                    organization.Storage = storage;
                    organization.TaxInspection = taxInspection;
                    organization.VatRate = vatRate;

                    organizationToReturn = organization;
                }

                return organization;
            },
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (!organizationToReturn.OrganizationTranslations.Any()) return organizationToReturn;

        organizationToReturn.OrganizationTranslations =
            CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")
                ? organizationToReturn.OrganizationTranslations.OrderBy(t => t.CultureCode).ToArray()
                : organizationToReturn.OrganizationTranslations.OrderByDescending(t => t.CultureCode).ToArray();

        return organizationToReturn;
    }

    public Organization GetByNetId(Guid netId) {
        Organization organizationToReturn = null;

        _connection.Query<Organization, OrganizationTranslation, OrganizationTranslation, Currency, Storage, TaxInspection, Organization>(
            "SELECT * " +
            "FROM [Organization] " +
            "LEFT JOIN [OrganizationTranslation] AS [CurrentTranslation] " +
            "ON [Organization].ID = [CurrentTranslation].OrganizationID " +
            "AND [CurrentTranslation].CultureCode = @Culture " +
            "AND [CurrentTranslation].Deleted = 0 " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [Organization].ID = [OrganizationTranslation].OrganizationID " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Organization].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Organization].StorageID " +
            "LEFT JOIN [TaxInspection] " +
            "ON [TaxInspection].ID = [Organization].TaxInspectionID " +
            "WHERE [Organization].NetUID = @NetId",
            (organization, currentTranslation, translation, currency, storage, taxInspection) => {
                if (currentTranslation != null) organization.Name = currentTranslation.Name;

                if (organizationToReturn != null) {
                    if (!organizationToReturn.OrganizationTranslations.Any(t => t.Id.Equals(translation.Id))) organizationToReturn.OrganizationTranslations.Add(translation);
                } else {
                    if (!organization.OrganizationTranslations.Any(t => t.Id.Equals(translation.Id))) organization.OrganizationTranslations.Add(translation);

                    organization.Currency = currency;
                    organization.Storage = storage;
                    organization.TaxInspection = taxInspection;

                    organizationToReturn = organization;
                }

                return organization;
            },
            new { NetId = netId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (!organizationToReturn.OrganizationTranslations.Any()) return organizationToReturn;

        organizationToReturn.OrganizationTranslations =
            CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")
                ? organizationToReturn.OrganizationTranslations.OrderBy(t => t.CultureCode).ToArray()
                : organizationToReturn.OrganizationTranslations.OrderByDescending(t => t.CultureCode).ToArray();

        return organizationToReturn;
    }

    public List<Organization> GetAll() {
        List<Organization> organizations = new();

        Type[] types = {
            typeof(Organization),
            typeof(OrganizationTranslation),
            typeof(OrganizationTranslation),
            typeof(Currency),
            typeof(Storage),
            typeof(TaxInspection),
            typeof(PaymentRegister),
            typeof(VatRate)
        };

        Func<object[], Organization> mapper = objects => {
            Organization organization = (Organization)objects[0];
            OrganizationTranslation currentTranslation = (OrganizationTranslation)objects[1];
            OrganizationTranslation translation = (OrganizationTranslation)objects[2];
            Currency currency = (Currency)objects[3];
            Storage storage = (Storage)objects[4];
            TaxInspection taxInspection = (TaxInspection)objects[5];
            PaymentRegister paymentRegister = (PaymentRegister)objects[6];
            VatRate vatRate = (VatRate)objects[7];

            if (currentTranslation != null) organization.Name = currentTranslation.Name;

            if (organizations.Any(o => o.Id.Equals(organization.Id))) {
                organization = organizations.First(o => o.Id.Equals(organization.Id));

                if (paymentRegister != null && paymentRegister.IsMain)
                    organization.MainPaymentRegister = paymentRegister;

                if (!organization.OrganizationTranslations.Any(t => t.Id.Equals(translation.Id)))
                    organization.OrganizationTranslations.Add(translation);

                if (paymentRegister != null && !organization.PaymentRegisters.Any(x => x.Id.Equals(paymentRegister.Id)))
                    organization.PaymentRegisters.Add(paymentRegister);
            } else {
                if (!organization.OrganizationTranslations.Any(t => t.Id.Equals(translation.Id)))
                    organization.OrganizationTranslations.Add(translation);

                if (paymentRegister != null && !organization.PaymentRegisters.Any(x => x.Id.Equals(paymentRegister.Id)))
                    organization.PaymentRegisters.Add(paymentRegister);

                if (paymentRegister != null && paymentRegister.IsMain)
                    organization.MainPaymentRegister = paymentRegister;

                organization.Currency = currency;
                organization.Storage = storage;
                organization.TaxInspection = taxInspection;
                organization.VatRate = vatRate;

                organizations.Add(organization);
            }

            return organization;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [Organization] " +
            "LEFT JOIN [OrganizationTranslation] AS [CurrentTranslation] " +
            "ON [Organization].ID = [CurrentTranslation].OrganizationID " +
            "AND [CurrentTranslation].CultureCode = @Culture " +
            "AND [CurrentTranslation].Deleted = 0 " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [Organization].ID = [OrganizationTranslation].OrganizationID " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Organization].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Organization].StorageID " +
            "LEFT JOIN [TaxInspection] " +
            "ON [TaxInspection].ID = [Organization].TaxInspectionID " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].[OrganizationID] = [Organization].[ID] " +
            "AND [PaymentRegister].[Deleted] = 0 " +
            "AND [PaymentRegister].[Type] = 2 " +
            "LEFT JOIN [VatRate] " +
            "ON [VatRate].[ID] = [Organization].[VatRateID] " +
            "WHERE [Organization].Deleted = 0",
            types, mapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName });

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) return organizations.OrderByDescending(o => o.Id).ToList();

        if (!organizations.Any(o => o.OrganizationTranslations.Any())) return organizations;

        foreach (Organization organization in organizations)
            organization.OrganizationTranslations =
                CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")
                    ? organization.OrganizationTranslations.OrderBy(t => t.CultureCode).ToArray()
                    : organization.OrganizationTranslations.OrderByDescending(t => t.CultureCode).ToArray();

        return organizations;
    }

    public bool IsAssignedToAnyAgreement(long organizationId) {
        return _connection.Query<long>(
            "SELECT Organization.ID FROM Organization " +
            "LEFT JOIN Agreement " +
            "ON Agreement.OrganizationID = Organization.ID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "WHERE Organization.ID = @Id " +
            "AND Organization.Deleted = 0 " +
            "AND Agreement.Deleted = 0 " +
            "AND ClientAgreement.Deleted = 0",
            new {
                Id = organizationId
            }
        ).ToArray().Any();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Organization SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new {
                NetId = netId.ToString()
            }
        );
    }

    public long GetOrganizationIdByNetId(Guid organizationNetId) {
        return _connection.Query<long>(
            "SELECT [Organization].[ID] " +
            "FROM [Organization] " +
            "WHERE [Organization].[NetUID] = @netId",
            new {
                netId = organizationNetId
            }
        ).SingleOrDefault();
    }

    public string GetCultureByNetId(Guid organizationNetId) {
        return _connection.Query<string>(
            "SELECT [Organization].[Culture] FROM [Organization] " +
            "WHERE [Organization].[NetUID] = @NetId; ",
            new {
                NetId = organizationNetId
            }).FirstOrDefault();
    }
}