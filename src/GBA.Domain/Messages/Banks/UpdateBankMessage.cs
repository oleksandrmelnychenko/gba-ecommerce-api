using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Banks;

public class UpdateBankMessage {
    public UpdateBankMessage(Bank bank) {
        Bank = bank;
    }

    public Bank Bank { get; private set; }
}