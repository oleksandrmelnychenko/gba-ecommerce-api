namespace GBA.Common.Helpers.Products;

public sealed class ProductTransferFromFileException {
    public ProductTransferFromFileException(
        string localizeMessage,
        object[] values) {
        LocalizeMessage = localizeMessage;
        Values = values;
    }

    public string LocalizeMessage { get; }
    public object[] Values { get; }
}