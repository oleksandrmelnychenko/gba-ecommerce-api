using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Regions;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels;

public class ClientDto {
    public Guid NetUid { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }
    public string TIN { get; set; }
    public string USREOU { get; set; }
    public string SROI { get; set; }
    public bool VatPayer { get; set; } // HAS SROI 
    public Guid GroupNetUid { get; set; }
    public string GroupName { get; set; }
}

public class ExtendedClientDto : ClientDto {
    public List<Agreement> Agreements { get; set; }

    public string FirstName { get; set; }

    public string MiddleName { get; set; }

    public string LastName { get; set; }

    public string SupplierName { get; set; }

    public string SupplierContactName { get; set; }

    public string SupplierCode { get; set; }

    public string Manufacturer { get; set; }

    public string Brand { get; set; }

    public string Comment { get; set; }

    public string MobileNumber { get; set; }

    public string ClientNumber { get; set; }

    public string SMSNumber { get; set; }

    public string FaxNumber { get; set; }

    public string AccountantNumber { get; set; }

    public string DirectorNumber { get; set; }

    public string ICQ { get; set; }

    public string EmailAddress { get; set; }

    public string DeliveryAddress { get; set; }

    public string LegalAddress { get; set; }

    public string ActualAddress { get; set; }

    public string IncotermsElse { get; set; }

    public string Street { get; set; }

    public string ZipCode { get; set; }

    public string HouseNumber { get; set; }

    public string RefId { get; set; }

    public int ClearCartAfterDays { get; set; }

    public long? SourceAmgCode { get; set; }

    public long? SourceFenixCode { get; set; }

    public byte[] SourceAmgId { get; set; }

    public byte[] SourceFenixId { get; set; }

    public long? RegionId { get; set; }

    public long? RegionCodeId { get; set; }

    public long? CountryId { get; set; }

    public long? ClientBankDetailsId { get; set; }

    public long? TermsOfDeliveryId { get; set; }

    public long? PackingMarkingId { get; set; }

    public long? PackingMarkingPaymentId { get; set; }

    public long? MainManagerId { get; set; }

    /// <summary>
    /// Abbreviation of first and last name, two characters.
    /// </summary>
    public string Abbreviation { get; set; }

    /// <summary>
    /// Determine either person individual or entity.
    /// </summary>
    public bool IsIndividual { get; set; }

    public bool IsSupplyOrganization { get; set; }

    public bool IsActive { get; set; }

    public bool IsSubClient { get; set; }

    public bool IsTradePoint { get; set; }

    public bool IsPayForDelivery { get; set; }

    public bool IsBlocked { get; set; }

    public bool IsIncotermsElse { get; set; }

    public bool IsTemporaryClient { get; set; }

    public bool IsFromECommerce { get; set; }

    public int NameDistance { get; set; }

    public string Manager { get; set; }

    public string OriginalRegionCode { get; set; }

    public bool IsNotResident { get; set; }

    public long? MainClientId { get; set; }

    public Region Region { get; set; }

    public RegionCode RegionCode { get; set; }

    // SupplyOrganization Fields
    public string Requisites { get; set; }

    public string Swift { get; set; }

    public string SwiftBic { get; set; }

    public string IntermediaryBank { get; set; }

    public string BeneficiaryBank { get; set; }

    public string AccountNumber { get; set; }

    public string Beneficiary { get; set; }

    public string Bank { get; set; }

    public string BankAccount { get; set; }

    public string NIP { get; set; }

    public string BankAccountPLN { get; set; }

    public string BankAccountEUR { get; set; }

    public string ContactPersonName { get; set; }

    public string ContactPersonPhone { get; set; }

    public string ContactPersonEmail { get; set; }

    public string ContactPersonViber { get; set; }

    public string ContactPersonSkype { get; set; }

    public string ContactPersonComment { get; set; }

    public bool IsAgreementReceived { get; set; }

    public bool IsBillReceived { get; set; }

    public DateTime? AgreementReceiveDate { get; set; }

    public DateTime? BillReceiveDate { get; set; }
}