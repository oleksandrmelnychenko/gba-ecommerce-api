namespace GBA.Domain.Messages.Consumables.CompanyCars;

public sealed class GetAllCompanyCarsFromSearchMessage {
    public GetAllCompanyCarsFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; set; }
}