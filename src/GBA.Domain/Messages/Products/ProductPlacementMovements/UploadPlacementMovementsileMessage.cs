using System;
using System.Collections.Generic;
using GBA.Common.Helpers.Products;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products.ProductPlacementMovements;

public sealed class UploadPlacementMovementsileMessage {
    public UploadPlacementMovementsileMessage(
        string filePath,
        long storageId,
        Guid userNetId,
        PlacementMovementsStorageParseConfiguration placementMovementsStorageParseConfiguration,
        List<ProductPlacementStorage> productPlacementStorages) {
        FilePath = filePath;
        StorageId = storageId;
        PlacementMovementsStorageParseConfiguration = placementMovementsStorageParseConfiguration;
        UserNetId = userNetId;
        ProductPlacementStorages = productPlacementStorages;
    }

    public string FilePath { get; }
    public long StorageId { get; }
    public Guid UserNetId { get; }
    public PlacementMovementsStorageParseConfiguration PlacementMovementsStorageParseConfiguration { get; }
    public List<ProductPlacementStorage> ProductPlacementStorages { get; }
}