namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncClientAddress {
    public long ActualClientCode { get; set; }

    public SyncClientAddressType AddressType { get; set; }

    public SyncClientAddressInfoType AddressInfoType { get; set; }

    public string Value { get; set; }
}