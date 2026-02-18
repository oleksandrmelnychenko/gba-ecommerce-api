namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncOrganizationAddress {
    public SyncClientAddressType AddressType { get; set; }

    public string Value { get; set; }
}