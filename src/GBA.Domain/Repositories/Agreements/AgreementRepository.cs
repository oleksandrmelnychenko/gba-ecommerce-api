using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Repositories.Agreements.Contracts;

namespace GBA.Domain.Repositories.Agreements;

public sealed class AgreementRepository : IAgreementRepository {
    private readonly IDbConnection _connection;

    public AgreementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Agreement agreement) {
        return _connection.Query<long>(
                "INSERT INTO Agreement (Name, IsManagementAccounting, IsAccounting, WithVATAccounting, IsControlAmountDebt, IsControlNumberDaysDebt, IsActive, AmountDebt, " +
                "NumberDaysDebt, CurrencyId, OrganizationId, PricingId, ProviderPricingId, DeferredPayment, TermsOfPayment, PrePaymentPercentages, IsPrePaymentFull, IsPrePayment, " +
                "IsDefault, Number, Updated, TaxAccountingSchemeID, [FromDate], [ToDate], [PromotionalPricingID], [IsSelected], [ForReSale], [WithAgreementLine]) " +
                "VALUES (@Name, @IsManagementAccounting, @IsAccounting, @WithVATAccounting, @IsControlAmountDebt, @IsControlNumberDaysDebt, @IsActive, @AmountDebt, " +
                "@NumberDaysDebt, @CurrencyId, @OrganizationId, @PricingId, @ProviderPricingId, @DeferredPayment, @TermsOfPayment, @PrePaymentPercentages, @IsPrePaymentFull, " +
                "@IsPrePayment, @IsDefault, @Number, getutcdate(), @TaxAccountingSchemeId, @FromDate, @ToDate, @PromotionalPricingId, @IsSelected, @ForReSale, @WithAgreementLine); " +
                "SELECT SCOPE_IDENTITY() ",
                agreement
            )
            .Single();
    }

    public void Update(Agreement agreement) {
        _connection.Execute(
            "UPDATE Agreement SET " +
            "Name = @Name, IsManagementAccounting = @IsManagementAccounting, IsAccounting = @IsAccounting, WithVATAccounting = @WithVATAccounting, " +
            "IsControlAmountDebt = @IsControlAmountDebt, IsControlNumberDaysDebt = @IsControlNumberDaysDebt, IsActive = @IsActive, AmountDebt = @AmountDebt, " +
            "NumberDaysDebt = @NumberDaysDebt, CurrencyId = @CurrencyId, OrganizationId = @OrganizationId, PricingId = @PricingId, " +
            "ProviderPricingId = @ProviderPricingId, DeferredPayment = @DeferredPayment, TermsOfPayment = @TermsOfPayment, " +
            "PrePaymentPercentages = @PrePaymentPercentages, IsPrePaymentFull = @IsPrePaymentFull, IsPrePayment = @IsPrePayment, IsDefault = @IsDefault, " +
            "Updated = getutcdate(), TaxAccountingSchemeID = @TaxAccountingSchemeId, Number = @Number, FromDate = @FromDate, ToDate = @ToDate, " +
            "[PromotionalPricingID] = @PromotionalPricingId, IsSelected = @IsSelected, [ForReSale] = @ForReSale, [WithAgreementLine] = @WithAgreementLine " +
            "WHERE NetUID = @NetUid ",
            agreement
        );
    }

    public Agreement GetById(long id) {
        return _connection.Query<Agreement>(
                "SELECT * FROM Agreement " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public Agreement GetLastRecord() {
        return _connection.Query<Agreement>(
                "SELECT TOP(1) * " +
                "FROM [Agreement] " +
                "WHERE [Number] IS NOT NULL " +
                "ORDER BY ID DESC"
            )
            .SingleOrDefault();
    }

    public Agreement GetAgreementByClientAgreementId(long id) {
        return _connection.Query<Agreement, Organization, Agreement>(
                "SELECT Agreement.*, Organization.* FROM Agreement " +
                "LEFT JOIN ClientAgreement " +
                "ON ClientAgreement.AgreementID = Agreement.ID " +
                "LEFT JOIN Organization " +
                "ON Organization.ID = Agreement.OrganizationID " +
                "WHERE ClientAgreement.ID = @Id ",
                (agreement, organization) => {
                    agreement.Organization = organization;
                    return agreement;
                },
                new { Id = id })
            .SingleOrDefault();
    }

    public Agreement GetLastRecordByOrganizationId(long organizationId) {
        return _connection.Query<Agreement>(
                "SELECT TOP(1) * " +
                "FROM [Agreement] " +
                "WHERE [Number] IS NOT NULL " +
                "AND OrganizationID = @OrganizationId " +
                "ORDER BY [Agreement].Created DESC",
                new { OrganizationId = organizationId }
            )
            .SingleOrDefault();
    }

    public Agreement GetLastRecordByOrganizationCode(string organizationCode) {
        return _connection.Query<Agreement>(
                "SELECT TOP 1 * FROM [Agreement] " +
                "WHERE [Agreement].[Number] LIKE '%' + @Code + '%' " +
                "ORDER BY [Agreement].[Created] DESC ",
                new { Code = organizationCode }
            )
            .SingleOrDefault();
    }

    public Agreement GetByNetId(Guid netId) {
        return _connection.Query<Agreement>(
                "SELECT * FROM Agreement " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<Agreement> GetAll() {
        return _connection.Query<Agreement>(
                "SELECT * FROM Agreement " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public List<Agreement> GetAllByIds(List<long> ids) {
        return _connection.Query<Agreement, Currency, Organization, Pricing, ProviderPricing, Agreement>(
                "SELECT * FROM Agreement " +
                "LEFT JOIN Currency " +
                "ON Agreement.CurrencyID = Currency.ID " +
                "LEFT JOIN Organization " +
                "ON Agreement.OrganizationID = Organization.ID " +
                "LEFT JOIN Pricing " +
                "ON Agreement.PricingID = Pricing.ID " +
                "LEFT JOIN ProviderPricing " +
                "ON Agreement.ProviderPricingID = ProviderPricing.ID " +
                "WHERE Agreement.ID IN @Ids",
                (agreement, currency, organization, pricing, providerPricing) => {
                    agreement.Currency = currency;
                    agreement.Organization = organization;
                    agreement.Pricing = pricing;
                    agreement.ProviderPricing = providerPricing;

                    return agreement;
                },
                new { Ids = ids }
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Agreement SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }

    public Agreement GetDefaultByCulture() {
        return _connection.Query<Agreement>(
                "SELECT TOP(1) * FROM [Agreement] " +
                "LEFT OUTER JOIN [Organization] " +
                "ON [Organization].ID = [Agreement].OrganizationID " +
                "WHERE [Agreement].IsDefault = 1 " +
                "AND [Organization].Culture = @Culture",
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public List<TaxAccountingScheme> GetAllTaxAccountingScheme() {
        return _connection.Query<TaxAccountingScheme>(
            "SELECT * FROM [TaxAccountingScheme]" +
            "WHERE [TaxAccountingScheme].[Deleted] = 0").ToList();
    }

    public List<AgreementTypeCivilCode> GetAllAgreementTypeCivilCodeMessage() {
        return _connection.Query<AgreementTypeCivilCode>(
            "SELECT * FROM [AgreementTypeCivilCode] " +
            "WHERE [AgreementTypeCivilCode].[Deleted] = 0 ").ToList();
    }
}