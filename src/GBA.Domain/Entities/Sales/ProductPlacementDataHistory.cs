using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities.Sales;

public sealed class ProductPlacementDataHistory : EntityBase {
    public double Qty { get; set; }
    public double TotalRowQty { get; set; }
    public string StorageNumber { get; set; }

    public string RowNumber { get; set; }

    public string CellNumber { get; set; }

    public string ConsignmentNumber { get; set; }
    public string VendorCode { get; set; }
    public string MainOriginalNumber { get; set; }
    public string NameUA { get; set; }
    public long? ProductAvailabilityDataHistoryID { get; set; }

    public long? ProductId { get; set; }

    public long? StorageId { get; set; }

    public long? ConsignmentItemId { get; set; }

    public ProductAvailabilityDataHistory ProductAvailabilityDataHistory { get; set; }

    public Product Product { get; set; }

    public Storage Storage { get; set; }

    public ConsignmentItem ConsignmentItem { get; set; }
}