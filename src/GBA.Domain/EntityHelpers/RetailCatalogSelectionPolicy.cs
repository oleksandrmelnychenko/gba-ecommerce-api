#nullable enable

using System;
using System.Data;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.EntityHelpers;

public interface IRetailCatalogSelectionProvider {
    RetailCatalogSelection? Resolve(IDbConnection connection, bool withVat);
}

public sealed class SqlRetailCatalogSelectionProvider : IRetailCatalogSelectionProvider {
    public RetailCatalogSelection? Resolve(IDbConnection connection, bool withVat) {
        return RetailCatalogSelectionPolicy.Resolve(connection, withVat);
    }
}

/// <summary>
/// The single SQL policy used to resolve anonymous retail storage and pricing contexts.
/// Request-time price and availability hydration must use this exact selector.
/// </summary>
public static class RetailCatalogSelectionPolicy {
    public const string SqlCtes = @"
RetailStorageCandidates AS (
    SELECT
        s.ID AS StorageId,
        s.OrganizationID,
        s.ForVatProducts,
        o.PriceSourceIsAmg AS IsAmgWorld,
        s.Updated AS StorageUpdated,
        ROW_NUMBER() OVER (
            PARTITION BY s.ForVatProducts
            ORDER BY s.RetailPriority, s.ID
        ) AS RowNumber
    FROM Storage s
    INNER JOIN Organization o ON o.ID = s.OrganizationID
    WHERE s.Deleted = 0
      AND s.ForEcommerce = 1
      AND s.ForDefective = 0
      AND o.Deleted = 0
      AND EXISTS (
          SELECT 1
          FROM Client retailClient
          INNER JOIN ClientAgreement retailClientAgreement
              ON retailClientAgreement.ClientID = retailClient.ID
             AND retailClientAgreement.Deleted = 0
          INNER JOIN Agreement retailAgreement
              ON retailAgreement.ID = retailClientAgreement.AgreementID
             AND retailAgreement.Deleted = 0
             AND retailAgreement.IsActive = 1
          INNER JOIN Pricing retailPricing
              ON retailPricing.ID = retailAgreement.PricingID
             AND retailPricing.Deleted = 0
          INNER JOIN Currency retailCurrency
              ON retailCurrency.ID = retailAgreement.CurrencyID
             AND retailCurrency.Deleted = 0
          WHERE retailClient.Deleted = 0
            AND retailClient.IsActive = 1
            AND retailClient.IsForRetail = 1
            AND retailAgreement.OrganizationID = s.OrganizationID
            AND retailAgreement.WithVATAccounting = s.ForVatProducts
            -- The agreement must be marked for the SAME pricing world as its organization:
            -- Fenix orgs carry SourceFenix*, AMG orgs carry SourceAmg*, never both.
            AND (
                (o.PriceSourceIsAmg = 0
                 AND (ISNULL(DATALENGTH(retailAgreement.SourceFenixID), 0) > 0
                   OR retailAgreement.SourceFenixCode IS NOT NULL)
                 AND ISNULL(DATALENGTH(retailAgreement.SourceAmgID), 0) = 0
                 AND retailAgreement.SourceAmgCode IS NULL)
             OR (o.PriceSourceIsAmg = 1
                 AND (ISNULL(DATALENGTH(retailAgreement.SourceAmgID), 0) > 0
                   OR retailAgreement.SourceAmgCode IS NOT NULL)
                 AND ISNULL(DATALENGTH(retailAgreement.SourceFenixID), 0) = 0
                 AND retailAgreement.SourceFenixCode IS NULL)
            )
      )
),
RetailStorage AS (
    SELECT StorageId, OrganizationID, ForVatProducts, IsAmgWorld, StorageUpdated
    FROM RetailStorageCandidates
    WHERE RowNumber = 1
),
RetailEcommerceStorages AS (
    SELECT
        s.ID,
        s.Locale,
        selected.ForVatProducts AS CatalogWithVat,
        selected.OrganizationID AS CatalogOrganizationId
    FROM Storage s
    INNER JOIN RetailStorage selected
        ON selected.OrganizationID = s.OrganizationID
       AND selected.ForVatProducts = s.ForVatProducts
    WHERE s.Deleted = 0
      AND s.ForEcommerce = 1
      AND s.ForDefective = 0
),
RetailProductAvailability AS (
    SELECT
        pa.ProductID,
        s.CatalogWithVat,
        s.CatalogOrganizationId,
        SUM(CASE WHEN s.Locale = 'uk' THEN pa.Amount ELSE 0 END) AS AvailableQtyUk,
        SUM(CASE WHEN s.Locale = 'pl' THEN pa.Amount ELSE 0 END) AS AvailableQtyPl,
        SUM(pa.Amount) AS AvailableQty
    FROM ProductAvailability pa
    INNER JOIN RetailEcommerceStorages s ON s.ID = pa.StorageID
    WHERE pa.Deleted = 0
    GROUP BY pa.ProductID, s.CatalogWithVat, s.CatalogOrganizationId
),
RetailAgreementPricing AS (
    SELECT
        a.WithVATAccounting,
        a.OrganizationID AS CatalogOrganizationId,
        a.PricingID AS CatalogPricingId,
        a.CurrencyID AS CatalogCurrencyId,
        a.SourceFenixID,
        a.SourceFenixCode,
        a.SourceAmgID,
        a.SourceAmgCode,
        selectedStorage.IsAmgWorld,
        a.Updated AS AgreementUpdated,
        ca.Updated AS ClientAgreementUpdated,
        CASE
            WHEN selectedStorage.IsAmgWorld = 1 THEN
                CASE
                    WHEN DATALENGTH(a.SourceAmgID) > 0
                        THEN CONCAT(
                            'amg:id-', CONVERT(varchar(128), a.SourceAmgID, 2),
                            CASE WHEN a.SourceAmgCode IS NOT NULL
                                THEN CONCAT('|code-', CONVERT(varchar(20), a.SourceAmgCode))
                                ELSE '' END)
                    ELSE CONCAT('amg:code-', CONVERT(varchar(20), a.SourceAmgCode))
                END
            ELSE
                CASE
                    WHEN DATALENGTH(a.SourceFenixID) > 0
                        THEN CONCAT(
                            'fenix:id-', CONVERT(varchar(128), a.SourceFenixID, 2),
                            CASE WHEN a.SourceFenixCode IS NOT NULL
                                THEN CONCAT('|code-', CONVERT(varchar(20), a.SourceFenixCode))
                                ELSE '' END)
                    ELSE CONCAT('fenix:code-', CONVERT(varchar(20), a.SourceFenixCode))
                END
        END AS CatalogAgreementSource,
        currency.Code AS CatalogCurrencyCode,
        pr.NetUID AS PricingNetUid,
        ca.NetUID AS ClientAgreementNetUid,
        ROW_NUMBER() OVER (
            PARTITION BY a.WithVATAccounting
            ORDER BY CASE WHEN a.IsSelected = 1 THEN 0 ELSE 1 END,
                     a.Updated DESC,
                     a.ID,
                     ca.ID
        ) AS RowNumber
    FROM Client c
    INNER JOIN ClientAgreement ca ON ca.ClientID = c.ID
    INNER JOIN Agreement a ON a.ID = ca.AgreementID
    INNER JOIN Pricing pr ON pr.ID = a.PricingID
    INNER JOIN Currency currency ON currency.ID = a.CurrencyID
    INNER JOIN RetailStorage selectedStorage
        ON selectedStorage.OrganizationID = a.OrganizationID
       AND selectedStorage.ForVatProducts = a.WithVATAccounting
    WHERE c.IsForRetail = 1
      AND c.IsActive = 1
      AND c.Deleted = 0
      AND ca.Deleted = 0
      AND a.IsActive = 1
      AND a.Deleted = 0
      AND (
            (selectedStorage.IsAmgWorld = 0
             AND (ISNULL(DATALENGTH(a.SourceFenixID), 0) > 0 OR a.SourceFenixCode IS NOT NULL)
             AND ISNULL(DATALENGTH(a.SourceAmgID), 0) = 0
             AND a.SourceAmgCode IS NULL)
         OR (selectedStorage.IsAmgWorld = 1
             AND (ISNULL(DATALENGTH(a.SourceAmgID), 0) > 0 OR a.SourceAmgCode IS NOT NULL)
             AND ISNULL(DATALENGTH(a.SourceFenixID), 0) = 0
             AND a.SourceFenixCode IS NULL)
      )
      AND pr.Deleted = 0
      AND currency.Deleted = 0
),
RetailPricingConfig AS (
    SELECT
        nonVat.CatalogOrganizationId AS CatalogOrganizationIdNonVat,
        vat.CatalogOrganizationId AS CatalogOrganizationIdVat,
        nonVat.CatalogAgreementSource AS CatalogAgreementSourceNonVat,
        vat.CatalogAgreementSource AS CatalogAgreementSourceVat,
        nonVat.ClientAgreementNetUid AS CatalogAgreementNetUidNonVat,
        vat.ClientAgreementNetUid AS CatalogAgreementNetUidVat,
        nonVat.CatalogPricingId AS CatalogPricingIdNonVat,
        vat.CatalogPricingId AS CatalogPricingIdVat,
        nonVat.CatalogCurrencyId AS CatalogCurrencyIdNonVat,
        vat.CatalogCurrencyId AS CatalogCurrencyIdVat,
        nonVat.CatalogCurrencyCode AS CatalogCurrencyCodeNonVat,
        vat.CatalogCurrencyCode AS CatalogCurrencyCodeVat,
        nonVat.PricingNetUid AS NonVatPricingNetUid,
        nonVat.ClientAgreementNetUid AS NonVatAgreementNetUid,
        vat.PricingNetUid AS VatPricingNetUid,
        vat.ClientAgreementNetUid AS VatAgreementNetUid
    FROM RetailAgreementPricing nonVat
    CROSS JOIN RetailAgreementPricing vat
    WHERE nonVat.WithVATAccounting = 0
      AND nonVat.RowNumber = 1
      AND vat.WithVATAccounting = 1
      AND vat.RowNumber = 1
)";

