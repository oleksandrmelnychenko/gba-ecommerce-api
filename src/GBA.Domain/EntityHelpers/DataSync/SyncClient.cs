using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncClient {
    public SyncClient() {
        SyncClientAddresses = new HashSet<SyncClientAddress>();
    }

    public byte[] SourceId { get; set; }

    public long ClientCode { get; set; }

    public string ClientName { get; set; }

    public bool IsIndividual { get; set; }

    public bool Buyer { get; set; }

    public bool Supplier { get; set; }

    public bool IsNotResident { get; set; }

    public string TIN { get; set; }

    public string FullName { get; set; }

    public string USREOU { get; set; }

    public string SROI { get; set; }

    public string RegionCode { get; set; }

    public string RegionName { get; set; }

    public string BankName { get; set; }

    public string BankAccountCode { get; set; }

    public string BankAccountNumber { get; set; }

    public string LastName { get; set; }

    public string FirstName { get; set; }

    public string MiddleName { get; set; }

    public long MainClientCode { get; set; }

    public string MainClientName { get; set; }

    public string MainContactPersonCode { get; set; }

    public string MainContactPersonName { get; set; }

    public string MainContactPersonPosition { get; set; }

    public string ActivityCode { get; set; }

    public string ActivityName { get; set; }

    public long MainRecipientCode { get; set; }

    public string MainRecipientName { get; set; }

    public string IdentityDocument { get; set; }

    public int SupplierDeadline { get; set; }

    public string Description { get; set; }

    public int QuantityDayDebt { get; set; }

    public bool IsControlDayDebt { get; set; }

    public string ClientGroupName { get; set; }

    public string ManagerName { get; set; }

    public bool IsSubClient => !ClientCode.Equals(MainClientCode);

    public ICollection<SyncClientAddress> SyncClientAddresses { get; set; }
}