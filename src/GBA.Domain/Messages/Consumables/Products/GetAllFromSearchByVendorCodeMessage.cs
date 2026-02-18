namespace GBA.Domain.Messages.Consumables.Products;

public sealed class GetAllFromSearchByVendorCodeMessage {
    public GetAllFromSearchByVendorCodeMessage(string value) {
        Value = value;
    }

    public string Value { get; set; }
}