    public static RetailCatalogSelection? Resolve(IDbConnection connection, bool withVat) {
        ArgumentNullException.ThrowIfNull(connection);

        string sql = ";WITH\n" + SqlCtes + @"
SELECT TOP (1)
    storage.StorageId,
    storage.OrganizationID AS OrganizationId,
    storage.ForVatProducts AS WithVat,
    storage.StorageUpdated,
    agreement.ClientAgreementNetUid,
    agreement.CatalogPricingId AS PricingId,
    agreement.CatalogCurrencyId AS CurrencyId,
    agreement.SourceFenixID,
    agreement.SourceFenixCode,
    agreement.SourceAmgID,
    agreement.SourceAmgCode,
    agreement.CatalogAgreementSource,
    agreement.ClientAgreementUpdated,
    agreement.AgreementUpdated
FROM RetailStorage storage
INNER JOIN RetailAgreementPricing agreement
    ON agreement.CatalogOrganizationId = storage.OrganizationID
   AND agreement.WithVATAccounting = storage.ForVatProducts
   AND agreement.RowNumber = 1
WHERE storage.ForVatProducts = @WithVat";

        RetailCatalogSelection? selection = connection.QuerySingleOrDefault<RetailCatalogSelection>(
            sql,
            new { WithVat = withVat });
        return selection?.IsValid == true ? selection : null;
    }
}

