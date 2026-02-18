namespace GBA.Domain.Messages.Logging;

public sealed class NewErrorAddSupplyInvoiceToServicesMessage {
    public NewErrorAddSupplyInvoiceToServicesMessage(string entity, string errorMessage) {
        Entity = entity;
        ErrorMessage = errorMessage;
    }

    public string Entity { get; set; }
    public string ErrorMessage { get; }
}