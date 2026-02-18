using GBA.Common.Helpers.RetailClients;

namespace GBA.Domain.Entities.Clients;

public sealed class RetailClientPaymentImageItem : EntityBase {
    public string ImgUrl { get; set; }

    public decimal Amount { get; set; }

    public long? UserId { get; set; }

    public long RetailClientPaymentImageId { get; set; }

    public PaymentType PaymentType { get; set; }

    public string Comment { get; set; }

    public bool IsLocked { get; set; }

    public User User { get; set; }

    public RetailClientPaymentImage RetailClientPaymentImage { get; set; }
}