namespace GBA.Common.Helpers.DepreciatedOrders;

public sealed class DepreciatedOrderFromFileException {
    public DepreciatedOrderFromFileException(
        string localizeMessage,
        object[] values) {
        LocalizeMessage = localizeMessage;
        Values = values;
    }

    public string LocalizeMessage { get; }
    public object[] Values { get; }
}