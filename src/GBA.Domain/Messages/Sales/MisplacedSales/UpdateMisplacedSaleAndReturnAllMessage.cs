using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales.MisplacedSales;

public sealed class UpdateMisplacedSaleAndReturnAllMessage {
    public UpdateMisplacedSaleAndReturnAllMessage(MisplacedSale misplacedSale) {
        MisplacedSale = misplacedSale;
    }

    public MisplacedSale MisplacedSale { get; }
}