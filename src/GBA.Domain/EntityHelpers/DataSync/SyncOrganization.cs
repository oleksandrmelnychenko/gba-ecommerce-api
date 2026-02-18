using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncOrganization {
    public string OrganizationCode { get; set; }

    public string OrganizationName { get; set; }

    public string Manager { get; set; }

    public string OrganizationPrefix { get; set; }

    public string OrganizationFullName { get; set; }

    public bool IsIndividual { get; set; }

    public string MainBankAccountName { get; set; }

    public string MainCurrencyCode { get; set; }

    public string EDRPOU { get; set; }

    public string IPN { get; set; }

    public DateTime? DateRegistration { get; set; }

    public string NumberRegistration { get; set; }

    public string TaxInspectionName { get; set; }

    public string NumberCertification { get; set; }

    public string CodeKVED { get; set; }

    public string StorageName { get; set; }

    public string PFURegistrationNumber { get; set; }
}