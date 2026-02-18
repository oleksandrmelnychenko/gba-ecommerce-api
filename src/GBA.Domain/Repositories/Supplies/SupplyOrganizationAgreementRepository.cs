using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyOrganizationAgreementRepository : ISupplyOrganizationAgreementRepository {
    private readonly IDbConnection _connection;

    public SupplyOrganizationAgreementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public SupplyOrganizationAgreement GetById(long id) {
        SupplyOrganizationAgreement toReturn = null;

        _connection.Query<SupplyOrganizationAgreement, Currency, SupplyOrganization, SupplyOrganizationDocument, Organization, SupplyOrganizationAgreement>(
            "SELECT * " +
            "FROM [SupplyOrganizationAgreement] " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [SupplyOrganizationAgreement].SupplyOrganizationID " +
            "LEFT JOIN [SupplyOrganizationDocument] " +
            "ON [SupplyOrganizationDocument].SupplyOrganizationAgreementID = [SupplyOrganizationAgreement].ID " +
            "AND [SupplyOrganizationDocument].Deleted = 0 " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [SupplyOrganizationAgreement].ID = @Id",
            (agreement, currency, supplyOrganization, document, organization) => {
                if (toReturn != null) {
                    toReturn.SupplyOrganizationDocuments.Add(document);
                } else {
                    if (document != null)
                        agreement.SupplyOrganizationDocuments.Add(document);

                    agreement.Organization = organization;

                    agreement.Currency = currency;
                    agreement.SupplyOrganization = supplyOrganization;

                    toReturn = agreement;
                }

                return agreement;
            },
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }


    public SupplyOrganizationAgreement GetByNetId(Guid netId) {
        SupplyOrganizationAgreement toReturn = null;

        _connection.Query<SupplyOrganizationAgreement, Currency, SupplyOrganization, SupplyOrganizationDocument, Organization, SupplyOrganizationAgreement>(
            "SELECT * " +
            "FROM [SupplyOrganizationAgreement] " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [SupplyOrganizationAgreement].SupplyOrganizationID " +
            "LEFT JOIN [SupplyOrganizationDocument] " +
            "ON [SupplyOrganizationDocument].SupplyOrganizationAgreementID = [SupplyOrganizationAgreement].ID " +
            "AND [SupplyOrganizationDocument].Deleted = 0 " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [SupplyOrganizationAgreement].NetUID = @NetId",
            (agreement, currency, supplyOrganization, document, organization) => {
                if (toReturn != null) {
                    toReturn.SupplyOrganizationDocuments.Add(document);
                } else {
                    if (document != null)
                        agreement.SupplyOrganizationDocuments.Add(document);

                    agreement.Organization = organization;

                    agreement.Currency = currency;
                    agreement.SupplyOrganization = supplyOrganization;

                    toReturn = agreement;
                }

                return agreement;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public List<SupplyOrganizationAgreement> GetAllBySupplyOrganizationId(long id) {
        return _connection.Query<SupplyOrganizationAgreement, Currency, SupplyOrganization, Organization, SupplyOrganizationAgreement>(
            "SELECT * " +
            "FROM [SupplyOrganizationAgreement] " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [SupplyOrganizationAgreement].SupplyOrganizationID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [SupplyOrganizationAgreement].SupplyOrganizationID = @Id",
            (agreement, currency, supplyOrganization, organization) => {
                agreement.Organization = organization;

                agreement.Currency = currency;
                agreement.SupplyOrganization = supplyOrganization;

                return agreement;
            },
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).ToList();
    }


    public void Add(IEnumerable<SupplyOrganizationAgreement> agreements) {
        _connection.Execute(
            "INSERT INTO [SupplyOrganizationAgreement] (Name, CurrentAmount, SupplyOrganizationId, CurrencyId, Updated, [ExistTo], [OrganizationID], [ExistFrom]) " +
            "VALUES (@Name, @CurrentAmount, @SupplyOrganizationId, @CurrencyId, GETUTCDATE(), @ExistTo, @OrganizationId, @ExistFrom)",
            agreements
        );
    }

    public long Add(SupplyOrganizationAgreement agreement) {
        return _connection.Query<long>(
            "INSERT INTO [SupplyOrganizationAgreement] (Name, CurrentAmount, SupplyOrganizationId, CurrencyId, Updated, [ExistTo], [OrganizationID], [ExistFrom]) " +
            "VALUES (@Name, @CurrentAmount, @SupplyOrganizationId, @CurrencyId, GETUTCDATE(), @ExistTo, @OrganizationId, @ExistFrom); " +
            "SELECT SCOPE_IDENTITY() ",
            agreement
        ).Single();
    }

    public void Update(IEnumerable<SupplyOrganizationAgreement> agreements) {
        _connection.Execute(
            "UPDATE [SupplyOrganizationAgreement] " +
            "SET [Name] = @Name " +
            ", Updated = GETUTCDATE() " +
            ", [SourceFenixID] = @SourceFenixID " +
            ", [SourceFenixCode] = @SourceFenixCode " +
            ", [SourceAmgID] = @SourceAmgID " +
            ", [SourceAmgCode] = @SourceAmgCode " +
            ", [ExistTo] = @ExistTo " +
            ", [OrganizationID] = @OrganizationId " +
            ", [ExistFrom] = @ExistFrom " +
            ", [CurrencyID] = @CurrencyId " +
            "WHERE [SupplyOrganizationAgreement].ID = @Id",
            agreements
        );
    }

    public void Update(SupplyOrganizationAgreement agreement) {
        _connection.Execute(
            "UPDATE [SupplyOrganizationAgreement] " +
            "SET [Name] = @Name" +
            ", Updated = GETUTCDATE() " +
            ", [SourceFenixID] = @SourceFenixID " +
            ", [SourceFenixCode] = @SourceFenixCode " +
            ", [SourceAmgID] = @SourceAmgID " +
            ", [SourceAmgCode] = @SourceAmgCode " +
            ", [ExistTo] = @ExistTo " +
            ", [OrganizationID] = @OrganizationId " +
            ", [ExistFrom] = @ExistFrom " +
            ", [CurrencyID] = @CurrencyId " +
            "WHERE [SupplyOrganizationAgreement].ID = @Id",
            agreement
        );
    }

    public void UpdateCurrentAmount(SupplyOrganizationAgreement agreement) {
        _connection.Execute(
            "UPDATE [SupplyOrganizationAgreement] " +
            "SET CurrentAmount = @CurrentAmount, AccountingCurrentAmount = @AccountingCurrentAmount, Updated = GETUTCDATE() " +
            "WHERE [SupplyOrganizationAgreement].ID = @Id",
            agreement
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyOrganizationAgreement] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [SupplyOrganizationAgreement].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllBySupplyOrganizationId(long id) {
        _connection.Execute(
            "UPDATE [SupplyOrganizationAgreement] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [SupplyOrganizationAgreement].SupplyOrganizationID = @Id",
            new { Id = id }
        );
    }
}