public sealed class RetailCatalogSelection {
    public long StorageId { get; set; }
    public long OrganizationId { get; set; }
    public bool WithVat { get; set; }
    public DateTime StorageUpdated { get; set; }
    public Guid ClientAgreementNetUid { get; set; }
    public long PricingId { get; set; }
    public long CurrencyId { get; set; }
    public byte[]? SourceFenixId { get; set; }
    public long? SourceFenixCode { get; set; }
    public byte[]? SourceAmgId { get; set; }
    public long? SourceAmgCode { get; set; }
    public string CatalogAgreementSource { get; set; } = string.Empty;
    public DateTime ClientAgreementUpdated { get; set; }
    public DateTime AgreementUpdated { get; set; }

    public string SourceWorld => ExternalSourceIdentity.TryCreate(
        SourceFenixId,
        SourceFenixCode,
        SourceAmgId,
        SourceAmgCode,
        out ExternalSourceIdentity? source)
        ? source!.System
        : string.Empty;

    public bool IsValid => StorageId > 0
                           && OrganizationId > 0
                           && ClientAgreementNetUid != Guid.Empty
                           && PricingId > 0
                           && CurrencyId > 0
                           && !string.IsNullOrWhiteSpace(CatalogAgreementSource)
                           && ExternalSourceIdentity.TryCreate(
                               SourceFenixId,
                               SourceFenixCode,
                               SourceAmgId,
                               SourceAmgCode,
                               out ExternalSourceIdentity? source)
                           && string.Equals(source!.Value, CatalogAgreementSource, StringComparison.Ordinal);

    public ClientAgreement ToClientAgreement() {
        return new ClientAgreement {
            NetUid = ClientAgreementNetUid,
            Updated = ClientAgreementUpdated,
            Agreement = new Agreement {
                IsActive = true,
                WithVATAccounting = WithVat,
                OrganizationId = OrganizationId,
                Organization = new Organization {
                    Id = OrganizationId,
                    PriceSourceIsAmg = string.Equals(
                        SourceWorld,
                        ProductSourceIdentitySql.Amg,
                        StringComparison.Ordinal)
                },
                PricingId = PricingId,
                CurrencyId = CurrencyId,
                SourceFenixId = SourceFenixId,
                SourceFenixCode = SourceFenixCode,
                SourceAmgId = SourceAmgId,
                SourceAmgCode = SourceAmgCode,
                Updated = AgreementUpdated
            }
        };
    }
}
