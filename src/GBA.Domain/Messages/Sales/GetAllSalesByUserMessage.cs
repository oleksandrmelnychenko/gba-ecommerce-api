using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Sales;

public sealed class GetAllSalesByUserMessage {
    public GetAllSalesByUserMessage(User user) {
        User = user;
    }

    public User User { get; set; }
}