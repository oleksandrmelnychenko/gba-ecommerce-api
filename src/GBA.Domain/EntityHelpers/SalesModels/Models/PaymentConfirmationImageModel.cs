namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class PaymentConfirmationImageModel {
    public PaymentConfirmationImageModel(string image, string imageName) {
        Image = image;
        ImageName = imageName;
    }

    public string Image { get; }
    public string ImageName { get; }
}